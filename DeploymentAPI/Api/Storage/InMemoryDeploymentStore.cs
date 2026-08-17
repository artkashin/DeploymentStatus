using System.Collections.Concurrent;
using DeploymentStatus.Api.Models;

namespace DeploymentStatus.Api.Storage;

public sealed class InMemoryDeploymentStore : IDeploymentStore
{
    private readonly ConcurrentDictionary<string, DeploymentEvent> _events = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, CustomerDesiredAppState> _desiredState = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, ArtifactSource> _artifactSources = new(StringComparer.OrdinalIgnoreCase);

    public Task<bool> RegisterAsync(DeploymentEvent item, CancellationToken cancellationToken = default)
    {
        item.Customer.Id = item.Customer.Id.ToLowerInvariant();
        var added = _events.TryAdd(item.EventId, item);
        if (added && item.Mode == DeploymentMode.Execute) UpdateDesiredState(item);
        return Task.FromResult(added);
    }

    public Task<PagedDeployments> QueryAsync(DeploymentQuery query, CancellationToken cancellationToken = default)
    {
        var items = Filter(_events.Values, query).OrderByDescending(item => item.CompletedAt).ThenBy(item => item.EventId).ToList();
        var page = items.Skip(query.Offset).Take(query.PageSize).ToList();
        var next = query.Offset + page.Count < items.Count ? Cursor.Encode(query.Offset + page.Count) : null;
        return Task.FromResult(new PagedDeployments(page, next));
    }

    public Task<DeploymentEvent?> GetAsync(string eventId, CancellationToken cancellationToken = default)
        => Task.FromResult(_events.GetValueOrDefault(eventId));

    public Task<IReadOnlyList<CustomerLatestStatus>> GetCustomersAsync(IReadOnlySet<string>? customerIds, CancellationToken cancellationToken = default)
    {
        var result = _events.Values.Where(item => customerIds is null || customerIds.Contains(item.Customer.Id))
            .GroupBy(item => item.Customer.Id, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderByDescending(item => item.CompletedAt).First())
            .Select(item => WithHealth(new CustomerLatestStatus { CustomerId = item.Customer.Id, CustomerName = item.Customer.Name,
                EventId = item.EventId, Status = item.Status, Mode = item.Mode, CompletedAt = item.CompletedAt, Summary = item.Summary,
                BcVersion = item.ArtifactSource?.BcVersion, PackageVersion = item.ArtifactSource?.PackageVersion }, item.Customer.Id))
            .OrderBy(item => item.CustomerName).Cast<CustomerLatestStatus>().ToList();
        return Task.FromResult<IReadOnlyList<CustomerLatestStatus>>(result);
    }

