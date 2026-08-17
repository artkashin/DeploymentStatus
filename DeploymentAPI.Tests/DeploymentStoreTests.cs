using DeploymentStatus.Api.Models;
using DeploymentStatus.Api.Storage;

namespace DeploymentAPI.Tests;

public sealed class DeploymentStoreTests
{
    [Fact]
    public async Task Register_is_idempotent()
    {
        var store = new InMemoryDeploymentStore();
        var item = Event("AdaptiveBS~CIApp:123:1:tappers:execute", DeploymentRunStatus.Success, DeploymentOutcome.Success, "1.0.0.0");
        Assert.True(await store.RegisterAsync(item));
        Assert.False(await store.RegisterAsync(item));
        Assert.Single((await store.QueryAsync(new DeploymentQuery(null, null, null, null, null, null, null, 0, 25))).Items);
    }

    [Fact]
    public async Task Failed_attempt_does_not_destroy_last_verified_version()
    {
        var store = new InMemoryDeploymentStore();
        await store.RegisterAsync(Event("AdaptiveBS~CIApp:123:1:tappers:execute", DeploymentRunStatus.Success, DeploymentOutcome.Success, "2.0.0.0"));
        var failed = Event("AdaptiveBS~CIApp:124:1:tappers:execute", DeploymentRunStatus.Failed, DeploymentOutcome.Failed, "2.0.0.0");
        failed.CompletedAt = failed.CompletedAt.AddMinutes(1);
        await store.RegisterAsync(failed);
        var state = Assert.Single(await store.GetDesiredAppStateAsync("tappers"));
        Assert.Equal("2.0.0.0", state.InstalledVersion);
        Assert.Equal("AdaptiveBS~CIApp:124:1:tappers:execute", state.EventId);
        Assert.Equal(DeploymentOutcome.Failed, state.LastOutcome);
        Assert.Equal("failed", state.State);
        Assert.Equal(DeploymentRunStatus.Failed, Assert.Single(await store.GetCustomersAsync(null)).Status);
    }

    [Fact]
    public async Task Customer_query_is_scoped()
    {
        var store = new InMemoryDeploymentStore();
        await store.RegisterAsync(Event("AdaptiveBS~CIApp:123:1:tappers:execute", DeploymentRunStatus.Success, DeploymentOutcome.Success, "1"));
        var second = Event("AdaptiveBS~CIApp:123:1:riddle:execute", DeploymentRunStatus.Success, DeploymentOutcome.Success, "1");
        second.Customer = new DeploymentCustomer { Id = "riddle", Name = "Riddle's" };
        await store.RegisterAsync(second);
        var query = new DeploymentQuery(new HashSet<string>(["tappers"]), null, null, null, null, null, null, 0, 25);
        Assert.Equal("tappers", Assert.Single((await store.QueryAsync(query)).Items).Customer.Id);
    }

    [Fact]
    public async Task Pagination_filters_and_dry_run_state_rules_are_enforced()
    {
        var store = new InMemoryDeploymentStore();
        var first = Event("AdaptiveBS~CIApp:201:1:tappers:execute", DeploymentRunStatus.Success, DeploymentOutcome.Success, "1");
        first.Source.Workflow = "Nightly update";
        first.Source.Branch = "main";
        await store.RegisterAsync(first);
        var dryRun = Event("AdaptiveBS~CIApp:202:1:tappers:dryRun", DeploymentRunStatus.Success, DeploymentOutcome.Success, "2");
        dryRun.Mode = DeploymentMode.DryRun;
        dryRun.CompletedAt = first.CompletedAt.AddMinutes(1);
        dryRun.Source.Workflow = "Nightly update";
        dryRun.Source.Branch = "main";
        await store.RegisterAsync(dryRun);

        var page = await store.QueryAsync(new DeploymentQuery(null, "success", "dryRun", "nightly", "main", null, null, 0, 1));
        Assert.Single(page.Items);
        Assert.Equal(DeploymentMode.DryRun, page.Items[0].Mode);
        Assert.Null(page.NextCursor);
        Assert.Equal("1", Assert.Single(await store.GetDesiredAppStateAsync("tappers")).InstalledVersion);
    }

    [Fact]
    public void Validation_rejects_invalid_contract()
    {
        var item = Event("", DeploymentRunStatus.Success, DeploymentOutcome.Success, "1");
        item.CompletedAt = item.StartedAt.AddMinutes(-1);
        var errors = DeploymentValidation.Validate(item);
        Assert.Contains(errors, error => error.Contains("eventId"));
        Assert.Contains(errors, error => error.Contains("completedAt"));
    }

    [Fact]
    public void Customer_projection_removes_internal_data()
    {
        var item = Event("AdaptiveBS~CIApp:123:1:tappers:execute", DeploymentRunStatus.Failed, DeploymentOutcome.Failed, "1");
        item.Source.RunUrl = "https://github.example/run";
        item.Source.ServiceName = "private-service";
        item.Operations[0].InternalError = "C:\\private\\path";
        var dto = item.ToDto(false, true);
        Assert.Null(dto.Source.RunUrl);
        Assert.Null(dto.Source.ServiceName);
        Assert.Null(dto.Source.RunId);
        Assert.Null(dto.Source.RunAttempt);
        Assert.Null(Assert.Single(dto.Operations!).InternalError);
        Assert.Equal("Update failed.", Assert.Single(dto.Operations!).Message);
    }

    private static DeploymentEvent Event(string id, DeploymentRunStatus status, DeploymentOutcome outcome, string version)
    {
        var started = DateTimeOffset.Parse("2026-08-17T10:00:00Z");
        return new DeploymentEvent
        {
            EventId = id,
            Source = new DeploymentSource { Repository = "AdaptiveBS/CIApp", Workflow = "Update tappers", RunId = 123 },
            Customer = new DeploymentCustomer { Id = "tappers", Name = "Tappers" },
            Mode = DeploymentMode.Execute,
            Status = status,
            StartedAt = started,
            CompletedAt = started.AddMinutes(2),
            Summary = new DeploymentSummary { Total = 1, Succeeded = outcome == DeploymentOutcome.Success ? 1 : 0, Failed = outcome == DeploymentOutcome.Failed ? 1 : 0 },
            Operations = [new DeploymentOperation { ApplicationId = "app", ApplicationName = "App", TenantId = "default", Action = "upgrade", Outcome = outcome, TargetVersion = version, ObservedVersion = outcome == DeploymentOutcome.Success ? version : null, SafeMessage = "Update failed." }],
            TenantAppStates = [new TenantAppState { TenantId = "default", ApplicationId = "app", ApplicationName = "App", DesiredVersion = version, InstalledVersion = version, ObservedAt = started.AddMinutes(2), State = outcome == DeploymentOutcome.Failed ? "failed" : "current", LastOutcome = outcome }]
        };
    }
}
