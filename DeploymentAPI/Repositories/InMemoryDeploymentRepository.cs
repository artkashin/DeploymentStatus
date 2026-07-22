using DeploymentAPI.Models;
using DeploymentAPI.Repositories.Entities;
using System.Collections.Concurrent;

namespace DeploymentAPI.Repositories;

public class InMemoryDeploymentRepository : IDeploymentRepository
{
    private readonly ConcurrentDictionary<string, DeploymentRecord> _currentDeployments = new();
    private readonly ConcurrentBag<DeploymentRecord> _deploymentHistory = new();
    private readonly ConcurrentDictionary<string, CustomerEntity> _customers = new();
    private readonly ConcurrentDictionary<string, ApplicationEntity> _applications = new();
    private readonly ConcurrentDictionary<string, CustomerApplicationEntity> _customerApplications = new();
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
            // Get customer
            if (!_customers.TryGetValue(clientId, out var customer))
                return null;

            // Get all applications for this customer
            var customerApps = _customerApplications.Values
                .Where(ca => ca.CustomerId == clientId)
                .ToList();

            if (!customerApps.Any())
                return null;

            var cicdTargetVersion = _currentCiCdVersion?.Version;

            // Convert to application status details
            var applicationDetails = customerApps
                .Select(ca => new ApplicationStatusDetail
                {
                    ApplicationId = ca.ApplicationId,
                    ApplicationName = ca.ApplicationName,
                    InstalledVersion = ca.InstalledVersion,
                    InstalledAt = ca.InstalledAt,
                    LatestVersion = ca.LatestVersion,
                    CiCdTargetVersion = cicdTargetVersion,
                    Status = ca.Status,
                    LastDeploymentTime = ca.LastDeploymentAttempt,
                    IsUpToDate = !string.IsNullOrEmpty(ca.InstalledVersion) && 
                                 ca.InstalledVersion == cicdTargetVersion,
                    IsBehind = !string.IsNullOrEmpty(ca.InstalledVersion) && 
                               !string.IsNullOrEmpty(cicdTargetVersion) &&
                               ca.InstalledVersion != cicdTargetVersion
                })
                .ToList();

            var versions = applicationDetails
                .Where(a => !string.IsNullOrEmpty(a.InstalledVersion))
                .Select(a => a.InstalledVersion!)
                .ToList();

            return new ClientStatusResponse
            {
                ClientId = customer.CustomerId,
                ClientName = customer.CustomerName,
                CreatedAt = customer.CreatedAt,
                Status = customer.Status,
                MaxVersion = versions.Any() ? versions.Max() : null,
                MinVersion = versions.Any() ? versions.Min() : null,
                CiCdVersion = cicdTargetVersion,
                Applications = applicationDetails
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
            var clientStatuses = new List<ClientStatusResponse>();

            foreach (var customer in _customers.Values)
            {
                var status = await GetClientStatusAsync(customer.CustomerId);
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

    // Customer Management Methods
    public Task<CustomerEntity?> GetCustomerAsync(string customerId)
    {
        _customers.TryGetValue(customerId, out var customer);
        return Task.FromResult(customer);
    }

    public Task<List<CustomerEntity>> GetAllCustomersAsync()
    {
        return Task.FromResult(_customers.Values.ToList());
    }

    public Task UpsertCustomerAsync(CustomerEntity customer)
    {
        customer.UpdatedAt = DateTime.UtcNow;
        _customers[customer.CustomerId] = customer;
        return Task.CompletedTask;
    }

    // Application Management Methods
    public Task<ApplicationEntity?> GetApplicationAsync(string applicationId)
    {
        _applications.TryGetValue(applicationId, out var application);
        return Task.FromResult(application);
    }

    public Task<List<ApplicationEntity>> GetAllApplicationsAsync()
    {
        return Task.FromResult(_applications.Values.ToList());
    }

    public Task UpsertApplicationAsync(ApplicationEntity application)
    {
        application.UpdatedAt = DateTime.UtcNow;
        _applications[application.ApplicationId] = application;
        return Task.CompletedTask;
    }

    // Customer-Application Relationship Methods
    public Task<CustomerApplicationEntity?> GetCustomerApplicationAsync(string customerId, string applicationId)
    {
        var key = $"{customerId}_{applicationId}";
        _customerApplications.TryGetValue(key, out var customerApp);
        return Task.FromResult(customerApp);
    }

    public Task<List<CustomerApplicationEntity>> GetCustomerApplicationsAsync(string customerId)
    {
        var apps = _customerApplications.Values
            .Where(ca => ca.CustomerId == customerId)
            .ToList();
        return Task.FromResult(apps);
    }

    public Task<List<CustomerApplicationEntity>> GetAllCustomerApplicationsAsync()
    {
        return Task.FromResult(_customerApplications.Values.ToList());
    }

    public Task UpsertCustomerApplicationAsync(CustomerApplicationEntity customerApp)
    {
        var key = $"{customerApp.CustomerId}_{customerApp.ApplicationId}";
        _customerApplications[key] = customerApp;
        return Task.CompletedTask;
    }
}

