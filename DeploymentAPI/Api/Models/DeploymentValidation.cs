namespace DeploymentStatus.Api.Models;

public static class DeploymentValidation
{
    public static IReadOnlyList<string> Validate(DeploymentEvent? item)
    {
        var errors = new List<string>();
        if (item is null) return ["Request body is required."];
        if (item.SchemaVersion != "1.0") errors.Add("schemaVersion must be '1.0'.");
        Required(item.EventId, "eventId", errors, 256);
        Required(item.Customer?.Id, "customer.id", errors, 100);
        Required(item.Customer?.Name, "customer.name", errors, 200);
        if (item.Customer is not null && !System.Text.RegularExpressions.Regex.IsMatch(item.Customer.Id ?? "", "^[a-z0-9][a-z0-9-]{0,99}$"))
            errors.Add("customer.id must be a lowercase slug containing letters, numbers, or hyphens.");
        Required(item.Source?.Repository, "source.repository", errors, 200);
        Required(item.Source?.Workflow, "source.workflow", errors, 200);
        if (item.Source?.RunId <= 0) errors.Add("source.runId must be positive.");
        if (item.Source?.RunAttempt <= 0) errors.Add("source.runAttempt must be positive.");
        if (item.Source is not null && item.Customer is not null && !string.IsNullOrWhiteSpace(item.Customer.Id))
        {
            var mode = item.Mode == DeploymentMode.DryRun ? "dryRun" : "execute";
            var repositoryKey = System.Text.RegularExpressions.Regex.Replace(item.Source.Repository ?? "", "[^A-Za-z0-9._-]", "~");
            var expectedEventId = $"{repositoryKey}:{item.Source.RunId}:{item.Source.RunAttempt}:{item.Customer.Id.ToLowerInvariant()}:{mode}";
            if (!string.Equals(item.EventId, expectedEventId, StringComparison.Ordinal))
                errors.Add("eventId must be repositoryKey:runId:runAttempt:customerId:mode.");
        }
        if (item.StartedAt == default || item.CompletedAt == default) errors.Add("startedAt and completedAt are required.");
        if (item.CompletedAt < item.StartedAt) errors.Add("completedAt cannot precede startedAt.");
        if (item.DetailCompleteness is not ("full" or "summary")) errors.Add("detailCompleteness must be 'full' or 'summary'.");
        if (item.Summary is null) errors.Add("summary is required.");
        else
        {
            var counts = new[] { item.Summary.Total, item.Summary.Succeeded, item.Summary.Failed, item.Summary.Skipped, item.Summary.Planned };
            if (counts.Any(value => value < 0)) errors.Add("summary counts cannot be negative.");
            if (item.Summary.Total != item.Summary.Succeeded + item.Summary.Failed + item.Summary.Skipped + item.Summary.Planned)
                errors.Add("summary.total must equal succeeded + failed + skipped + planned.");
        }
        if (item.Operations is null) { errors.Add("operations is required."); return errors; }
        if (item.Operations.Count > 10000) errors.Add("operations cannot exceed 10000 items.");
        foreach (var operation in item.Operations)
        {
            if (operation is null) { errors.Add("operations cannot contain null items."); continue; }
            Required(operation.Scope, "operations[].scope", errors, 40);
            Required(operation.ApplicationName, "operations[].applicationName", errors, 250);
            Required(operation.Action, "operations[].action", errors, 80);
            if (operation.DurationMs < 0) errors.Add("operations[].durationMs cannot be negative.");
            if (operation.SafeMessage?.Length > 2000) errors.Add("operations[].safeMessage cannot exceed 2000 characters.");
            if (operation.InternalError?.Length > 8192) errors.Add("operations[].internalError cannot exceed 8192 characters.");
        }
        return errors;
    }
    private static void Required(string? value, string name, ICollection<string> errors, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) errors.Add($"{name} is required.");
        else if (value.Length > max) errors.Add($"{name} cannot exceed {max} characters.");
    }
}

public static class ArtifactSourceValidation
{
    public static IReadOnlyList<string> Validate(ArtifactSource? item)
    {
        var errors = new List<string>();
        if (item is null) return ["Request body is required."];
        if (item.SchemaVersion != "1.0") errors.Add("schemaVersion must be '1.0'.");
        Required(item.SourceId, "sourceId", errors, 256);
        Required(item.Repository, "repository", errors, 200); Required(item.Workflow, "workflow", errors, 200);
        Required(item.Branch, "branch", errors, 120); Required(item.BcVersion, "bcVersion", errors, 20);
        if (item.RunId < 0) errors.Add("runId cannot be negative.");
        if (item.RunAttempt <= 0) errors.Add("runAttempt must be positive.");
        if (item.CompletedAt == default) errors.Add("completedAt is required.");
        if (item.Usable && !item.ArtifactAvailable) errors.Add("usable sources must have an available artifact.");
        if (item.ArtifactName?.Length > 500 || item.PackageVersion?.Length > 100 || item.Warning?.Length > 2000)
            errors.Add("artifact source text fields exceed their maximum length.");
        return errors;
    }
    private static void Required(string? value, string name, ICollection<string> errors, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) errors.Add($"{name} is required.");
        else if (value.Length > max) errors.Add($"{name} cannot exceed {max} characters.");
    }
}
