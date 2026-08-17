using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure;
using Azure.Data.Tables;
using DeploymentStatus.Api.Models;

namespace DeploymentStatus.Api.Storage;

public sealed class TableDeploymentStore(TableServiceClient serviceClient) : IDeploymentStore
{
    private readonly TableClient _runs = serviceClient.GetTableClient("DeploymentRuns");
    private readonly TableClient _operations = serviceClient.GetTableClient("DeploymentOperations");
    private readonly TableClient _feed = serviceClient.GetTableClient("DeploymentFeed");
    private readonly TableClient _desiredState = serviceClient.GetTableClient("CustomerDesiredState");
    private readonly TableClient _latest = serviceClient.GetTableClient("CustomerLatest");
    private readonly TableClient _index = serviceClient.GetTableClient("DeploymentEventIndex");
    private readonly TableClient _artifactSources = serviceClient.GetTableClient("ArtifactSources");
    private readonly TableClient _artifactSourceIndex = serviceClient.GetTableClient("ArtifactSourceIndex");
    private readonly TableClient _artifactSourceLatest = serviceClient.GetTableClient("ArtifactSourceLatest");
    private readonly SemaphoreSlim _initializeLock = new(1, 1);
    private volatile bool _initialized;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<bool> RegisterAsync(DeploymentEvent item, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        item.Customer.Id = item.Customer.Id.ToLowerInvariant();
        var hash = Hash(item.EventId);
        var runRow = $"{DateTimeOffset.MaxValue.UtcTicks - item.CompletedAt.UtcTicks:D19}-{hash}";
        var created = true;
        try
        {
            await _index.AddEntityAsync(new TableEntity("event", hash)
            {
                ["EventId"] = item.EventId, ["CustomerId"] = item.Customer.Id,
                ["RunRow"] = runRow, ["Status"] = "processing"
            }, cancellationToken);
        }
        catch (RequestFailedException exception) when (exception.Status == 409)
        {
            created = false;
            var existing = await _index.GetEntityAsync<TableEntity>("event", hash, cancellationToken: cancellationToken);
            if (string.Equals(existing.Value.GetString("Status"), "complete", StringComparison.OrdinalIgnoreCase))
                return false;
            runRow = existing.Value.GetString("RunRow") ?? runRow;
        }

        var header = CloneWithoutOperations(item);
        var json = JsonSerializer.Serialize(header, JsonOptions);
        await _runs.UpsertEntityAsync(new TableEntity(item.Customer.Id, runRow)
        {
            ["EventId"] = item.EventId, ["CompletedAt"] = item.CompletedAt,
            ["Status"] = item.Status.ToString(), ["Mode"] = item.Mode.ToString(), ["EventJson"] = json
        }, TableUpdateMode.Replace, cancellationToken);

        var feedPartition = item.CompletedAt.UtcDateTime.ToString("yyyyMM");
        await _feed.UpsertEntityAsync(new TableEntity(feedPartition, runRow)
        {
            ["EventId"] = item.EventId, ["CustomerId"] = item.Customer.Id,
            ["CompletedAt"] = item.CompletedAt, ["EventJson"] = json
        }, TableUpdateMode.Replace, cancellationToken);

        for (var index = 0; index < item.Operations.Count; index++)
        {
            var operation = item.Operations[index];
            await _operations.UpsertEntityAsync(new TableEntity(hash, $"{index:D6}")
            {
                ["OperationJson"] = JsonSerializer.Serialize(operation, JsonOptions)
            }, TableUpdateMode.Replace, cancellationToken);
        }

        await UpdateLatestAsync(item, json, cancellationToken);
        if (item.Mode == DeploymentMode.Execute)
            await UpdateDesiredStateAsync(item, cancellationToken);

        await _index.UpdateEntityAsync(new TableEntity("event", hash)
        {
            ["EventId"] = item.EventId, ["CustomerId"] = item.Customer.Id,
            ["RunRow"] = runRow, ["Status"] = "complete"
        }, ETag.All, TableUpdateMode.Replace, cancellationToken);
        return created;
    }

