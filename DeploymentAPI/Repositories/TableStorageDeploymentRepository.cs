using Azure;
using Azure.Data.Tables;
using DeploymentAPI.Models;
using DeploymentAPI.Repositories.Entities;
using Microsoft.Extensions.Logging;

namespace DeploymentAPI.Repositories;

public class TableStorageDeploymentRepository : IDeploymentRepository
{
    private readonly TableClient _deploymentsTable;
    private readonly TableClient _deploymentHistoryTable;
    private readonly TableClient _cicdVersionTable;
    private readonly TableClient _customersTable;
    private readonly TableClient _applicationsTable;
    private readonly TableClient _customerApplicationsTable;
    private readonly ILogger<TableStorageDeploymentRepository> _logger;

    public TableStorageDeploymentRepository(
        string connectionString,
        ILogger<TableStorageDeploymentRepository> logger)
    {
        _logger = logger;

        // Initialize table clients
        _deploymentsTable = new TableClient(connectionString, "Deployments");
        _deploymentHistoryTable = new TableClient(connectionString, "DeploymentHistory");
        _cicdVersionTable = new TableClient(connectionString, "CiCdVersion");
        _customersTable = new TableClient(connectionString, "Customers");
        _applicationsTable = new TableClient(connectionString, "Applications");
        _customerApplicationsTable = new TableClient(connectionString, "CustomerApplications");

        // Create tables if they don't exist
        _deploymentsTable.CreateIfNotExists();
        _deploymentHistoryTable.CreateIfNotExists();
        _cicdVersionTable.CreateIfNotExists();
        _customersTable.CreateIfNotExists();
        _applicationsTable.CreateIfNotExists();
        _customerApplicationsTable.CreateIfNotExists();

        _logger.LogInformation("TableStorageDeploymentRepository initialized with 6 tables: Deployments, DeploymentHistory, CiCdVersion, Customers, Applications, CustomerApplications");
    }

