namespace DeploymentAPI.Models;

public class ClientStatusResponse
{
    public required string ClientId { get; set; }
    public required string ClientName { get; set; }
    public string? MaxVersion { get; set; }
    public string? MinVersion { get; set; }
    public string? CiCdVersion { get; set; }
    public List<ApplicationStatus> Applications { get; set; } = new();
}

public class ApplicationStatus
{
    public required string ApplicationId { get; set; }
    public required string ApplicationName { get; set; }
    public string? CurrentVersion { get; set; }
    public DateTime? LastDeploymentTime { get; set; }
    public DeploymentStatus? LastDeploymentStatus { get; set; }
}

public class AllClientsStatusResponse
{
    public List<ClientStatusResponse> Clients { get; set; } = new();
    public int TotalClients { get; set; }
    public DateTime GeneratedAt { get; set; }
}
