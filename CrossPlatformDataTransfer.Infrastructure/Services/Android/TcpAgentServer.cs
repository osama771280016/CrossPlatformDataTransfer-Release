using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Buffers.Binary;
using CrossPlatformDataTransfer.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace CrossPlatformDataTransfer.Infrastructure.Services.Android;

public class TcpAgentServer : ITcpAgentServer, IDisposable
{
    private readonly IAdbService _adbService;
    private readonly ILogger<TcpAgentServer> _logger;
    private TcpListener? _listener;
    private TcpClient? _currentClient;
    private CancellationTokenSource? _cts;
    private bool _isRunning;
    private const int BufferSize = 8192;

    public event EventHandler<AgentConnectedEventArgs>? AgentConnected;
    public event EventHandler<AgentMessageReceivedEventArgs>? MessageReceived;
    public event EventHandler? AgentDisconnected;

    public bool IsRunning => _isRunning;

    public TcpAgentServer(IAdbService adbService, ILogger<TcpAgentServer> logger)
    {
        _adbService = adbService;
        _logger = logger;
    }

    public async Task StartAsync(int port, CancellationToken cancellationToken = default)
    {
        if (_isRunning) return;

        try
        {
            // 1. Setup ADB Port Forwarding
            _logger.LogInformation("Setting up ADB port forward for port {Port}...", port);
            await _adbService.ExecuteAdbCommandAsync($"forward tcp:{port} tcp:{port}");

            // 2. Start TCP Listener
            _listener = new TcpListener(IPAddress.Loopback, port);
            _listener.Start();
            _isRunning = true;
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            _logger.LogInformation("TCP Server listening on 127.0.0.1:{Port}", port);

            // 3. Accept loop
            _ = Task.Run(() => AcceptLoopAsync(_cts.Token), _cts.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start TCP Server");
            throw;
        }
    }

    private async Task AcceptLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                var client = await _listener!.AcceptTcpClientAsync(token);
                _logger.LogInformation("New agent connection from {EndPoint}", client.Client.RemoteEndPoint);

                if (_currentClient != null)
                {
                    _logger.LogWarning("Existing client connection detected. Closing old connection.");
                    _currentClient.Close();
                }

                _currentClient = client;
                _ = Task.Run(() => HandleClientAsync(client, token), token);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in accept loop");
            }
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken token)
    {
        using var stream = client.GetStream();
        var headerLengthBuffer = new byte[4];

        try
        {
            while (!token.IsCancellationRequested && client.Connected)
            {
                // 1. Read Header Length (4-byte big-endian int)
                int bytesRead = await ReadExactlyAsync(stream, headerLengthBuffer, 4, token);
                if (bytesRead == 0) break;

                int headerLength = BinaryPrimitives.ReadInt32BigEndian(headerLengthBuffer);
                if (headerLength <= 0 || headerLength > 1024 * 1024) // 1MB sanity check
                {
                    _logger.LogError("Invalid header length: {Length}", headerLength);
                    break;
                }

                // 2. Read JSON Header
                var headerBuffer = new byte[headerLength];
                await ReadExactlyAsync(stream, headerBuffer, headerLength, token);
                string jsonHeader = Encoding.UTF8.GetString(headerBuffer);
                _logger.LogDebug("Received JSON Header: {Header}", jsonHeader);

                var headerDoc = JsonDocument.Parse(jsonHeader);
                string command = headerDoc.RootElement.GetProperty("command").GetString() ?? "UNKNOWN";
                long payloadSize = 0;
                if (headerDoc.RootElement.TryGetProperty("payloadSize", out var payloadElem))
                {
                    payloadSize = payloadElem.GetInt64();
                }

                // 3. Read Payload if present
                byte[]? payload = null;
                if (payloadSize > 0)
                {
                    payload = new byte[payloadSize];
                    await ReadExactlyAsync(stream, payload, (int)payloadSize, token);
                }

                // 4. Handle Internal Commands (PING/CONNECTED)
                HandleInternalCommand(command, jsonHeader, payload);

                // 5. Notify Subscribers
                MessageReceived?.Invoke(this, new AgentMessageReceivedEventArgs
                {
                    Command = command,
                    JsonHeader = jsonHeader,
                    Payload = payload
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling client connection");
        }
        finally
        {
            _logger.LogInformation("Agent disconnected.");
            _currentClient = null;
            AgentDisconnected?.Invoke(this, EventArgs.Empty);
        }
    }

    private void HandleInternalCommand(string command, string jsonHeader, byte[]? payload)
    {
        switch (command)
        {
            case "CONNECTED":
                var headerDoc = JsonDocument.Parse(jsonHeader);
                var deviceId = headerDoc.RootElement.TryGetProperty("deviceId", out var d) ? d.GetString() : "Unknown";
                var model = headerDoc.RootElement.TryGetProperty("model", out var m) ? m.GetString() : "Unknown";
                var version = headerDoc.RootElement.TryGetProperty("version", out var v) ? v.GetString() : "Unknown";

                AgentConnected?.Invoke(this, new AgentConnectedEventArgs
                {
                    DeviceId = deviceId ?? "Unknown",
                    Model = model ?? "Unknown",
                    Version = version ?? "Unknown"
                });
                break;

            case "PING":
                _ = SendCommandAsync("PONG");
                break;
        }
    }

    public async Task SendCommandAsync(string command, object? payload = null)
    {
        if (_currentClient == null || !_currentClient.Connected)
        {
            _logger.LogWarning("Attempted to send command '{Command}' but no agent is connected.", command);
            return;
        }

        try
        {
            var stream = _currentClient.GetStream();
            byte[]? payloadBytes = null;
            long payloadSize = 0;

            if (payload != null)
            {
                if (payload is byte[] bytes)
                {
                    payloadBytes = bytes;
                }
                else
                {
                    payloadBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));
                }
                payloadSize = payloadBytes.Length;
            }

            var headerObj = new { command, payloadSize, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() };
            byte[] headerBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(headerObj));

            // Write Header Length
            byte[] lengthBuffer = new byte[4];
            BinaryPrimitives.WriteInt32BigEndian(lengthBuffer, headerBytes.Length);
            await stream.WriteAsync(lengthBuffer, 0, 4);

            // Write Header
            await stream.WriteAsync(headerBytes, 0, headerBytes.Length);

            // Write Payload
            if (payloadBytes != null)
            {
                await stream.WriteAsync(payloadBytes, 0, payloadBytes.Length);
            }

            await stream.FlushAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send command '{Command}'", command);
        }
    }

    private async Task<int> ReadExactlyAsync(Stream stream, byte[] buffer, int length, CancellationToken token)
    {
        int totalRead = 0;
        while (totalRead < length)
        {
            int read = await stream.ReadAsync(buffer, totalRead, length - totalRead, token);
            if (read == 0) return 0;
            totalRead += read;
        }
        return totalRead;
    }

    public async Task StopAsync()
    {
        _isRunning = false;
        _cts?.Cancel();
        _listener?.Stop();
        _currentClient?.Close();
        _logger.LogInformation("TCP Server stopped.");
        await Task.CompletedTask;
    }

    public void Dispose()
    {
        _cts?.Dispose();
        _currentClient?.Dispose();
    }
}
