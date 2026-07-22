using DeploymentAPI.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Octokit;

namespace DeploymentAPI.Services;

public class OctokitGitHubService : IGitHubService
{
    private readonly ILogger<OctokitGitHubService> _logger;
    private readonly IGitHubAuthProvider _authProvider;
    private readonly string _owner;
    private readonly string _repo;
    private GitHubClient? _client;

    public OctokitGitHubService(
        IConfiguration configuration,
        ILogger<OctokitGitHubService> logger,
        IGitHubAuthProvider authProvider)
    {
        _logger = logger;
        _authProvider = authProvider;

        _owner = configuration["GitHub:Owner"] ?? "AdaptiveBS";
        _repo = configuration["GitHub:Repository"] ?? "CIApp";

        _logger.LogInformation("OctokitGitHubService initialized with {AuthType} authentication",
            _authProvider.GetAuthenticationType());
    }

    private async Task<GitHubClient> GetAuthenticatedClientAsync()
    {
        if (_client == null)
        {
            var token = await _authProvider.GetAuthenticationTokenAsync();
            _client = new GitHubClient(new ProductHeaderValue("DeploymentAPI"))
            {
                Credentials = new Credentials(token)
            };
        }

        return _client;
    }

    public async Task<IEnumerable<GitHubWorkflowRun>> GetWorkflowRunsAsync(string? clientName = null)
    {
        try
        {
            var client = await GetAuthenticatedClientAsync();

            _logger.LogInformation("Fetching workflow runs for {Owner}/{Repo}", _owner, _repo);

            var request = new WorkflowRunsRequest();
            var response = await client.Actions.Workflows.Runs.List(_owner, _repo, request);

            var runs = response.WorkflowRuns.Select(MapToGitHubWorkflowRun).ToList();

            // Filter by client name if provided
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
            var client = await GetAuthenticatedClientAsync();

            _logger.LogInformation("Fetching workflow run {RunId}", runId);

            var run = await client.Actions.Workflows.Runs.Get(_owner, _repo, runId);

            return MapToGitHubWorkflowRun(run);
        }
        catch (NotFoundException)
        {
            _logger.LogWarning("Workflow run {RunId} not found", runId);
            return null;
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
            var client = await GetAuthenticatedClientAsync();

            _logger.LogInformation("Fetching workflows for {Owner}/{Repo}", _owner, _repo);

            var response = await client.Actions.Workflows.List(_owner, _repo);

            var workflows = response.Workflows.Select(w => new GitHubWorkflow
            {
                Id = w.Id,
                NodeId = w.NodeId,
                Name = w.Name,
                Path = w.Path,
                State = w.State.StringValue,
                CreatedAt = w.CreatedAt.UtcDateTime,
                UpdatedAt = w.UpdatedAt.UtcDateTime,
                HtmlUrl = w.HtmlUrl
            }).ToList();

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
            var client = await GetAuthenticatedClientAsync();

            _logger.LogInformation("Fetching repository info for {Owner}/{Repo}", _owner, _repo);

            var repo = await client.Repository.Get(_owner, _repo);

            return new GitHubRepository
            {
                Id = repo.Id,
                NodeId = repo.NodeId,
                Name = repo.Name,
                FullName = repo.FullName,
                Private = repo.Private,
                HtmlUrl = repo.HtmlUrl,
                Description = repo.Description,
                CreatedAt = repo.CreatedAt.UtcDateTime,
                UpdatedAt = repo.UpdatedAt.UtcDateTime,
                PushedAt = repo.PushedAt.HasValue ? repo.PushedAt.Value.UtcDateTime : DateTime.MinValue,
                DefaultBranch = repo.DefaultBranch
            };
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
            var client = await GetAuthenticatedClientAsync();

            _logger.LogInformation("Fetching workflow run jobs for run {RunId}", runId);

            var request = new WorkflowRunJobsRequest();
            var response = await client.Actions.Workflows.Jobs.List(_owner, _repo, runId, request);

            var jobs = response.Jobs.Select(MapToGitHubWorkflowJob).ToList();

            _logger.LogInformation("Retrieved {Count} jobs for workflow run {RunId}", jobs.Count, runId);

            return new GitHubWorkflowJobsResponse
            {
                TotalCount = jobs.Count,
                Jobs = jobs
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

    private static GitHubWorkflowRun MapToGitHubWorkflowRun(WorkflowRun run)
    {
        return new GitHubWorkflowRun
        {
            Id = run.Id,
            Name = run.Name,
            NodeId = run.NodeId,
            HeadBranch = run.HeadBranch,
            HeadSha = run.HeadSha,
            Path = run.Path,
            DisplayTitle = run.DisplayTitle,
            RunNumber = (int)run.RunNumber,
            Event = run.Event,
            Status = run.Status.StringValue,
            Conclusion = run.Conclusion?.StringValue,
            WorkflowId = run.WorkflowId,
            CreatedAt = run.CreatedAt.UtcDateTime,
            UpdatedAt = run.UpdatedAt.UtcDateTime,
            RunStartedAt = run.RunStartedAt.UtcDateTime,
            HtmlUrl = run.HtmlUrl,
            JobsUrl = run.JobsUrl,
            LogsUrl = run.LogsUrl,
            ArtifactsUrl = run.ArtifactsUrl,
            CancelUrl = run.CancelUrl,
            RerunUrl = run.RerunUrl,
            Actor = run.Actor != null ? new GitHubUser
            {
                Login = run.Actor.Login,
                Id = run.Actor.Id,
                AvatarUrl = run.Actor.AvatarUrl,
                HtmlUrl = run.Actor.HtmlUrl
            } : null,
            TriggeringActor = run.TriggeringActor != null ? new GitHubUser
            {
                Login = run.TriggeringActor.Login,
                Id = run.TriggeringActor.Id,
                AvatarUrl = run.TriggeringActor.AvatarUrl,
                HtmlUrl = run.TriggeringActor.HtmlUrl
            } : null
        };
    }

    private static GitHubWorkflowJob MapToGitHubWorkflowJob(WorkflowJob job)
    {
        return new GitHubWorkflowJob
        {
            Id = job.Id,
            RunId = job.RunId,
            RunUrl = job.RunUrl,
            NodeId = job.NodeId,
            HeadSha = job.HeadSha,
            Url = job.Url,
            HtmlUrl = job.HtmlUrl,
            Status = job.Status.StringValue,
            Conclusion = job.Conclusion?.StringValue,
            StartedAt = job.StartedAt.UtcDateTime,
            CompletedAt = job.CompletedAt?.UtcDateTime,
            Name = job.Name,
            CheckRunUrl = job.CheckRunUrl,
            Labels = job.Labels.ToList(),
            RunnerId = job.RunnerId,
            RunnerName = job.RunnerName,
            RunnerGroupId = job.RunnerGroupId,
            RunnerGroupName = job.RunnerGroupName,
            Steps = job.Steps.Select(s => new GitHubWorkflowStep
            {
                Name = s.Name,
                Status = s.Status.StringValue,
                Conclusion = s.Conclusion?.StringValue,
                Number = s.Number,
                StartedAt = s.StartedAt?.UtcDateTime,
                CompletedAt = s.CompletedAt?.UtcDateTime
            }).ToList()
        };
    }
}
