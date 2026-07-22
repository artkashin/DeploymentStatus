using System.Net.Http.Headers;
using System.Text.Json;
using DeploymentAPI.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DeploymentAPI.Services;

public class GitHubService : IGitHubService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GitHubService> _logger;
    private readonly IGitHubAuthProvider _authProvider;
    private readonly string _owner;
    private readonly string _repo;

    public GitHubService(
        HttpClient httpClient, 
        IConfiguration configuration, 
        ILogger<GitHubService> logger,
        IGitHubAuthProvider authProvider)
    {
        _httpClient = httpClient;
        _logger = logger;
        _authProvider = authProvider;

        _owner = configuration["GitHub:Owner"] ?? "AdaptiveBS";
        _repo = configuration["GitHub:Repository"] ?? "CIApp";

        _httpClient.BaseAddress = new Uri("https://api.github.com/");
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("DeploymentAPI", "1.0"));
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        _httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");

        _logger.LogInformation("GitHubService initialized with {AuthType} authentication", 
            _authProvider.GetAuthenticationType());
    }

    private async Task EnsureAuthenticationAsync()
    {
        var token = await _authProvider.GetAuthenticationTokenAsync();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<IEnumerable<GitHubWorkflowRun>> GetWorkflowRunsAsync(string? clientName = null)
    {
        try
        {
            await EnsureAuthenticationAsync();
            
            var endpoint = $"repos/{_owner}/{_repo}/actions/runs?per_page=100";
            
            _logger.LogInformation("Fetching workflow runs from {Endpoint}", endpoint);

            var response = await _httpClient.GetAsync(endpoint);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<GitHubWorkflowRunsResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var runs = result?.WorkflowRuns ?? new List<GitHubWorkflowRun>();

            // Filter by client name if provided (assuming client name is in the workflow name or branch)
            if (!string.IsNullOrEmpty(clientName))
            {
                runs = runs.Where(r => 
                    r.Name?.Contains(clientName, StringComparison.OrdinalIgnoreCase) == true ||
                    r.HeadBranch?.Contains(clientName, StringComparison.OrdinalIgnoreCase) == true
                ).ToList();
            }

            _logger.LogInformation("Retrieved {Count} workflow runs", runs.Count);
            return runs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching workflow runs from GitHub");
            throw;
        }
    }

    public async Task<GitHubWorkflowRun?> GetWorkflowRunByIdAsync(long runId)
    {
        try
        {
            await EnsureAuthenticationAsync();
            
            var endpoint = $"repos/{_owner}/{_repo}/actions/runs/{runId}";
            
            _logger.LogInformation("Fetching workflow run {RunId} from {Endpoint}", runId, endpoint);

            var response = await _httpClient.GetAsync(endpoint);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Workflow run {RunId} not found", runId);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var run = JsonSerializer.Deserialize<GitHubWorkflowRun>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return run;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching workflow run {RunId} from GitHub", runId);
            throw;
        }
    }

    public async Task<IEnumerable<GitHubWorkflow>> GetWorkflowsAsync()
    {
        try
        {
            await EnsureAuthenticationAsync();
            
            var endpoint = $"repos/{_owner}/{_repo}/actions/workflows";
            
            _logger.LogInformation("Fetching workflows from {Endpoint}", endpoint);

            var response = await _httpClient.GetAsync(endpoint);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<GitHubWorkflowsResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var workflows = result?.Workflows ?? new List<GitHubWorkflow>();
            _logger.LogInformation("Retrieved {Count} workflows", workflows.Count);
            
            return workflows;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching workflows from GitHub");
            throw;
        }
    }

    public async Task<GitHubRepository> GetRepositoryInfoAsync()
    {
        try
        {
            await EnsureAuthenticationAsync();
            
            var endpoint = $"repos/{_owner}/{_repo}";
            
            _logger.LogInformation("Fetching repository info from {Endpoint}", endpoint);

            var response = await _httpClient.GetAsync(endpoint);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var repo = JsonSerializer.Deserialize<GitHubRepository>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return repo ?? throw new InvalidOperationException("Failed to deserialize repository info");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching repository info from GitHub");
            throw;
        }
    }

    public async Task<GitHubWorkflowJobsResponse> GetWorkflowRunJobsAsync(long runId)
    {
        try
        {
            await EnsureAuthenticationAsync();

            var allJobs = new List<GitHubWorkflowJob>();
            var page = 1;
            var perPage = 100; // Maximum allowed by GitHub API
            var hasMorePages = true;

            _logger.LogInformation("Fetching workflow run jobs for run {RunId} (paginated)", runId);

            while (hasMorePages)
            {
                var endpoint = $"repos/{_owner}/{_repo}/actions/runs/{runId}/jobs?per_page={perPage}&page={page}";

                var response = await _httpClient.GetAsync(endpoint);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Workflow run jobs for {RunId} not found on page {Page}", runId, page);
                    break;
                }

                var content = await response.Content.ReadAsStringAsync();
                var pageResponse = JsonSerializer.Deserialize<GitHubWorkflowJobsResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (pageResponse?.Jobs != null && pageResponse.Jobs.Any())
                {
                    allJobs.AddRange(pageResponse.Jobs);
                    _logger.LogInformation("Retrieved {Count} jobs from page {Page} for run {RunId}", 
                        pageResponse.Jobs.Count, page, runId);

                    // Check if there are more pages
                    hasMorePages = pageResponse.Jobs.Count == perPage;
                    page++;
                }
                else
                {
                    hasMorePages = false;
                }
            }

            _logger.LogInformation("Retrieved total of {Count} jobs for workflow run {RunId}", allJobs.Count, runId);

            return new GitHubWorkflowJobsResponse
            {
                TotalCount = allJobs.Count,
                Jobs = allJobs
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching workflow run jobs for {RunId} from GitHub", runId);
            throw;
        }
    }

    public async Task<WorkflowRunCustomerStatusResponse> GetWorkflowRunCustomerStatusAsync(long runId)
    {
        try
        {
            _logger.LogInformation("Getting customer status for workflow run {RunId}", runId);

            // Get the workflow run details
            var workflowRun = await GetWorkflowRunByIdAsync(runId);
            if (workflowRun == null)
            {
                throw new InvalidOperationException($"Workflow run {runId} not found");
            }

            // Get all jobs for this run
            var jobsResponse = await GetWorkflowRunJobsAsync(runId);

            var customers = new List<CustomerInstallationStatus>();

            // Parse jobs to extract customer installation status
            // Pattern: "Update {customer} / Update {customer}"
            foreach (var job in jobsResponse.Jobs)
            {
                if (string.IsNullOrEmpty(job.Name))
                    continue;

                // Match pattern: "Update customerName / Update customerName"
                var match = System.Text.RegularExpressions.Regex.Match(
                    job.Name, 
                    @"^Update\s+(\w+)\s+/\s+Update\s+\1$",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                if (match.Success)
                {
                    var customerName = match.Groups[1].Value;

                    // Find the "Execute update" step
                    var executeStep = job.Steps.FirstOrDefault(s => s.Name == "Execute update");
                    bool installed = executeStep?.Conclusion == "success";

                    var duration = 0;
                    if (job.StartedAt.HasValue && job.CompletedAt.HasValue)
                    {
                        duration = (int)(job.CompletedAt.Value - job.StartedAt.Value).TotalSeconds;
                    }

                    customers.Add(new CustomerInstallationStatus
                    {
                        Name = customerName,
                        Installed = installed,
                        Status = job.Conclusion ?? "unknown",
                        Runner = job.RunnerName,
                        DurationSeconds = duration,
                        StartedAt = job.StartedAt,
                        CompletedAt = job.CompletedAt,
                        Url = job.HtmlUrl
                    });

                    _logger.LogInformation("Customer {Customer}: Installed={Installed}, Status={Status}", 
                        customerName, installed, job.Conclusion);
                }
            }

            var successCount = customers.Count(c => c.Installed);
            var failedCount = customers.Count(c => !c.Installed);

            var response = new WorkflowRunCustomerStatusResponse
            {
                RunId = runId,
                RunNumber = workflowRun.RunNumber,
                WorkflowName = workflowRun.Name ?? "Unknown",
                Status = workflowRun.Status ?? "unknown",
                OverallSuccess = failedCount == 0 && successCount > 0,
                TotalCustomers = customers.Count,
                SuccessfulInstallations = successCount,
                FailedInstallations = failedCount,
                Timestamp = DateTime.UtcNow,
                Customers = customers
            };

            _logger.LogInformation("Workflow run {RunId}: {Total} customers, {Success} installed, {Failed} failed",
                runId, customers.Count, successCount, failedCount);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer status for workflow run {RunId}", runId);
            throw;
        }
    }
}

