using Azure.Data.Tables;
using DeploymentStatus.Api.Models;
using DeploymentStatus.Api.Storage;

namespace DeploymentAPI.Tests;

public sealed class TableDeploymentStoreTests
{
    private static readonly string[] TableNames =
    [
        "DeploymentRuns", "DeploymentOperations", "DeploymentFeed",
        "CustomerCurrentState", "CustomerLatest", "DeploymentEventIndex"
    ];

    [Fact]
    public async Task Azurite_projects_event_into_every_read_model()
    {
        var connectionString = Environment.GetEnvironmentVariable("AZURITE_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
            return;

        var service = new TableServiceClient(connectionString);
        await DeleteTablesAsync(service);
        try
        {
            var store = new TableDeploymentStore(service);
            var started = DateTimeOffset.Parse("2026-08-17T12:00:00Z");
            var deployment = new DeploymentEvent
            {
                EventId = "AdaptiveBS~CIApp:98765:1:retaildemo:execute",
                Source = new DeploymentSource { Repository = "AdaptiveBS/CIApp", Workflow = "Update retaildemo", RunId = 98765, RunAttempt = 1 },
                Customer = new DeploymentCustomer { Id = "retaildemo", Name = "Retail Demo" },
                Mode = DeploymentMode.Execute,
                Status = DeploymentRunStatus.Success,
                StartedAt = started,
                CompletedAt = started.AddMinutes(3),
                Summary = new DeploymentSummary { Total = 1, Succeeded = 1 },
                Operations =
                [
                    new DeploymentOperation
                    {
                        Scope = "tenant", TenantId = "default", TenantLabel = "Default",
                        ApplicationId = "retail", ApplicationName = "Retail", Action = "verify",
                        Outcome = DeploymentOutcome.Success, TargetVersion = "3.0.0.0", ObservedVersion = "3.0.0.0"
                    }
                ]
            };

            Assert.True(await store.RegisterAsync(deployment));
            Assert.False(await store.RegisterAsync(deployment));
            Assert.Equal(deployment.EventId, (await store.GetAsync(deployment.EventId))?.EventId);
            Assert.Single((await store.QueryAsync(new DeploymentQuery(null, null, null, null, null, null, null, 0, 25))).Items);
            Assert.Equal("3.0.0.0", Assert.Single(await store.GetCurrentStateAsync("retaildemo")).Version);
            Assert.Equal("retaildemo", Assert.Single(await store.GetCustomersAsync(null)).CustomerId);

            var createdTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await foreach (var table in service.QueryAsync())
                createdTables.Add(table.Name);
            foreach (var tableName in TableNames)
                Assert.Contains(tableName, createdTables);
            foreach (var tableName in TableNames)
                Assert.Equal(1, await CountAsync(service.GetTableClient(tableName)));
        }
        finally
        {
            await DeleteTablesAsync(service);
        }
    }

    private static async Task DeleteTablesAsync(TableServiceClient service)
    {
        foreach (var tableName in TableNames)
        {
            try { await service.DeleteTableAsync(tableName); }
            catch (Azure.RequestFailedException exception) when (exception.Status == 404) { }
        }
    }

    private static async Task<int> CountAsync(TableClient table)
    {
        var count = 0;
        await foreach (var _ in table.QueryAsync<TableEntity>()) count++;
        return count;
    }
}
