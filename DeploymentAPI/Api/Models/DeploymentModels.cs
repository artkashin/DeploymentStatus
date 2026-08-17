using System.Text.Json.Serialization;

namespace DeploymentStatus.Api.Models;

[JsonConverter(typeof(DeploymentModeConverter))]
public enum DeploymentMode
{
    [JsonStringEnumMemberName("execute")] Execute,
    [JsonStringEnumMemberName("dryRun")] DryRun
}
[JsonConverter(typeof(DeploymentRunStatusConverter))]
public enum DeploymentRunStatus
{
    [JsonStringEnumMemberName("success")] Success,
    [JsonStringEnumMemberName("partial")] Partial,
    [JsonStringEnumMemberName("failed")] Failed,
    [JsonStringEnumMemberName("cancelled")] Cancelled,
    [JsonStringEnumMemberName("skipped")] Skipped
}
[JsonConverter(typeof(DeploymentOutcomeConverter))]
public enum DeploymentOutcome
{
    [JsonStringEnumMemberName("success")] Success,
    [JsonStringEnumMemberName("failed")] Failed,
    [JsonStringEnumMemberName("alreadyCurrent")] AlreadyCurrent,
    [JsonStringEnumMemberName("newerPresent")] NewerPresent,
    [JsonStringEnumMemberName("excluded")] Excluded,
    [JsonStringEnumMemberName("planned")] Planned,
    [JsonStringEnumMemberName("skipped")] Skipped
}

public sealed class DeploymentModeConverter() : JsonStringEnumConverter<DeploymentMode>(null, false);
public sealed class DeploymentRunStatusConverter() : JsonStringEnumConverter<DeploymentRunStatus>(null, false);
public sealed class DeploymentOutcomeConverter() : JsonStringEnumConverter<DeploymentOutcome>(null, false);

public sealed class DeploymentEvent
{
    public string SchemaVersion { get; set; } = "1.0";
    public required string EventId { get; set; }
    public required DeploymentSource Source { get; set; }
    public required DeploymentCustomer Customer { get; set; }
    public DeploymentMode Mode { get; set; }
    public DeploymentRunStatus Status { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset CompletedAt { get; set; }
    public string DetailCompleteness { get; set; } = "full";
    public DeploymentSummary Summary { get; set; } = new();
    public List<DeploymentOperation> Operations { get; set; } = [];
}

public sealed class DeploymentSource
{
    public required string Repository { get; set; }
    public required string Workflow { get; set; }
    public long RunId { get; set; }
    public int RunAttempt { get; set; } = 1;
    public string? JobName { get; set; }
    public string? RunUrl { get; set; }
    public string? Branch { get; set; }
    public string? CommitSha { get; set; }
    public string? Actor { get; set; }
    public long? ArtifactRunId { get; set; }
    public string? RunnerLabel { get; set; }
    public string? ServiceName { get; set; }
}

public sealed class DeploymentCustomer
{
    public required string Id { get; set; }
    public required string Name { get; set; }
}

public sealed class DeploymentSummary
{
    public int Total { get; set; }
    public int Succeeded { get; set; }
    public int Failed { get; set; }
    public int Skipped { get; set; }
    public int Planned { get; set; }
}

public sealed class DeploymentOperation
{
    public string Scope { get; set; } = "tenant";
    public string? TenantId { get; set; }
    public string? TenantLabel { get; set; }
    public string? ApplicationId { get; set; }
    public required string ApplicationName { get; set; }
    public string? Publisher { get; set; }
    public string? PreviousVersion { get; set; }
    public string? TargetVersion { get; set; }
    public string? ObservedVersion { get; set; }
    public required string Action { get; set; }
    public DeploymentOutcome Outcome { get; set; }
    public long? DurationMs { get; set; }
    public string? SafeMessage { get; set; }
    public string? InternalError { get; set; }
}

public sealed record DeploymentQuery(IReadOnlySet<string>? CustomerIds, string? Status, string? Mode,
    string? Workflow, string? Branch, DateTimeOffset? From, DateTimeOffset? To, int Offset, int PageSize);
public sealed record PagedDeployments(IReadOnlyList<DeploymentEvent> Items, string? NextCursor);

public sealed class CustomerLatestStatus
{
    public required string CustomerId { get; init; }
    public required string CustomerName { get; init; }
    public required string EventId { get; init; }
    public DeploymentRunStatus Status { get; init; }
    public DeploymentMode Mode { get; init; }
    public DateTimeOffset CompletedAt { get; init; }
    public DeploymentSummary Summary { get; init; } = new();
}

public sealed class CurrentDeploymentState
{
    public required string CustomerId { get; init; }
    public required string TenantId { get; init; }
    public string? TenantLabel { get; init; }
    public required string ApplicationId { get; init; }
    public required string ApplicationName { get; init; }
    public string? Version { get; init; }
    public DeploymentOutcome LastOutcome { get; init; }
    public DateTimeOffset VerifiedAt { get; init; }
    public required string EventId { get; init; }
}