    public async Task<PagedDeployments> QueryAsync(DeploymentQuery query, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        var events = new List<DeploymentEvent>();
        if (query.CustomerIds is null)
        {
            await foreach (var entity in _feed.QueryAsync<TableEntity>(cancellationToken: cancellationToken))
                AddEvent(entity, events);
        }
        else
        {
            foreach (var customerId in query.CustomerIds)
            {
                var filter = TableClient.CreateQueryFilter($"PartitionKey eq {customerId}");
                await foreach (var entity in _runs.QueryAsync<TableEntity>(filter, cancellationToken: cancellationToken))
                    AddEvent(entity, events);
            }
        }
        var filtered = InMemoryDeploymentStore.Filter(events, query).OrderByDescending(item => item.CompletedAt).ThenBy(item => item.EventId).ToList();
        var page = filtered.Skip(query.Offset).Take(query.PageSize).ToList();
        var next = query.Offset + page.Count < filtered.Count ? Cursor.Encode(query.Offset + page.Count) : null;
        return new PagedDeployments(page, next);
    }

    public async Task<DeploymentEvent?> GetAsync(string eventId, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        var hash = Hash(eventId);
        var index = await _index.GetEntityIfExistsAsync<TableEntity>("event", hash, cancellationToken: cancellationToken);
        if (!index.HasValue || index.Value!.GetString("Status") != "complete") return null;
        var customerId = index.Value!.GetString("CustomerId")!;
        var runRow = index.Value!.GetString("RunRow")!;
        var run = await _runs.GetEntityIfExistsAsync<TableEntity>(customerId, runRow, cancellationToken: cancellationToken);
        if (!run.HasValue) return null;
        var item = DeserializeEvent(run.Value!.GetString("EventJson"));
        if (item is null) return null;
        var filter = TableClient.CreateQueryFilter($"PartitionKey eq {hash}");
        await foreach (var entity in _operations.QueryAsync<TableEntity>(filter, cancellationToken: cancellationToken))
        {
            var operation = JsonSerializer.Deserialize<DeploymentOperation>(entity.GetString("OperationJson")!, JsonOptions);
            if (operation is not null) item.Operations.Add(operation);
        }
        return item;
    }

    public async Task<IReadOnlyList<CustomerLatestStatus>> GetCustomersAsync(IReadOnlySet<string>? customerIds, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        var result = new List<CustomerLatestStatus>();
        await foreach (var entity in _latest.QueryAsync<TableEntity>(cancellationToken: cancellationToken))
        {
            var item = DeserializeEvent(entity.GetString("EventJson"));
            if (item is null || (customerIds is not null && !customerIds.Contains(item.Customer.Id))) continue;
            var latest = new CustomerLatestStatus { CustomerId = item.Customer.Id, CustomerName = item.Customer.Name,
                EventId = item.EventId, Status = item.Status, Mode = item.Mode, CompletedAt = item.CompletedAt, Summary = item.Summary,
                BcVersion = item.ArtifactSource?.BcVersion, PackageVersion = item.ArtifactSource?.PackageVersion };
            result.Add(await WithHealthAsync(latest, item.Customer.Id, cancellationToken));
        }
        return result.OrderBy(item => item.CustomerName).ToList();
    }

    public async Task<IReadOnlyList<CustomerDesiredAppState>> GetDesiredAppStateAsync(string customerId, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        var result = new List<CustomerDesiredAppState>();
        var filter = TableClient.CreateQueryFilter($"PartitionKey eq {customerId}");
        await foreach (var entity in _desiredState.QueryAsync<TableEntity>(filter, cancellationToken: cancellationToken))
        {
            var item = JsonSerializer.Deserialize<CustomerDesiredAppState>(entity.GetString("StateJson")!, JsonOptions);
            if (item is not null) result.Add(item);
        }
        return result.OrderBy(item => item.TenantId).ThenBy(item => item.ApplicationName).ToList();
    }

