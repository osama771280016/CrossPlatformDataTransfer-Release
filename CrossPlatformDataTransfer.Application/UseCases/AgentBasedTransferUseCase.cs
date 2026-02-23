using CrossPlatformDataTransfer.Application.DTOs;
using CrossPlatformDataTransfer.Core.Interfaces.Repositories;
using CrossPlatformDataTransfer.Core.Interfaces.Services;

namespace CrossPlatformDataTransfer.Application.UseCases;

public class AgentBasedTransferUseCase
{
    private readonly IAgentCommunicationService _agentClient;
    private readonly IDeviceRepository _deviceRepository;
    private readonly IEncryptionService _encryptionService;
    private readonly IKeyManagementService _keyManagement;

    public AgentBasedTransferUseCase(
        IAgentCommunicationService agentClient,
        IDeviceRepository deviceRepository,
        IEncryptionService encryptionService,
        IKeyManagementService keyManagement)
    {
        _agentClient = agentClient;
        _deviceRepository = deviceRepository;
        _encryptionService = encryptionService;
        _keyManagement = keyManagement;
    }

    public async Task ExecuteFullMigrationAsync(string sourceSerial, string targetSerial)
    {
        // 1. Handshake with Source Agent
        await _agentClient.ConnectAsync(sourceSerial, 8888);
        var handshake = await _agentClient.SendCommandAsync(new AgentCommand { CommandType = "HANDSHAKE" });
        if (!handshake.Success) throw new Exception("Handshake failed with source agent.");

        // 2. Request Permissions
        await _agentClient.SendCommandAsync(new AgentCommand 
        { 
            CommandType = "PERMISSION_CHECKER",
            Parameters = new Dictionary<string, string> { { "action", "request" }, { "scope", "sms" } }
        });

        // 3. Start Streaming SMS
        var fetchSmsCommand = new AgentCommand { CommandType = "GET_SMS" };
        
        await foreach (var chunk in _agentClient.StreamDataAsync(fetchSmsCommand))
        {
            // Perform Client-Side Encryption (AES-256-GCM)
            var sessionKey = _keyManagement.GenerateNewKey(256);
            var encrypted = await _encryptionService.EncryptAesGcmAsync(chunk, sessionKey);
            
            // Log progress or handle data
            Console.WriteLine($"Processed encrypted chunk of size: {encrypted.EncryptedData.Length}");
        }

        await _agentClient.DisconnectAsync();
    }
}
