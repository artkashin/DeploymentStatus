using Azure;
using Azure.Data.Tables;

namespace DeploymentAPI.Repositories.Entities;

/// <summary>
/// Junction entity tracking which applications are installed for each customer
/// </summary>
public class CustomerApplicationEntity : ITableEntity
{
    // ITableEntity properties
    public string PartitionKey { get; set; } = default!; // CustomerId
    public string RowKey { get; set; } = default!; // ApplicationId
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    // Custom properties
    public string CustomerId { get; set; } = default!;
    public string CustomerName { get; set; } = default!;
    public string ApplicationId { get; set; } = default!;
    public string ApplicationName { get; set; } = default!;

    // Version tracking
    public string? InstalledVersion { get; set; }
    public DateTime? InstalledAt { get; set; }
    public string? LatestVersion { get; set; } // Copied from ApplicationEntity
    public string? CiCdTargetVersion { get; set; } // From CiCdVersion table

    // Status tracking
    public string Status { get; set; } = "Unknown"; // Success, Failed, InProgress, Unknown
    public DateTime? LastDeploymentAttempt { get; set; }

    public CustomerApplicationEntity()
    {
    }

    public CustomerApplicationEntity(
        string customerId,
        string customerName,
        string applicationId,
        string applicationName,
        string? installedVersion = null,
        DateTime? installedAt = null,
        string status = "Unknown")
    {
        CustomerId = customerId;
        CustomerName = customerName;
        ApplicationId = applicationId;
        ApplicationName = applicationName;
        InstalledVersion = installedVersion;
        InstalledAt = installedAt;
        Status = status;

        PartitionKey = customerId;
        RowKey = applicationId;
        LastDeploymentAttempt = DateTime.UtcNow;
    }
}
