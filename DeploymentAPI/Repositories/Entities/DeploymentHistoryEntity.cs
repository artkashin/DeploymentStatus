using Azure;
using Azure.Data.Tables;
using DeploymentAPI.Models;

namespace DeploymentAPI.Repositories.Entities;

/// <summary>
/// Entity for deployment history (all deployments, chronological order)
/// </summary>
public class DeploymentHistoryEntity : ITableEntity
{
    // ITableEntity properties
    public string PartitionKey { get; set; } = default!; // ClientId
    public string RowKey { get; set; } = default!; // Timestamp (reverse sorted for newest first)
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    // Custom properties
    public string ClientId { get; set; } = default!;
    public string ClientName { get; set; } = default!;
    public string ApplicationId { get; set; } = default!;
    public string ApplicationName { get; set; } = default!;
    public string Version { get; set; } = default!;
    public DateTime DeploymentTime { get; set; }
    public int Status { get; set; } // 0=Success, 1=Failed, 2=InProgress

    public DeploymentHistoryEntity()
    {
    }

    public DeploymentHistoryEntity(DeploymentRecord record)
    {
        PartitionKey = record.ClientId;
        
        // Reverse timestamp for newest-first sorting
        var reverseTicks = DateTime.MaxValue.Ticks - record.DeploymentTime.Ticks;
        RowKey = $"{reverseTicks:D19}";
        
        ClientId = record.ClientId;
        ClientName = record.ClientName;
        ApplicationId = record.ApplicationId;
        ApplicationName = record.ApplicationName;
        Version = record.Version;
        DeploymentTime = record.DeploymentTime;
        Status = (int)record.Status;
    }

    public DeploymentRecord ToDeploymentRecord()
    {
        return new DeploymentRecord
        {
            ClientId = ClientId,
            ClientName = ClientName,
            ApplicationId = ApplicationId,
            ApplicationName = ApplicationName,
            Version = Version,
            DeploymentTime = DeploymentTime,
            Status = (DeploymentStatus)Status
        };
    }
}
