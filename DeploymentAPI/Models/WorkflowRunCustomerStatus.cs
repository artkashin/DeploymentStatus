using System.Text.Json.Serialization;

namespace DeploymentAPI.Models;

public class CustomerInstallationStatus
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("installed")]
    public bool Installed { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("runner")]
    public string? Runner { get; set; }

    [JsonPropertyName("durationSeconds")]
    public int DurationSeconds { get; set; }

    [JsonPropertyName("startedAt")]
    public DateTime? StartedAt { get; set; }

    [JsonPropertyName("completedAt")]
    public DateTime? CompletedAt { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

public class WorkflowRunCustomerStatusResponse
{
    [JsonPropertyName("runId")]
    public long RunId { get; set; }

    [JsonPropertyName("runNumber")]
    public int RunNumber { get; set; }

    [JsonPropertyName("workflowName")]
    public string WorkflowName { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("overallSuccess")]
    public bool OverallSuccess { get; set; }

    [JsonPropertyName("totalCustomers")]
    public int TotalCustomers { get; set; }

    [JsonPropertyName("successfulInstallations")]
    public int SuccessfulInstallations { get; set; }

    [JsonPropertyName("failedInstallations")]
    public int FailedInstallations { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("customers")]
    public List<CustomerInstallationStatus> Customers { get; set; } = new();
}

public class GitHubWorkflowJob
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("run_id")]
    public long RunId { get; set; }

    [JsonPropertyName("workflow_name")]
    public string? WorkflowName { get; set; }

    [JsonPropertyName("head_branch")]
    public string? HeadBranch { get; set; }

    [JsonPropertyName("run_url")]
    public string? RunUrl { get; set; }

    [JsonPropertyName("run_attempt")]
    public int RunAttempt { get; set; }

    [JsonPropertyName("node_id")]
    public string? NodeId { get; set; }

    [JsonPropertyName("head_sha")]
    public string? HeadSha { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("html_url")]
    public string? HtmlUrl { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("conclusion")]
    public string? Conclusion { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("started_at")]
    public DateTime? StartedAt { get; set; }

    [JsonPropertyName("completed_at")]
    public DateTime? CompletedAt { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("steps")]
    public List<GitHubWorkflowStep> Steps { get; set; } = new();

    [JsonPropertyName("check_run_url")]
    public string? CheckRunUrl { get; set; }

    [JsonPropertyName("labels")]
    public List<string> Labels { get; set; } = new();

    [JsonPropertyName("runner_id")]
    public long? RunnerId { get; set; }

    [JsonPropertyName("runner_name")]
    public string? RunnerName { get; set; }

    [JsonPropertyName("runner_group_id")]
    public long? RunnerGroupId { get; set; }

    [JsonPropertyName("runner_group_name")]
    public string? RunnerGroupName { get; set; }
}

public class GitHubWorkflowStep
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("conclusion")]
    public string? Conclusion { get; set; }

    [JsonPropertyName("number")]
    public int Number { get; set; }

    [JsonPropertyName("started_at")]
    public DateTime? StartedAt { get; set; }

    [JsonPropertyName("completed_at")]
    public DateTime? CompletedAt { get; set; }
}

public class GitHubWorkflowJobsResponse
{
    [JsonPropertyName("total_count")]
    public int TotalCount { get; set; }

    [JsonPropertyName("jobs")]
    public List<GitHubWorkflowJob> Jobs { get; set; } = new();
}
