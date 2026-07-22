using DeploymentAPI.Models;

namespace DeploymentAPI.Services;

public interface IGitHubService
{
    Task<IEnumerable<GitHubWorkflowRun>> GetWorkflowRunsAsync(string? clientName = null);
    Task<GitHubWorkflowRun?> GetWorkflowRunByIdAsync(long runId);
    Task<IEnumerable<GitHubWorkflow>> GetWorkflowsAsync();
    Task<GitHubRepository> GetRepositoryInfoAsync();
    Task<GitHubWorkflowJobsResponse> GetWorkflowRunJobsAsync(long runId);
    Task<WorkflowRunCustomerStatusResponse> GetWorkflowRunCustomerStatusAsync(long runId);
}
