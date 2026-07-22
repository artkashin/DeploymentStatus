using DeploymentAPI.Models;
using DeploymentAPI.Repositories.Entities;

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

    // Customer Management
    Task<CustomerEntity?> GetCustomerAsync(string customerId);
    Task<List<CustomerEntity>> GetAllCustomersAsync();
    Task UpsertCustomerAsync(CustomerEntity customer);

    // Application Management
    Task<ApplicationEntity?> GetApplicationAsync(string applicationId);
    Task<List<ApplicationEntity>> GetAllApplicationsAsync();
    Task UpsertApplicationAsync(ApplicationEntity application);

    // Customer-Application Relationship Management
    Task<CustomerApplicationEntity?> GetCustomerApplicationAsync(string customerId, string applicationId);
    Task<List<CustomerApplicationEntity>> GetCustomerApplicationsAsync(string customerId);
    Task<List<CustomerApplicationEntity>> GetAllCustomerApplicationsAsync();
    Task UpsertCustomerApplicationAsync(CustomerApplicationEntity customerApp);
}