    public async Task<bool> RegisterArtifactSourceAsync(ArtifactSource item, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        var hash = Hash(item.SourceId);
        var row = $"{DateTimeOffset.MaxValue.UtcTicks - item.CompletedAt.UtcTicks:D19}-{hash}";
        try
        {
            await _artifactSourceIndex.AddEntityAsync(new TableEntity("source", hash) { ["SourceId"] = item.SourceId, ["Branch"] = item.Branch, ["Row"] = row }, cancellationToken);
        }
        catch (RequestFailedException exception) when (exception.Status == 409) { return false; }
        var json = JsonSerializer.Serialize(item, JsonOptions);
        await _artifactSources.UpsertEntityAsync(new TableEntity(item.Branch.ToLowerInvariant(), row)
        { ["SourceId"] = item.SourceId, ["CompletedAt"] = item.CompletedAt, ["SourceJson"] = json }, TableUpdateMode.Replace, cancellationToken);
        var existing = await _artifactSourceLatest.GetEntityIfExistsAsync<TableEntity>("latest", item.Branch.ToLowerInvariant(), cancellationToken: cancellationToken);
        if (!existing.HasValue || existing.Value!.GetDateTimeOffset("CompletedAt") <= item.CompletedAt)
            await _artifactSourceLatest.UpsertEntityAsync(new TableEntity("latest", item.Branch.ToLowerInvariant())
            { ["CompletedAt"] = item.CompletedAt, ["SourceJson"] = json }, TableUpdateMode.Replace, cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<ArtifactSource>> GetArtifactSourcesAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        var result = new List<ArtifactSource>();
        await foreach (var entity in _artifactSourceLatest.QueryAsync<TableEntity>(cancellationToken: cancellationToken))
        {
            var item = JsonSerializer.Deserialize<ArtifactSource>(entity.GetString("SourceJson") ?? "", JsonOptions);
            if (item is not null) result.Add(item);
        }
        return result.OrderBy(item => item.BcVersion).ThenBy(item => item.Branch).ToList();
    }

    private async Task UpdateLatestAsync(DeploymentEvent item, string json, CancellationToken cancellationToken)
    {
        var existing = await _latest.GetEntityIfExistsAsync<TableEntity>("latest", item.Customer.Id, cancellationToken: cancellationToken);
        if (existing.HasValue && existing.Value!.GetDateTimeOffset("CompletedAt") > item.CompletedAt) return;
        await _latest.UpsertEntityAsync(new TableEntity("latest", item.Customer.Id)
        {
            ["CompletedAt"] = item.CompletedAt, ["EventJson"] = json
        }, TableUpdateMode.Replace, cancellationToken);
    }

    private async Task UpdateDesiredStateAsync(DeploymentEvent item, CancellationToken cancellationToken)
    {
        var existingByTenant = new Dictionary<string, List<(string Row, CustomerDesiredAppState State)>>(StringComparer.OrdinalIgnoreCase);
        var filter = TableClient.CreateQueryFilter($"PartitionKey eq {item.Customer.Id}");
        await foreach (var entity in _desiredState.QueryAsync<TableEntity>(filter, cancellationToken: cancellationToken))
        {
            var existing = JsonSerializer.Deserialize<CustomerDesiredAppState>(entity.GetString("StateJson")!, JsonOptions);
            if (existing is null) continue;
            (existingByTenant.TryGetValue(existing.TenantId, out var entries) ? entries : existingByTenant[existing.TenantId] = []).Add((entity.RowKey, existing));
        }
        foreach (var tenant in item.TenantAppStates.GroupBy(state => state.TenantId, StringComparer.OrdinalIgnoreCase))
        {
            var expectedIds = tenant.Select(state => state.ApplicationId ?? state.ApplicationName.ToLowerInvariant().Replace(' ', '-')).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (!existingByTenant.TryGetValue(tenant.Key, out var entries)) continue;
            foreach (var existing in entries.Where(entry => !expectedIds.Contains(entry.State.ApplicationId)))
                await _desiredState.DeleteEntityAsync(item.Customer.Id, existing.Row, ETag.All, cancellationToken);
        }
        foreach (var snapshot in item.TenantAppStates)
        {
            var appId = snapshot.ApplicationId ?? snapshot.ApplicationName.ToLowerInvariant().Replace(' ', '-');
            var row = Hash($"{snapshot.TenantId}|{appId}");
            var existing = await _desiredState.GetEntityIfExistsAsync<TableEntity>(item.Customer.Id, row, cancellationToken: cancellationToken);
            if (existing.HasValue && existing.Value!.GetDateTimeOffset("UpdatedAt") > item.CompletedAt) continue;
            var previous = existing.HasValue ? JsonSerializer.Deserialize<CustomerDesiredAppState>(existing.Value!.GetString("StateJson")!, JsonOptions) : null;
            var state = new CustomerDesiredAppState
            {
                CustomerId = item.Customer.Id, TenantId = snapshot.TenantId, TenantLabel = snapshot.TenantLabel,
                ApplicationId = appId, ApplicationName = snapshot.ApplicationName, Publisher = snapshot.Publisher,
                DesiredVersion = snapshot.DesiredVersion, InstalledVersion = snapshot.InstalledVersion ?? previous?.InstalledVersion,
                ObservedAt = snapshot.InstalledVersion is null ? previous?.ObservedAt : snapshot.ObservedAt ?? item.CompletedAt,
                State = snapshot.State, LastOutcome = snapshot.LastOutcome, SafeMessage = snapshot.SafeMessage,
                UpdatedAt = item.CompletedAt, EventId = item.EventId
            };
            await _desiredState.UpsertEntityAsync(new TableEntity(item.Customer.Id, row)
            {
                ["UpdatedAt"] = item.CompletedAt, ["StateJson"] = JsonSerializer.Serialize(state, JsonOptions)
            }, TableUpdateMode.Replace, cancellationToken);
        }
    }

    private async Task<CustomerLatestStatus> WithHealthAsync(CustomerLatestStatus item, string customerId, CancellationToken cancellationToken)
    {
        var filter = TableClient.CreateQueryFilter($"PartitionKey eq {customerId}");
        var states = new List<CustomerDesiredAppState>();
        await foreach (var entity in _desiredState.QueryAsync<TableEntity>(filter, cancellationToken: cancellationToken))
        {
            var state = JsonSerializer.Deserialize<CustomerDesiredAppState>(entity.GetString("StateJson")!, JsonOptions);
            if (state is not null) states.Add(state);
        }
        var failed = states.Count(state => state.State == "failed");
        var current = states.Count(state => state.State == "current");
        var attention = states.Count - current - failed;
        return new CustomerLatestStatus { CustomerId = item.CustomerId, CustomerName = item.CustomerName, EventId = item.EventId, Status = item.Status, Mode = item.Mode, CompletedAt = item.CompletedAt, Summary = item.Summary, BcVersion = item.BcVersion, PackageVersion = item.PackageVersion, DesiredAppCount = states.Count, CurrentAppCount = current, AttentionAppCount = attention, FailedAppCount = failed, Health = failed > 0 ? "failed" : attention > 0 ? "attention" : states.Count > 0 ? "current" : "unknown" };
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (_initialized) return;
        await _initializeLock.WaitAsync(cancellationToken);
        try
        {
            if (_initialized) return;
            foreach (var table in new[] { _runs, _operations, _feed, _desiredState, _latest, _index, _artifactSources, _artifactSourceIndex, _artifactSourceLatest })
                await table.CreateIfNotExistsAsync(cancellationToken);
            _initialized = true;
        }
        finally { _initializeLock.Release(); }
    }

    private static void AddEvent(TableEntity entity, ICollection<DeploymentEvent> target)
    {
        var item = DeserializeEvent(entity.GetString("EventJson"));
        if (item is not null) target.Add(item);
    }
    private static DeploymentEvent? DeserializeEvent(string? json) => string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<DeploymentEvent>(json, JsonOptions);
    private static DeploymentEvent CloneWithoutOperations(DeploymentEvent item)
    {
        var json = JsonSerializer.Serialize(item, JsonOptions);
        var clone = JsonSerializer.Deserialize<DeploymentEvent>(json, JsonOptions)!;
        clone.Operations = [];
        return clone;
    }
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
