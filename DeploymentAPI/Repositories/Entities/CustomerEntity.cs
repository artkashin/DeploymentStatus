using Azure;
using Azure.Data.Tables;

namespace DeploymentAPI.Repositories.Entities;

/// <summary>
/// Entity for customer master data
/// </summary>
public class CustomerEntity : ITableEntity
{
    // ITableEntity properties
    public string PartitionKey { get; set; } = "Customer"; // Fixed partition for all customers
    public string RowKey { get; set; } = default!; // CustomerId
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    // Custom properties
    public string CustomerId { get; set; } = default!;
    public string CustomerName { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string Status { get; set; } = "Active"; // Active, Inactive

    public CustomerEntity()
    {
    }

    public CustomerEntity(string customerId, string customerName)
    {
        CustomerId = customerId;
        CustomerName = customerName;
        RowKey = customerId;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        Status = "Active";
    }
}
