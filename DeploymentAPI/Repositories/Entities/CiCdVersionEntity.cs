using Azure;
using Azure.Data.Tables;
using DeploymentAPI.Models;

namespace DeploymentAPI.Repositories.Entities;

public class CiCdVersionEntity : ITableEntity
{
    // ITableEntity properties
    public string PartitionKey { get; set; } = "CiCdVersion"; // Fixed partition
    public string RowKey { get; set; } = "Current"; // Fixed row key
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    // Custom properties
    public string Version { get; set; } = default!;
    public DateTime UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public string? Notes { get; set; }

    public CiCdVersionEntity()
    {
    }

    public CiCdVersionEntity(CiCdVersion version)
    {
        PartitionKey = "CiCdVersion";
        RowKey = "Current";
        
        Version = version.Version;
        UpdatedAt = version.UpdatedAt;
        UpdatedBy = version.UpdatedBy;
        Notes = version.Notes;
    }

    public CiCdVersion ToCiCdVersion()
    {
        return new CiCdVersion
        {
            Version = Version,
            UpdatedAt = UpdatedAt,
            UpdatedBy = UpdatedBy,
            Notes = Notes
        };
    }
}
