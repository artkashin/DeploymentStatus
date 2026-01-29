namespace DeploymentAPI.Models;

public class UpdateCiCdVersionRequest
{
    public required string Version { get; set; }
    public string? UpdatedBy { get; set; }
    public string? Notes { get; set; }
}
