using System;
using System.Threading;
using System.Threading.Tasks;

namespace CrossPlatformDataTransfer.Core.Interfaces.Services;

/// <summary>
/// Represents the PC-side TCP Server that listens for connections from the Android Agent via ADB forward.
/// </summary>
public interface ITcpAgentServer
{
    event EventHandler<AgentConnectedEventArgs> AgentConnected;
    event EventHandler<AgentMessageReceivedEventArgs> MessageReceived;
    event EventHandler AgentDisconnected;

    bool IsRunning { get; }
    Task StartAsync(int port, CancellationToken cancellationToken = default);
    Task StopAsync();
    Task SendCommandAsync(string command, object? payload = null);
}

public class AgentConnectedEventArgs : EventArgs
{
    public string DeviceId { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
}

public class AgentMessageReceivedEventArgs : EventArgs
{
    public string Command { get; set; } = string.Empty;
    public string? JsonHeader { get; set; }
    public byte[]? Payload { get; set; }
}
