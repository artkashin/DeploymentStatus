using DeploymentStatus.Api.Models;

namespace DeploymentStatus.Api.Storage;

public interface IDeploymentStore
{
    Task<bool> RegisterAsync(DeploymentEvent item, CancellationToken cancellationToken = default);
    Task<PagedDeployments> QueryAsync(DeploymentQuery query, CancellationToken cancellationToken = default);
    Task<DeploymentEvent?> GetAsync(string eventId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerLatestStatus>> GetCustomersAsync(IReadOnlySet<string>? customerIds, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CurrentDeploymentState>> GetCurrentStateAsync(string customerId, CancellationToken cancellationToken = default);
}
