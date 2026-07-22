using DeploymentAPI.Models;
using DeploymentAPI.Repositories;
using Microsoft.Extensions.Logging;

namespace DeploymentAPI.Services;

public class WorkflowSyncService : IWorkflowSyncService
{
    private readonly IGitHubService _gitHubService;
    private readonly IDeploymentRepository _repository;
    private readonly ILogger<WorkflowSyncService> _logger;

    public WorkflowSyncService(
        IGitHubService gitHubService,
        IDeploymentRepository repository,
        ILogger<WorkflowSyncService> logger)
    {
        _gitHubService = gitHubService;
        _repository = repository;
        _logger = logger;
    }

    public async Task<WorkflowSyncResult> SyncLatestWorkflowRunAsync()
    {
        _logger.LogInformation("Starting sync of latest workflow run");

        try
        {
            // Get all workflow runs and find the latest "Update all customers"
            var runs = await _gitHubService.GetWorkflowRunsAsync(null);
            var latestRun = runs
                .Where(r => r.Name == "Update all customers" && r.Status == "completed")
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefault();

            if (latestRun == null)
            {
                _logger.LogWarning("No completed 'Update all customers' workflow run found");
                return new WorkflowSyncResult
                {
                    Errors = new List<string> { "No completed workflow run found" }
                };
            }

            _logger.LogInformation("Found latest run: #{RunNumber} (ID: {RunId})", latestRun.RunNumber, latestRun.Id);
            return await SyncWorkflowRunByIdAsync(latestRun.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing latest workflow run");
            return new WorkflowSyncResult
            {
                Errors = new List<string> { $"Exception: {ex.Message}" }
            };
        }
    }

    public async Task<WorkflowSyncResult> SyncWorkflowRunByIdAsync(long runId)
    {
        var result = new WorkflowSyncResult
        {
            WorkflowRunId = runId
        };

        try
        {
            _logger.LogInformation("Syncing workflow run {RunId}", runId);

            // Get workflow run to extract run number and timestamp
            var workflowRun = await _gitHubService.GetWorkflowRunByIdAsync(runId);
            if (workflowRun == null)
            {
                result.Errors.Add($"Workflow run {runId} not found");
                return result;
            }

            result.RunNumber = workflowRun.RunNumber;
            var deploymentTime = workflowRun.UpdatedAt;

            // Get current CI/CD version (or use a default)
            var cicdVersion = await _repository.GetCurrentCiCdVersionAsync();
            var deploymentVersion = cicdVersion?.Version ?? "Unknown";

            // Get all jobs directly to parse customer and application info
            var jobsResponse = await _gitHubService.GetWorkflowRunJobsAsync(runId);

            _logger.LogInformation("Processing {Count} jobs from run #{RunNumber}", 
                jobsResponse.Jobs.Count, workflowRun.RunNumber);

            // Process each job
            foreach (var job in jobsResponse.Jobs)
            {
                if (string.IsNullOrEmpty(job.Name))
                    continue;

                try
                {
                    await ProcessJobAsync(job, deploymentVersion, deploymentTime, result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing job {JobName}", job.Name);
                    result.Errors.Add($"Job {job.Name}: {ex.Message}");
                }
            }

            _logger.LogInformation(
                "Sync completed: {Created} created, {Updated} updated, {Deployments} deployments recorded",
                result.CustomersCreated, result.CustomersUpdated, result.DeploymentsRecorded);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing workflow run {RunId}", runId);
            result.Errors.Add($"Exception: {ex.Message}");
            return result;
        }
    }

    private async Task ProcessJobAsync(
        GitHubWorkflowJob job,
        string version,
        DateTime deploymentTime,
        WorkflowSyncResult result)
    {
        // Parse job name to extract customer and application info
        var parsed = ParseJobName(job.Name);
        if (!parsed.HasValue)
        {
            // Fall back to old pattern for backward compatibility
            _logger.LogDebug("Job name doesn't match new pattern, skipping: {JobName}", job.Name);
            return;
        }

        var (customerId, customerName, applicationId, applicationName) = parsed.Value;

        // Determine deployment status from job
        var executeStep = job.Steps.FirstOrDefault(s => s.Name == "Execute update");
        bool isSuccess = executeStep?.Conclusion == "success";
        var deploymentStatus = isSuccess ? "Success" : "Failed";

        var installedAt = job.CompletedAt ?? deploymentTime;

        _logger.LogInformation("Processing: Customer={Customer}, App={App}, Status={Status}", 
            customerName, applicationName, deploymentStatus);

        // 1. Upsert Customer entity
        var existingCustomer = await _repository.GetCustomerAsync(customerId);
        if (existingCustomer == null)
        {
            var customer = new Repositories.Entities.CustomerEntity(customerId, customerName);
            await _repository.UpsertCustomerAsync(customer);
            _logger.LogInformation("Created new customer: {CustomerId}", customerId);
            result.CustomersCreated++;
        }
        else
        {
            existingCustomer.CustomerName = customerName; // Update name if changed
            existingCustomer.UpdatedAt = DateTime.UtcNow;
            await _repository.UpsertCustomerAsync(existingCustomer);
            result.CustomersUpdated++;
        }

        result.CustomersProcessed++;

        // 2. Upsert Application entity
        var existingApp = await _repository.GetApplicationAsync(applicationId);
        if (existingApp == null)
        {
            var app = new Repositories.Entities.ApplicationEntity(applicationId, applicationName, version);
            await _repository.UpsertApplicationAsync(app);
            _logger.LogInformation("Created new application: {ApplicationId}", applicationId);
        }
        else
        {
            // Update application name and latest version if needed
            existingApp.ApplicationName = applicationName;
            existingApp.LatestVersion = version;
            await _repository.UpsertApplicationAsync(existingApp);
        }

        // 3. Upsert CustomerApplication junction entity
        var customerApp = new Repositories.Entities.CustomerApplicationEntity(
            customerId,
            customerName,
            applicationId,
            applicationName,
            isSuccess ? version : null, // Only set version if successful
            isSuccess ? installedAt : null,
            deploymentStatus);

        // Get CI/CD target version and app latest version
        var cicdVersion = await _repository.GetCurrentCiCdVersionAsync();
        customerApp.CiCdTargetVersion = cicdVersion?.Version;
        customerApp.LatestVersion = version;
        customerApp.LastDeploymentAttempt = installedAt;

        await _repository.UpsertCustomerApplicationAsync(customerApp);

        // 4. Continue to register deployment for history/audit
        var deployment = new DeploymentRecord
        {
            ClientId = customerId,
            ClientName = customerName,
            ApplicationId = applicationId,
            ApplicationName = applicationName,
            Version = version,
            DeploymentTime = installedAt,
            Status = isSuccess ? DeploymentStatus.Success : DeploymentStatus.Failed
        };

        await _repository.RegisterDeploymentAsync(deployment);
        result.DeploymentsRecorded++;

        _logger.LogInformation(
            "Synced: {CustomerId}/{ApplicationId} v{Version} - {Status}",
            customerId,
            applicationId,
            version,
            deploymentStatus);
    }

    /// <summary>
    /// Parses job name to extract customer and application information.
    /// Expected pattern: "Update {CustomerName} / Update {ApplicationName}"
    /// </summary>
    private (string customerId, string customerName, string applicationId, string applicationName)? 
        ParseJobName(string jobName)
    {
        try
        {
            // Pattern: "Update CustomerName / Update ApplicationName"
            var pattern = @"^Update\s+(.+?)\s+/\s+Update\s+(.+)$";
            var match = System.Text.RegularExpressions.Regex.Match(jobName, pattern);

            if (!match.Success)
            {
                _logger.LogWarning("Job name does not match expected pattern: {JobName}", jobName);
                return null;
            }

            var customerName = match.Groups[1].Value.Trim();
            var applicationName = match.Groups[2].Value.Trim();

            // Generate IDs (lowercase, no spaces)
            var customerId = customerName.ToLowerInvariant().Replace(" ", "");
            var applicationId = applicationName.ToLowerInvariant().Replace(" ", "");

            return (customerId, customerName, applicationId, applicationName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing job name: {JobName}", jobName);
            return null;
        }
    }
}
