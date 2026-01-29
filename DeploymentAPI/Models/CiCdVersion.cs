namespace DeploymentAPI.Models;

public class CiCdVersion
{
    public required string Version { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public string? Notes { get; set; }
}
