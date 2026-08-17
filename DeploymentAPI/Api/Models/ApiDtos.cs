namespace DeploymentStatus.Api.Models;

public sealed record MeResponse(string Name, bool IsAdaptive, IReadOnlyList<string> CustomerIds);
public sealed record DeploymentSourceDto(string? Repository, string? Workflow, long? RunId, int? RunAttempt,
    string? JobName, string? RunUrl, string? Branch, string? CommitSha, string? Actor, long? ArtifactRunId,
    string? RunnerLabel, string? ServiceName);
public sealed record DeploymentOperationDto(string Scope, string? TenantId, string? TenantLabel,
    string? ApplicationId, string ApplicationName, string? Publisher, string? PreviousVersion,
    string? TargetVersion, string? ObservedVersion, string Action, DeploymentOutcome Outcome,
    long? DurationMs, string? Message, string? InternalError);
public sealed record DeploymentEventDto(string EventId, DeploymentSourceDto Source, DeploymentCustomer Customer,
    DeploymentMode Mode, DeploymentRunStatus Status, DateTimeOffset StartedAt, DateTimeOffset CompletedAt,
    string DetailCompleteness, DeploymentSummary Summary, ArtifactSourceReferenceDto? ArtifactSource, IReadOnlyList<DeploymentOperationDto>? Operations);
public sealed record DeploymentPageResponse(IReadOnlyList<DeploymentEventDto> Items, string? NextCursor);
public sealed record ArtifactSourceReferenceDto(string Branch, string BcVersion, long? RunId, string? RunUrl,
    string? ArtifactName, string? PackageVersion, bool Usable, string? Conclusion, string? Warning);
public sealed record ArtifactSourceDto(string SourceId, string Repository, string Workflow, string Branch, string BcVersion,
    long RunId, int RunAttempt, string? RunUrl, string? CommitSha, DateTimeOffset CompletedAt, string? Conclusion,
    string? ArtifactName, string? PackageVersion, bool ArtifactAvailable, bool Usable, string? Warning);

public static class DeploymentMapping
{
    public static DeploymentEventDto ToDto(this DeploymentEvent item, bool adaptive, bool includeOperations)
    {
        var source = adaptive
            ? new DeploymentSourceDto(item.Source.Repository, item.Source.Workflow, item.Source.RunId,
                item.Source.RunAttempt, item.Source.JobName, item.Source.RunUrl, item.Source.Branch, item.Source.CommitSha,
                item.Source.Actor, item.Source.ArtifactRunId, item.Source.RunnerLabel, item.Source.ServiceName)
            : new DeploymentSourceDto(null, null, null, null,
                null, null, null, null, null, null, null, null);
        IReadOnlyList<DeploymentOperationDto>? operations = includeOperations
            ? item.Operations.Select(operation => new DeploymentOperationDto(operation.Scope,
                operation.TenantId, operation.TenantLabel, operation.ApplicationId,
                operation.ApplicationName, operation.Publisher, operation.PreviousVersion,
                operation.TargetVersion, operation.ObservedVersion, operation.Action,
                operation.Outcome, operation.DurationMs, operation.SafeMessage,
                adaptive ? operation.InternalError : null)).ToList()
            : null;
        ArtifactSourceReferenceDto? artifactSource = item.ArtifactSource is null ? null : new ArtifactSourceReferenceDto(
            item.ArtifactSource.Branch, item.ArtifactSource.BcVersion,
            adaptive ? item.ArtifactSource.RunId : null, adaptive ? item.ArtifactSource.RunUrl : null,
            adaptive ? item.ArtifactSource.ArtifactName : null, item.ArtifactSource.PackageVersion,
            item.ArtifactSource.Usable, adaptive ? item.ArtifactSource.Conclusion : null, item.ArtifactSource.Warning);
        return new DeploymentEventDto(item.EventId, source, item.Customer, item.Mode, item.Status,
            item.StartedAt, item.CompletedAt, item.DetailCompleteness, item.Summary, artifactSource, operations);
    }

    public static ArtifactSourceDto ToDto(this ArtifactSource item) => new(item.SourceId, item.Repository, item.Workflow,
        item.Branch, item.BcVersion, item.RunId, item.RunAttempt, item.RunUrl, item.CommitSha, item.CompletedAt,
        item.Conclusion, item.ArtifactName, item.PackageVersion, item.ArtifactAvailable, item.Usable, item.Warning);
}
