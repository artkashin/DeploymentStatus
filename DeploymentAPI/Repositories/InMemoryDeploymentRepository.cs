using DeploymentAPI.Models;
using System.Collections.Concurrent;

namespace DeploymentAPI.Repositories;

public class InMemoryDeploymentRepository : IDeploymentRepository
{
    private readonly ConcurrentDictionary<string, DeploymentRecord> _currentDeployments = new();
    private readonly ConcurrentBag<DeploymentRecord> _deploymentHistory = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private CiCdVersion? _currentCiCdVersion;

    public Task RegisterDeploymentAsync(DeploymentRecord deployment)
    {
        deployment.DeploymentTime = DateTime.UtcNow;
        
        // Create unique key for current state: ClientId_ApplicationId
        var currentKey = $"{deployment.ClientId}_{deployment.ApplicationId}";
        
        // Check if version is different
        bool shouldUpdateCurrent = true;
        if (_currentDeployments.TryGetValue(currentKey, out var existing))
        {
            shouldUpdateCurrent = existing.Version != deployment.Version;
        }
        
        // Always add to history
        _deploymentHistory.Add(deployment);
        
        // Update current state only if version changed
        if (shouldUpdateCurrent)
        {
            _currentDeployments[currentKey] = deployment;
        }
        
        return Task.CompletedTask;
    }

    public async Task<ClientStatusResponse?> GetClientStatusAsync(string clientId)
    {
        await _semaphore.WaitAsync();
        try
        {
            var clientDeployments = _currentDeployments.Values
                .Where(d => d.ClientId == clientId)
                .ToList();

            if (!clientDeployments.Any())
                return null;

            var clientName = clientDeployments.First().ClientName;

            var applicationGroups = clientDeployments
                .Select(d => new ApplicationStatus
                {
                    ApplicationId = d.ApplicationId,
                    ApplicationName = d.ApplicationName,
                    CurrentVersion = d.Version,
                    LastDeploymentTime = d.DeploymentTime,
                    LastDeploymentStatus = d.Status
                })
                .ToList();

            var versions = applicationGroups
                .Where(a => !string.IsNullOrEmpty(a.CurrentVersion))
                .Select(a => a.CurrentVersion!)
                .ToList();

            return new ClientStatusResponse
            {
                ClientId = clientId,
                ClientName = clientName,
                MaxVersion = versions.Any() ? versions.Max() : null,
                MinVersion = versions.Any() ? versions.Min() : null,
                CiCdVersion = _currentCiCdVersion?.Version,
                Applications = applicationGroups
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<AllClientsStatusResponse> GetAllClientsStatusAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            var clientIds = _currentDeployments.Values
                .Select(d => d.ClientId)
                .Distinct()
                .ToList();

            var clientStatuses = new List<ClientStatusResponse>();

            foreach (var clientId in clientIds)
            {
                var status = await GetClientStatusAsync(clientId);
                if (status != null)
                {
                    clientStatuses.Add(status);
                }
            }

            return new AllClientsStatusResponse
            {
                Clients = clientStatuses.OrderBy(c => c.ClientName).ToList(),
                TotalClients = clientStatuses.Count,
                GeneratedAt = DateTime.UtcNow
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<List<DeploymentRecord>> GetDeploymentHistoryAsync(string clientId, string? applicationId = null, int limit = 100)
    {
        await _semaphore.WaitAsync();
        try
        {
            var query = _deploymentHistory.Where(d => d.ClientId == clientId);

            if (!string.IsNullOrEmpty(applicationId))
            {
                query = query.Where(d => d.ApplicationId == applicationId);
            }

            return query
                .OrderByDescending(d => d.DeploymentTime)
                .Take(limit)
                .ToList();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task UpdateCiCdVersionAsync(CiCdVersion ciCdVersion)
    {
        await _semaphore.WaitAsync();
        try
        {
            ciCdVersion.UpdatedAt = DateTime.UtcNow;
            _currentCiCdVersion = ciCdVersion;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public Task<CiCdVersion?> GetCurrentCiCdVersionAsync()
    {
        return Task.FromResult(_currentCiCdVersion);
    }
}

