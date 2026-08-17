using System.Collections.Concurrent;
using DeploymentStatus.Api.Models;

namespace DeploymentStatus.Api.Storage;

public sealed class InMemoryDeploymentStore : IDeploymentStore
{
    private readonly ConcurrentDictionary<string, DeploymentEvent> _events = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, CurrentDeploymentState> _state = new(StringComparer.OrdinalIgnoreCase);

    public Task<bool> RegisterAsync(DeploymentEvent item, CancellationToken cancellationToken = default)
    {
        item.Customer.Id = item.Customer.Id.ToLowerInvariant();
        var added = _events.TryAdd(item.EventId, item);
        if (added && item.Mode == DeploymentMode.Execute)
        {
            foreach (var operation in item.Operations.Where(operation => operation.Scope.Equals("tenant", StringComparison.OrdinalIgnoreCase) && IsVerified(operation)))
            {
                var tenantId = operation.TenantId ?? "service";
                var appId = operation.ApplicationId ?? operation.ApplicationName.ToLowerInvariant().Replace(' ', '-');
                var key = $"{item.Customer.Id}|{tenantId}|{appId}";
                var state = new CurrentDeploymentState
                {
                    CustomerId = item.Customer.Id, TenantId = tenantId, TenantLabel = operation.TenantLabel,
                    ApplicationId = appId, ApplicationName = operation.ApplicationName,
                    Version = operation.ObservedVersion ?? operation.TargetVersion, LastOutcome = operation.Outcome,
                    VerifiedAt = item.CompletedAt, EventId = item.EventId
                };
                _state.AddOrUpdate(key, state, (_, existing) => existing.VerifiedAt > state.VerifiedAt ? existing : state);
            }
        }
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
            .Select(item => new CustomerLatestStatus { CustomerId = item.Customer.Id, CustomerName = item.Customer.Name,
                EventId = item.EventId, Status = item.Status, Mode = item.Mode, CompletedAt = item.CompletedAt, Summary = item.Summary })
            .OrderBy(item => item.CustomerName).Cast<CustomerLatestStatus>().ToList();
        return Task.FromResult<IReadOnlyList<CustomerLatestStatus>>(result);
    }

    public Task<IReadOnlyList<CurrentDeploymentState>> GetCurrentStateAsync(string customerId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<CurrentDeploymentState>>(_state.Values.Where(item => item.CustomerId.Equals(customerId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => item.TenantId).ThenBy(item => item.ApplicationName).ToList());

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