    public Task<IReadOnlyList<CustomerDesiredAppState>> GetDesiredAppStateAsync(string customerId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<CustomerDesiredAppState>>(_desiredState.Values.Where(item => item.CustomerId.Equals(customerId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => item.TenantId).ThenBy(item => item.ApplicationName).ToList());

    public Task<bool> RegisterArtifactSourceAsync(ArtifactSource item, CancellationToken cancellationToken = default)
        => Task.FromResult(_artifactSources.TryAdd(item.SourceId, item));

    public Task<IReadOnlyList<ArtifactSource>> GetArtifactSourcesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ArtifactSource>>(_artifactSources.Values.GroupBy(item => item.Branch, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderByDescending(item => item.CompletedAt).ThenByDescending(item => item.SourceId).First())
            .OrderBy(item => item.BcVersion).ToList());

    internal static IEnumerable<DeploymentEvent> Filter(IEnumerable<DeploymentEvent> items, DeploymentQuery query) => items
        .Where(item => query.CustomerIds is null || query.CustomerIds.Contains(item.Customer.Id))
        .Where(item => query.Status is null || item.Status.ToString().Equals(query.Status, StringComparison.OrdinalIgnoreCase))
        .Where(item => query.Mode is null || item.Mode.ToString().Equals(query.Mode, StringComparison.OrdinalIgnoreCase))
        .Where(item => query.Workflow is null || item.Source.Workflow.Contains(query.Workflow, StringComparison.OrdinalIgnoreCase))
        .Where(item => query.Branch is null || string.Equals(item.Source.Branch, query.Branch, StringComparison.OrdinalIgnoreCase))
        .Where(item => query.From is null || item.CompletedAt >= query.From)
        .Where(item => query.To is null || item.CompletedAt <= query.To);

    internal static bool IsVerified(DeploymentOperation operation) => operation.Outcome is DeploymentOutcome.Success
        or DeploymentOutcome.AlreadyCurrent or DeploymentOutcome.NewerPresent;

    private void UpdateDesiredState(DeploymentEvent item)
    {
        foreach (var tenant in item.TenantAppStates.GroupBy(state => state.TenantId, StringComparer.OrdinalIgnoreCase))
        {
            var expectedIds = tenant.Select(state => state.ApplicationId ?? state.ApplicationName.ToLowerInvariant().Replace(' ', '-')).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var existing in _desiredState.Where(pair => pair.Value.CustomerId.Equals(item.Customer.Id, StringComparison.OrdinalIgnoreCase) && pair.Value.TenantId.Equals(tenant.Key, StringComparison.OrdinalIgnoreCase) && !expectedIds.Contains(pair.Value.ApplicationId)).ToList())
                _desiredState.TryRemove(existing.Key, out _);
        }
        foreach (var snapshot in item.TenantAppStates)
        {
            var appId = snapshot.ApplicationId ?? snapshot.ApplicationName.ToLowerInvariant().Replace(' ', '-');
            var key = $"{item.Customer.Id}|{snapshot.TenantId}|{appId}";
            _desiredState.AddOrUpdate(key,
                _ => ToDesiredState(item, snapshot, null),
                (_, existing) => existing.UpdatedAt > item.CompletedAt ? existing : ToDesiredState(item, snapshot, existing));
        }
    }

    private static CustomerDesiredAppState ToDesiredState(DeploymentEvent item, TenantAppState snapshot, CustomerDesiredAppState? existing)
    {
        var installed = snapshot.InstalledVersion ?? existing?.InstalledVersion;
        var observedAt = snapshot.InstalledVersion is null ? existing?.ObservedAt : snapshot.ObservedAt ?? item.CompletedAt;
        return new CustomerDesiredAppState
        {
            CustomerId = item.Customer.Id, TenantId = snapshot.TenantId, TenantLabel = snapshot.TenantLabel,
            ApplicationId = snapshot.ApplicationId ?? snapshot.ApplicationName.ToLowerInvariant().Replace(' ', '-'),
            ApplicationName = snapshot.ApplicationName, Publisher = snapshot.Publisher, DesiredVersion = snapshot.DesiredVersion,
            InstalledVersion = installed, InstalledAt = snapshot.InstalledVersion is null ? existing?.InstalledAt : snapshot.InstalledAt, ObservedAt = observedAt, State = snapshot.State,
            LastOutcome = snapshot.LastOutcome, SafeMessage = snapshot.SafeMessage, UpdatedAt = item.CompletedAt, EventId = item.EventId
        };
    }

    private CustomerLatestStatus WithHealth(CustomerLatestStatus item, string customerId)
    {
        var states = _desiredState.Values.Where(state => state.CustomerId.Equals(customerId, StringComparison.OrdinalIgnoreCase)).ToList();
        var failed = states.Count(state => state.State == "failed");
        var current = states.Count(state => state.State == "current");
        var attention = states.Count - current - failed;
        var tenants = states.GroupBy(state => state.TenantId, StringComparer.OrdinalIgnoreCase).Select(group => TenantHealth(group)).OrderBy(tenant => tenant.TenantLabel ?? tenant.TenantId).ToList();
        return new CustomerLatestStatus { CustomerId = item.CustomerId, CustomerName = item.CustomerName, EventId = item.EventId, Status = item.Status, Mode = item.Mode, CompletedAt = item.CompletedAt, Summary = item.Summary, BcVersion = item.BcVersion, PackageVersion = item.PackageVersion, DesiredAppCount = states.Count, CurrentAppCount = current, AttentionAppCount = attention, FailedAppCount = failed, Health = failed > 0 ? "failed" : attention > 0 ? "attention" : states.Count > 0 ? "current" : "unknown", Tenants = tenants };
    }

    internal static TenantLatestStatus TenantHealth(IEnumerable<CustomerDesiredAppState> states)
    {
        var values = states.ToList(); var failed = values.Count(state => state.State == "failed"); var current = values.Count(state => state.State == "current"); var attention = values.Count - current - failed;
        var versions = values.Select(state => state.InstalledVersion).Where(version => !string.IsNullOrWhiteSpace(version)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        return new TenantLatestStatus { TenantId = values[0].TenantId, TenantLabel = values[0].TenantLabel, InstalledVersion = versions.Count == 1 ? versions[0] : versions.Count == 0 ? null : "Multiple versions", InstalledAt = values.Where(state => state.InstalledAt.HasValue).Select(state => state.InstalledAt).Max(), DesiredAppCount = values.Count, CurrentAppCount = current, AttentionAppCount = attention, FailedAppCount = failed, Health = failed > 0 ? "failed" : attention > 0 ? "attention" : values.Count > 0 ? "current" : "unknown" };
    }
}

public static class Cursor
{
    public static string Encode(int offset) => Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(offset.ToString()));
    public static int Decode(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor)) return 0;
        try { return int.TryParse(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(cursor)), out var value) && value >= 0 ? value : 0; }
        catch (FormatException) { return 0; }
    }
}
