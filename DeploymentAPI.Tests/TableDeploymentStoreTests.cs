using Azure.Data.Tables;
using DeploymentStatus.Api.Models;
using DeploymentStatus.Api.Storage;

namespace DeploymentAPI.Tests;

public sealed class TableDeploymentStoreTests
{
    private static readonly string[] TableNames =
    [
        "DeploymentRuns", "DeploymentOperations", "DeploymentFeed",
        "CustomerDesiredState", "CustomerLatest", "DeploymentEventIndex"
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
                        ApplicationId = "avalara-tax", ApplicationName = "AvalaraTax", Publisher = "Adaptive Business Solutions Inc", Action = "verify",
                        Outcome = DeploymentOutcome.Success, TargetVersion = "3.0.0.0", ObservedVersion = "3.0.0.0"
                    }
                ],
                TenantAppStates =
                [
                    new TenantAppState
                    {
                        TenantId = "default", TenantLabel = "Default", ApplicationId = "avalara-tax", ApplicationName = "AvalaraTax",
                        Publisher = "Adaptive Business Solutions Inc", DesiredVersion = "3.0.0.0", InstalledVersion = "3.0.0.0",
                        ObservedAt = started.AddMinutes(3), State = "current", LastOutcome = DeploymentOutcome.Success
                    },
                    new TenantAppState
                    {
                        TenantId = "default", TenantLabel = "Default", ApplicationId = "5fedf952-2450-4bce-b578-f3b7c13cd9a9", ApplicationName = "Avalara AvaTax",
                        Publisher = "Avalara", DesiredVersion = "27.0.52.0", InstalledVersion = "27.0.52.0", State = "current"
                    }
                ]
            };

            Assert.True(await store.RegisterAsync(deployment));
            Assert.False(await store.RegisterAsync(deployment));
            Assert.Equal(deployment.EventId, (await store.GetAsync(deployment.EventId))?.EventId);
            Assert.Single((await store.QueryAsync(new DeploymentQuery(null, null, null, null, null, null, null, 0, 25))).Items);
            var states = await store.GetDesiredAppStateAsync("retaildemo");
            Assert.Equal("3.0.0.0", Assert.Single(states, state => state.ApplicationName == "AvalaraTax").InstalledVersion);
            var customer = Assert.Single(await store.GetCustomersAsync(null));
            Assert.Equal("retaildemo", customer.CustomerId);
            var tenant = Assert.Single(customer.Tenants);
            Assert.Equal("3.0.0.0", tenant.AdaptiveInstalledVersion);
            Assert.Equal("27.0.52.0", tenant.AvalaraInstalledVersion);
            Assert.Equal("Multiple versions", tenant.InstalledVersion);
            Assert.Equal(2, (await store.GetAsync(deployment.EventId))!.TenantAppStates.Count);

            var createdTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await foreach (var table in service.QueryAsync())
                createdTables.Add(table.Name);
            foreach (var tableName in TableNames)
                Assert.Contains(tableName, createdTables);
            foreach (var tableName in TableNames)
                Assert.Equal(tableName switch
                {
                    "DeploymentOperations" => 3,
                    "CustomerDesiredState" => 2,
                    _ => 1
                }, await CountAsync(service.GetTableClient(tableName)));
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
