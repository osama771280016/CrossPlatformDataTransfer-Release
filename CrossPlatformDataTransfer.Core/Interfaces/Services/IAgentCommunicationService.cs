using CrossPlatformDataTransfer.Core.Entities;
using CrossPlatformDataTransfer.Core.Enums;

namespace CrossPlatformDataTransfer.Core.Interfaces.Services;

public interface IAgentCommunicationService
{
    Task ConnectAsync(string deviceSerial, int port);
    Task DisconnectAsync();
    Task<AgentResponse> SendCommandAsync(AgentCommand command);
    IAsyncEnumerable<byte[]> StreamDataAsync(AgentCommand command);
    Task SendEncryptedPayloadAsync(byte[] encryptedData);
    bool IsConnected { get; }
}

public class AgentCommand
{
    public string CommandType { get; set; } = string.Empty;
    public Dictionary<string, string> Parameters { get; set; } = new();
    public byte[]? Payload { get; set; }
}

public class AgentResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public byte[]? Data { get; set; }
}
