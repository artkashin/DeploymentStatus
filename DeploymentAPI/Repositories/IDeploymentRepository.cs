using DeploymentAPI.Models;

namespace DeploymentAPI.Repositories;

public interface IDeploymentRepository
{
    Task RegisterDeploymentAsync(DeploymentRecord deployment);
    Task<ClientStatusResponse?> GetClientStatusAsync(string clientId);
    Task<AllClientsStatusResponse> GetAllClientsStatusAsync();
    Task<List<DeploymentRecord>> GetDeploymentHistoryAsync(string clientId, string? applicationId = null, int limit = 100);
    
    // CI/CD Version Management
    Task UpdateCiCdVersionAsync(CiCdVersion ciCdVersion);
    Task<CiCdVersion?> GetCurrentCiCdVersionAsync();
}
