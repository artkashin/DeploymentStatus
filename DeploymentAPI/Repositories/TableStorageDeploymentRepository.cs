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

        // Create tables if they don't exist
        _deploymentsTable.CreateIfNotExists();
        _deploymentHistoryTable.CreateIfNotExists();
        _cicdVersionTable.CreateIfNotExists();

        _logger.LogInformation("TableStorageDeploymentRepository initialized with Deployments + DeploymentHistory tables");
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
            // Query current deployments for this client (from Deployments table)
            var query = _deploymentsTable.QueryAsync<DeploymentEntity>(
                filter: $"PartitionKey eq '{clientId}'");

            var deployments = new List<DeploymentEntity>();
            await foreach (var entity in query)
            {
                deployments.Add(entity);
            }

            if (!deployments.Any())
                return null;

            var clientName = deployments.First().ClientName;

            // Convert to application statuses
            var applicationGroups = deployments
                .Select(d => new ApplicationStatus
                {
                    ApplicationId = d.ApplicationId,
                    ApplicationName = d.ApplicationName,
                    CurrentVersion = d.Version,
                    LastDeploymentTime = d.DeploymentTime,
                    LastDeploymentStatus = (DeploymentStatus)d.Status
                })
                .ToList();

            var versions = applicationGroups
                .Where(a => !string.IsNullOrEmpty(a.CurrentVersion))
                .Select(a => a.CurrentVersion!)
                .ToList();

            // Get current CI/CD version
            var cicdVersion = await GetCurrentCiCdVersionAsync();

            return new ClientStatusResponse
            {
                ClientId = clientId,
                ClientName = clientName,
                MaxVersion = versions.Any() ? versions.Max() : null,
                MinVersion = versions.Any() ? versions.Min() : null,
                CiCdVersion = cicdVersion?.Version,
                Applications = applicationGroups
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
            // Get all unique client IDs from current deployments
            var allDeployments = _deploymentsTable.QueryAsync<DeploymentEntity>();
            var clientIds = new HashSet<string>();

            await foreach (var entity in allDeployments)
            {
                clientIds.Add(entity.ClientId);
            }

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

            return response.Value.ToCiCdVersion();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting CI/CD version from Table Storage");
            throw;
        }
    }
}

