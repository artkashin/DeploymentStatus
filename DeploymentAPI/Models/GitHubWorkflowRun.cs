using System.Text.Json.Serialization;

namespace DeploymentAPI.Models;

public class GitHubWorkflowRun
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("node_id")]
    public string? NodeId { get; set; }

    [JsonPropertyName("head_branch")]
    public string? HeadBranch { get; set; }

    [JsonPropertyName("head_sha")]
    public string? HeadSha { get; set; }

    [JsonPropertyName("path")]
    public string? Path { get; set; }

    [JsonPropertyName("display_title")]
    public string? DisplayTitle { get; set; }

    [JsonPropertyName("run_number")]
    public int RunNumber { get; set; }

    [JsonPropertyName("event")]
    public string? Event { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("conclusion")]
    public string? Conclusion { get; set; }

    [JsonPropertyName("workflow_id")]
    public long WorkflowId { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("run_started_at")]
    public DateTime? RunStartedAt { get; set; }

    [JsonPropertyName("html_url")]
    public string? HtmlUrl { get; set; }

    [JsonPropertyName("jobs_url")]
    public string? JobsUrl { get; set; }

    [JsonPropertyName("logs_url")]
    public string? LogsUrl { get; set; }

    [JsonPropertyName("artifacts_url")]
    public string? ArtifactsUrl { get; set; }

    [JsonPropertyName("cancel_url")]
    public string? CancelUrl { get; set; }

    [JsonPropertyName("rerun_url")]
    public string? RerunUrl { get; set; }

    [JsonPropertyName("actor")]
    public GitHubUser? Actor { get; set; }

    [JsonPropertyName("triggering_actor")]
    public GitHubUser? TriggeringActor { get; set; }
}

public class GitHubWorkflowRunsResponse
{
    [JsonPropertyName("total_count")]
    public int TotalCount { get; set; }

    [JsonPropertyName("workflow_runs")]
    public List<GitHubWorkflowRun> WorkflowRuns { get; set; } = new();
}

public class GitHubUser
{
    [JsonPropertyName("login")]
    public string? Login { get; set; }

    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("avatar_url")]
    public string? AvatarUrl { get; set; }

    [JsonPropertyName("html_url")]
    public string? HtmlUrl { get; set; }
}
