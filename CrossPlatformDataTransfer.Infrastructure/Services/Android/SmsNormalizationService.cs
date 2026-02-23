using CrossPlatformDataTransfer.Core.Entities;
using CrossPlatformDataTransfer.Core.Enums;

namespace CrossPlatformDataTransfer.Infrastructure.Services.Android;

/// <summary>
/// Service responsible for normalizing raw SQLite SMS data into Universal TransferItems.
/// </summary>
public class SmsNormalizationService
{
    public IEnumerable<TransferItem> NormalizeSmsData(string dbPath)
    {
        // In a real implementation, we would use Microsoft.Data.Sqlite to read the DB
        // For this architecture demo, we'll simulate the mapping logic
        
        var items = new List<TransferItem>();
        
        // Mocking the extraction of 2 SMS records from the DB for demonstration
        items.Add(new TransferItem
        {
            Id = Guid.NewGuid(),
            Name = "SMS_001",
            Type = TransferType.Message,
            Size = 1024,
            Metadata = new Dictionary<string, string>
            {
                { "address", "+123456789" },
                { "body", "Hello from Source Device!" },
                { "date", DateTime.Now.ToString() }
            }
        });

        items.Add(new TransferItem
        {
            Id = Guid.NewGuid(),
            Name = "SMS_002",
            Type = TransferType.Message,
            Size = 1024,
            Metadata = new Dictionary<string, string>
            {
                { "address", "+987654321" },
                { "body", "Architecture is solid." },
                { "date", DateTime.Now.ToString() }
            }
        });

        return items;
    }
}
