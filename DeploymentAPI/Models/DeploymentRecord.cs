namespace DeploymentAPI.Models;

public class DeploymentRecord
{
    public required string ClientId { get; set; }
    public required string ClientName { get; set; }
    public required string ApplicationId { get; set; }
    public required string ApplicationName { get; set; }
    public required string Version { get; set; }
    public DateTime DeploymentTime { get; set; }
    public DeploymentStatus Status { get; set; }
}

public enum DeploymentStatus
{
    Success,
    Failed,
    InProgress
}
