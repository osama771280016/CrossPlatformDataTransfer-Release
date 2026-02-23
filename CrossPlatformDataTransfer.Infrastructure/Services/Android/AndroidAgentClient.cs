using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using CrossPlatformDataTransfer.Core.Interfaces.Services;

namespace CrossPlatformDataTransfer.Infrastructure.Services.Android;

/// <summary>
/// Refined Client for communicating with the Android Data Agent.
/// Implements length-prefixed JSON protocol matching the Kotlin SocketServer.
/// </summary>
public class AndroidAgentClient : IAgentCommunicationService, IDisposable
{
    private readonly IAdbService _adbService;
    private TcpClient? _tcpClient;
    private NetworkStream? _stream;
    private string? _currentDeviceSerial;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public bool IsConnected => _tcpClient?.Connected ?? false;

    public AndroidAgentClient(IAdbService adbService)
    {
        _adbService = adbService;
    }

    public async Task ConnectAsync(string deviceSerial, int port)
    {
        _currentDeviceSerial = deviceSerial;
        
        // Ensure ADB Port Forwarding is active
        await _adbService.ExecuteAdbCommandAsync($"-s {deviceSerial} forward tcp:{port} tcp:{port}");

        _tcpClient = new TcpClient();
        await _tcpClient.ConnectAsync("127.0.0.1", port);
        _stream = _tcpClient.GetStream();
    }

    public async Task<AgentResponse> SendCommandAsync(AgentCommand command)
    {
        if (_stream == null) throw new InvalidOperationException("Not connected to agent.");

        // Serialize command to JSON
        var json = JsonSerializer.Serialize(command, _jsonOptions);
        var data = Encoding.UTF8.GetBytes(json);
        
        // Protocol: [4-byte Length][JSON Payload]
        var lengthPrefix = BitConverter.GetBytes(data.Length);
        if (BitConverter.IsLittleEndian) Array.Reverse(lengthPrefix); // Network Byte Order (Big Endian)
        
        await _stream.WriteAsync(lengthPrefix, 0, 4);
        await _stream.WriteAsync(data, 0, data.Length);
        await _stream.FlushAsync();

        // Read Response: [4-byte Length][JSON Payload]
        var responseLengthBuffer = new byte[4];
        int read = await _stream.ReadAsync(responseLengthBuffer, 0, 4);
        if (read < 4) throw new Exception("Failed to read response length.");
        
        if (BitConverter.IsLittleEndian) Array.Reverse(responseLengthBuffer);
        var responseLength = BitConverter.ToInt32(responseLengthBuffer, 0);

        var responseBuffer = new byte[responseLength];
        int totalRead = 0;
        while (totalRead < responseLength)
        {
            read = await _stream.ReadAsync(responseBuffer, totalRead, responseLength - totalRead);
            if (read == 0) break;
            totalRead += read;
        }

        var responseJson = Encoding.UTF8.GetString(responseBuffer);
        return JsonSerializer.Deserialize<AgentResponse>(responseJson, _jsonOptions) 
               ?? new AgentResponse { Success = false, Message = "Deserialization failed" };
    }

    public async IAsyncEnumerable<byte[]> StreamDataAsync(AgentCommand command)
    {
        if (_stream == null) throw new InvalidOperationException("Not connected to agent.");

        // Send command to start streaming (e.g., GET_SMS)
        await SendCommandAsync(command);

        // Protocol for streaming: [4-byte Chunk Length][Raw Bytes]
        while (true)
        {
            var lengthBuffer = new byte[4];
            int read = await _stream.ReadAsync(lengthBuffer, 0, 4);
            if (read < 4) break;

            if (BitConverter.IsLittleEndian) Array.Reverse(lengthBuffer);
            var chunkLength = BitConverter.ToInt32(lengthBuffer, 0);
            
            if (chunkLength <= 0) break; // EOF or End of Stream signal

            var chunkBuffer = new byte[chunkLength];
            int totalRead = 0;
            while (totalRead < chunkLength)
            {
                read = await _stream.ReadAsync(chunkBuffer, totalRead, chunkLength - totalRead);
                if (read == 0) break;
                totalRead += read;
            }

            yield return chunkBuffer;
        }
    }

    public async Task SendEncryptedPayloadAsync(byte[] encryptedData)
    {
        if (_stream == null) throw new InvalidOperationException("Not connected to agent.");

        // 1. Notify agent of incoming data
        await SendCommandAsync(new AgentCommand { CommandType = "INSERT_DATA_START" });

        // 2. Send payload with length prefix
        var lengthPrefix = BitConverter.GetBytes(encryptedData.Length);
        if (BitConverter.IsLittleEndian) Array.Reverse(lengthPrefix);
        
        await _stream.WriteAsync(lengthPrefix, 0, 4);
        await _stream.WriteAsync(encryptedData, 0, encryptedData.Length);
        await _stream.FlushAsync();
    }

    public async Task DisconnectAsync()
    {
        if (_stream != null) await _stream.DisposeAsync();
        _tcpClient?.Close();
        
        if (_currentDeviceSerial != null)
        {
            await _adbService.ExecuteAdbCommandAsync($"-s {_currentDeviceSerial} forward --remove-all");
        }
    }

    public void Dispose()
    {
        _stream?.Dispose();
        _tcpClient?.Dispose();
    }
}
