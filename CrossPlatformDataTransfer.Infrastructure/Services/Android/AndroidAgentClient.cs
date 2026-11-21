using System.Buffers.Binary;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
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
    private const int MaxFrameLength = 1024 * 1024;
    private readonly IAdbService _adbService;
    private TcpClient? _tcpClient;
    private SslStream? _stream;
    private string? _currentDeviceSerial;
    private int _forwardedPort;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public bool IsConnected => _tcpClient?.Connected ?? false;
    public string? VerificationCode { get; private set; }

    public AndroidAgentClient(IAdbService adbService)
    {
        _adbService = adbService;
    }

    public async Task ConnectAsync(string deviceSerial, int port)
    {
        if (string.IsNullOrWhiteSpace(deviceSerial))
            throw new ArgumentException("A device serial is required.", nameof(deviceSerial));
        if (port is < 1 or > 65535)
            throw new ArgumentOutOfRangeException(nameof(port));
        if (_tcpClient is not null)
            await DisconnectAsync();

        _currentDeviceSerial = deviceSerial;
        _forwardedPort = port;
        
        // Ensure ADB Port Forwarding is active
        await _adbService.ForwardPortAsync(deviceSerial, port, port);

        _tcpClient = new TcpClient();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        await _tcpClient.ConnectAsync("127.0.0.1", port, timeout.Token);
        VerificationCode = null;
        _stream = new SslStream(_tcpClient.GetStream(), leaveInnerStreamOpen: false, ValidateAgentCertificate);
        await _stream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
        {
            TargetHost = "TransferAI Agent",
            EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
            CertificateRevocationCheckMode = X509RevocationMode.NoCheck
        }, timeout.Token);
    }

    public async Task<AgentResponse> SendCommandAsync(AgentCommand command)
    {
        if (_stream == null) throw new InvalidOperationException("Not connected to agent.");
        var commandTimeout = string.Equals(command.CommandType, "PAIR", StringComparison.OrdinalIgnoreCase)
            ? TimeSpan.FromSeconds(150)
            : TimeSpan.FromSeconds(30);
        using var timeout = new CancellationTokenSource(commandTimeout);

        // Serialize command to JSON
        var json = JsonSerializer.Serialize(command, _jsonOptions);
        var data = Encoding.UTF8.GetBytes(json);
        if (data.Length > MaxFrameLength)
            throw new InvalidOperationException($"Command frame exceeds {MaxFrameLength} bytes.");
        
        // Protocol: [4-byte Length][JSON Payload]
        var lengthPrefix = new byte[sizeof(int)];
        BinaryPrimitives.WriteInt32BigEndian(lengthPrefix, data.Length);
        
        await _stream.WriteAsync(lengthPrefix.AsMemory(), timeout.Token);
        await _stream.WriteAsync(data.AsMemory(), timeout.Token);
        await _stream.FlushAsync(timeout.Token);

        // Read Response: [4-byte Length][JSON Payload]
        var responseLengthBuffer = new byte[sizeof(int)];
        await ReadExactlyAsync(_stream, responseLengthBuffer, timeout.Token);
        var responseLength = BinaryPrimitives.ReadInt32BigEndian(responseLengthBuffer);
        if (responseLength is <= 0 or > MaxFrameLength)
            throw new InvalidDataException($"Invalid agent response length: {responseLength}.");

        var responseBuffer = new byte[responseLength];
        await ReadExactlyAsync(_stream, responseBuffer, timeout.Token);

        var responseJson = Encoding.UTF8.GetString(responseBuffer);
        return JsonSerializer.Deserialize<AgentResponse>(responseJson, _jsonOptions) 
               ?? new AgentResponse { Success = false, Message = "Deserialization failed" };
    }

    public IAsyncEnumerable<byte[]> StreamDataAsync(AgentCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        throw new NotSupportedException("Streaming is disabled until a versioned, integrity-checked data protocol is implemented.");
    }

    public Task SendEncryptedPayloadAsync(byte[] encryptedData)
    {
        ArgumentNullException.ThrowIfNull(encryptedData);
        return Task.FromException(new NotSupportedException(
            "Payload transfer is disabled until the Android Agent implements the matching authenticated chunk protocol."));
    }

    public async Task DisconnectAsync()
    {
        var stream = _stream;
        var client = _tcpClient;
        var serial = _currentDeviceSerial;
        var port = _forwardedPort;

        _stream = null;
        _tcpClient = null;
        _currentDeviceSerial = null;

        // Closing the socket first interrupts an in-progress TLS handshake and
        // prevents application shutdown from waiting for a graceful TLS alert.
        client?.Close();
        try
        {
            if (stream is not null)
                await stream.DisposeAsync();
        }
        finally
        {
            if (serial is not null)
                await _adbService.RemoveForwardAsync(serial, port);
        }
    }

    public void Dispose()
    {
        _stream?.Dispose();
        _tcpClient?.Dispose();
    }

    private static async Task ReadExactlyAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        await ReadExactlyAsync(stream, buffer.AsMemory(), cancellationToken);
    }

    private static async Task ReadExactlyAsync(Stream stream, Memory<byte> buffer, CancellationToken cancellationToken)
    {
        int offset = 0;
        while (offset < buffer.Length)
        {
            int read = await stream.ReadAsync(buffer[offset..], cancellationToken);
            if (read == 0)
                throw new EndOfStreamException("The Android Agent closed the connection mid-frame.");
            offset += read;
        }
    }

    private bool ValidateAgentCertificate(
        object sender,
        X509Certificate? certificate,
        X509Chain? chain,
        SslPolicyErrors sslPolicyErrors)
    {
        if (certificate is null) return false;

        try
        {
            using var candidate = new X509Certificate2(certificate);
            if (candidate.Subject != candidate.Issuer ||
                candidate.GetNameInfo(X509NameType.SimpleName, forIssuer: false) != "TransferAI Agent" ||
                candidate.NotBefore.ToUniversalTime() > DateTime.UtcNow ||
                candidate.NotAfter.ToUniversalTime() < DateTime.UtcNow)
                return false;

            using var ecdsa = candidate.GetECDsaPublicKey();
            if (ecdsa is null || ecdsa.KeySize != 256) return false;

            using var trustedChain = new X509Chain();
            trustedChain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
            trustedChain.ChainPolicy.CustomTrustStore.Add(candidate);
            trustedChain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            if (!trustedChain.Build(candidate)) return false;

            VerificationCode = CreateVerificationCode(candidate.RawData);
            return true;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    private static string CreateVerificationCode(byte[] certificateDer)
    {
        var digest = SHA256.HashData(certificateDer);
        var code = Convert.ToHexString(digest.AsSpan(0, 5));
        return $"{code[..5]}-{code[5..]}";
    }
}