    public async Task RegisterDeploymentAsync(DeploymentRecord deployment)
    {
        try
        {
            deployment.DeploymentTime = DateTime.UtcNow;

            // 1. Check if current version is the same (in Deployments table)
            var currentEntity = await GetCurrentDeploymentEntityAsync(deployment.ClientId, deployment.ApplicationId);
            
            bool shouldUpdateCurrent = currentEntity == null || currentEntity.Version != deployment.Version;

            // 2. Always add to history
            var historyEntity = new DeploymentHistoryEntity(deployment);
            await _deploymentHistoryTable.AddEntityAsync(historyEntity);
            
            _logger.LogInformation(
                "Added to history: {ClientId}/{ApplicationId} v{Version}",
                deployment.ClientId, deployment.ApplicationId, deployment.Version);

            // 3. Update current state only if version changed
            if (shouldUpdateCurrent)
            {
                var currentDeploymentEntity = new DeploymentEntity(deployment);
                await _deploymentsTable.UpsertEntityAsync(currentDeploymentEntity, TableUpdateMode.Replace);
                
                _logger.LogInformation(
                    "Updated current state: {ClientId}/{ApplicationId} v{Version}",
                    deployment.ClientId, deployment.ApplicationId, deployment.Version);
            }
            else
            {
                _logger.LogInformation(
                    "Skipped current state update (same version): {ClientId}/{ApplicationId} v{Version}",
                    deployment.ClientId, deployment.ApplicationId, deployment.Version);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering deployment to Table Storage");
            throw;
        }
    }

    private async Task<DeploymentEntity?> GetCurrentDeploymentEntityAsync(string clientId, string applicationId)
    {
        try
        {
            var response = await _deploymentsTable.GetEntityIfExistsAsync<DeploymentEntity>(
                partitionKey: clientId,
                rowKey: applicationId);

            return response.HasValue ? response.Value : null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<ClientStatusResponse?> GetClientStatusAsync(string clientId)
    {
        try
        {
            // Get customer entity
            var customer = await GetCustomerAsync(clientId);
            if (customer == null)
                return null;

            // Get all applications for this customer
            var customerApps = await GetCustomerApplicationsAsync(clientId);
            if (!customerApps.Any())
                return null;

            // Get current CI/CD version
            var cicdVersion = await GetCurrentCiCdVersionAsync();
            var cicdTargetVersion = cicdVersion?.Version;

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting client status from Table Storage for {ClientId}", clientId);
            throw;
        }
    }

    public async Task<AllClientsStatusResponse> GetAllClientsStatusAsync()
    {
        try
        {
            // Get all customers from the Customers table
            var customers = await GetAllCustomersAsync();

            var clientStatuses = new List<ClientStatusResponse>();

            foreach (var customer in customers)
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all clients status from Table Storage");
            throw;
        }
    }

    public async Task<List<DeploymentRecord>> GetDeploymentHistoryAsync(
        string clientId, 
        string? applicationId = null, 
        int limit = 100)
    {
        try
        {
            // Query from history table
            var filter = $"PartitionKey eq '{clientId}'";
            
            if (!string.IsNullOrEmpty(applicationId))
            {
                filter += $" and ApplicationId eq '{applicationId}'";
            }

            var query = _deploymentHistoryTable.QueryAsync<DeploymentHistoryEntity>(filter: filter);

            var deployments = new List<DeploymentHistoryEntity>();
            await foreach (var entity in query)
            {
                deployments.Add(entity);
                if (deployments.Count >= limit)
                    break;
            }

            // History is already sorted by RowKey (reverse timestamp), so newest first
            return deployments
                .Select(e => e.ToDeploymentRecord())
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting deployment history from Table Storage");
            throw;
        }
    }

    public async Task UpdateCiCdVersionAsync(CiCdVersion ciCdVersion)
    {
        try
        {
            ciCdVersion.UpdatedAt = DateTime.UtcNow;
            var entity = new CiCdVersionEntity(ciCdVersion);
            
            await _cicdVersionTable.UpsertEntityAsync(entity, TableUpdateMode.Replace);
            
            _logger.LogInformation("Updated CI/CD version to {Version}", ciCdVersion.Version);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating CI/CD version in Table Storage");
            throw;
        }
    }

    public async Task<CiCdVersion?> GetCurrentCiCdVersionAsync()
    {
        try
        {
            var response = await _cicdVersionTable.GetEntityIfExistsAsync<CiCdVersionEntity>(
                partitionKey: "CiCdVersion",
                rowKey: "Current");

            if (!response.HasValue)
                return null;

            return response.Value?.ToCiCdVersion();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting CI/CD version from Table Storage");
            throw;
        }
    }

    // Customer Management Methods
    public async Task<CustomerEntity?> GetCustomerAsync(string customerId)
    {
        try
        {
            var response = await _customersTable.GetEntityIfExistsAsync<CustomerEntity>(
                partitionKey: "Customer",
                rowKey: customerId);

            return response.HasValue ? response.Value : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer {CustomerId}", customerId);
            throw;
        }
    }

    public async Task<List<CustomerEntity>> GetAllCustomersAsync()
    {
        try
        {
            var customers = new List<CustomerEntity>();
            var query = _customersTable.QueryAsync<CustomerEntity>(filter: $"PartitionKey eq 'Customer'");

            await foreach (var customer in query)
            {
                customers.Add(customer);
            }

            return customers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all customers");
            throw;
        }
    }

    public async Task UpsertCustomerAsync(CustomerEntity customer)
    {
        try
        {
            customer.UpdatedAt = DateTime.UtcNow;
            await _customersTable.UpsertEntityAsync(customer, TableUpdateMode.Replace);
            _logger.LogInformation("Upserted customer {CustomerId}", customer.CustomerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting customer {CustomerId}", customer.CustomerId);
            throw;
        }
    }

    // Application Management Methods
    public async Task<ApplicationEntity?> GetApplicationAsync(string applicationId)
    {
        try
        {
            var response = await _applicationsTable.GetEntityIfExistsAsync<ApplicationEntity>(
                partitionKey: "Application",
                rowKey: applicationId);

            return response.HasValue ? response.Value : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting application {ApplicationId}", applicationId);
            throw;
        }
    }

    public async Task<List<ApplicationEntity>> GetAllApplicationsAsync()
    {
        try
        {
            var applications = new List<ApplicationEntity>();
            var query = _applicationsTable.QueryAsync<ApplicationEntity>(filter: $"PartitionKey eq 'Application'");

            await foreach (var app in query)
            {
                applications.Add(app);
            }

            return applications;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all applications");
            throw;
        }
    }

    public async Task UpsertApplicationAsync(ApplicationEntity application)
    {
        try
        {
            application.UpdatedAt = DateTime.UtcNow;
            await _applicationsTable.UpsertEntityAsync(application, TableUpdateMode.Replace);
            _logger.LogInformation("Upserted application {ApplicationId}", application.ApplicationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting application {ApplicationId}", application.ApplicationId);
            throw;
        }
    }

    // Customer-Application Relationship Methods
    public async Task<CustomerApplicationEntity?> GetCustomerApplicationAsync(string customerId, string applicationId)
    {
        try
        {
            var response = await _customerApplicationsTable.GetEntityIfExistsAsync<CustomerApplicationEntity>(
                partitionKey: customerId,
                rowKey: applicationId);

            return response.HasValue ? response.Value : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer application {CustomerId}/{ApplicationId}", customerId, applicationId);
            throw;
        }
    }

    public async Task<List<CustomerApplicationEntity>> GetCustomerApplicationsAsync(string customerId)
    {
        try
        {
            var customerApps = new List<CustomerApplicationEntity>();
            var query = _customerApplicationsTable.QueryAsync<CustomerApplicationEntity>(
                filter: $"PartitionKey eq '{customerId}'");

            await foreach (var app in query)
            {
                customerApps.Add(app);
            }

            return customerApps;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting applications for customer {CustomerId}", customerId);
            throw;
        }
    }

    public async Task<List<CustomerApplicationEntity>> GetAllCustomerApplicationsAsync()
    {
        try
        {
            var customerApps = new List<CustomerApplicationEntity>();
            var query = _customerApplicationsTable.QueryAsync<CustomerApplicationEntity>();

            await foreach (var app in query)
            {
                customerApps.Add(app);
            }

            return customerApps;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all customer applications");
            throw;
        }
    }

    public async Task UpsertCustomerApplicationAsync(CustomerApplicationEntity customerApp)
    {
        try
        {
            await _customerApplicationsTable.UpsertEntityAsync(customerApp, TableUpdateMode.Replace);
            _logger.LogInformation("Upserted customer application {CustomerId}/{ApplicationId}", 
                customerApp.CustomerId, customerApp.ApplicationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting customer application {CustomerId}/{ApplicationId}", 
                customerApp.CustomerId, customerApp.ApplicationId);
            throw;
        }
    }
}

