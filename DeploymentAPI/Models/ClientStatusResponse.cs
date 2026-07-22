namespace DeploymentAPI.Models;

public class ClientStatusResponse
{
    public required string ClientId { get; set; }
    public required string ClientName { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string Status { get; set; } = "Active"; // Active, Inactive
    public string? MaxVersion { get; set; }
    public string? MinVersion { get; set; }
    public string? CiCdVersion { get; set; }
    public List<ApplicationStatusDetail> Applications { get; set; } = new();
}

public class ApplicationStatusDetail
{
    public required string ApplicationId { get; set; }
    public required string ApplicationName { get; set; }
    public string? InstalledVersion { get; set; }
    public DateTime? InstalledAt { get; set; }
    public string? LatestVersion { get; set; }
    public string? CiCdTargetVersion { get; set; }
    public string Status { get; set; } = "Unknown"; // Success, Failed, InProgress, Unknown
    public DateTime? LastDeploymentTime { get; set; }
    public bool IsUpToDate { get; set; } // InstalledVersion == CiCdTargetVersion
    public bool IsBehind { get; set; } // InstalledVersion < CiCdTargetVersion (or not equal)
}

public class AllClientsStatusResponse
{
    public List<ClientStatusResponse> Clients { get; set; } = new();
    public int TotalClients { get; set; }
    public DateTime GeneratedAt { get; set; }
}
