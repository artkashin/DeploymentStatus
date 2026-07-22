using Azure;
using Azure.Data.Tables;

namespace DeploymentAPI.Repositories.Entities;

/// <summary>
/// Entity for application master data
/// </summary>
public class ApplicationEntity : ITableEntity
{
    // ITableEntity properties
    public string PartitionKey { get; set; } = "Application"; // Fixed partition for all applications
    public string RowKey { get; set; } = default!; // ApplicationId
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    // Custom properties
    public string ApplicationId { get; set; } = default!;
    public string ApplicationName { get; set; } = default!;
    public string? LatestVersion { get; set; } // Latest available version
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ApplicationEntity()
    {
    }

    public ApplicationEntity(string applicationId, string applicationName, string? latestVersion = null)
    {
        ApplicationId = applicationId;
        ApplicationName = applicationName;
        LatestVersion = latestVersion;
        RowKey = applicationId;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
