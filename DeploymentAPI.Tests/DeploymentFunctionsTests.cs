using DeploymentStatus.Api.Functions;
using DeploymentStatus.Api.Models;
using DeploymentStatus.Api.Security;
using DeploymentStatus.Api.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace DeploymentAPI.Tests;

public sealed class DeploymentFunctionsTests
{
    [Fact]
    public void Me_rejects_an_unauthenticated_caller()
    {
        var functions = Functions(new InMemoryDeploymentStore());
        Assert.IsType<UnauthorizedObjectResult>(functions.Me(new DefaultHttpContext().Request));
    }

    [Fact]
    public async Task Customer_role_union_is_applied_server_side()
    {
        var store = new InMemoryDeploymentStore();
        await store.RegisterAsync(Event("tappers", 301));
        await store.RegisterAsync(Event("riddle", 302));
        await store.RegisterAsync(Event("stratus", 303));
        var request = Request("DeploymentStatus.Customer.tappers,DeploymentStatus.Customer.riddle");

        var result = Assert.IsType<OkObjectResult>(await Functions(store).Deployments(request, CancellationToken.None));
        var response = Assert.IsType<DeploymentPageResponse>(result.Value);
        Assert.Equal(["riddle", "tappers"], response.Items.Select(item => item.Customer.Id).Order().ToArray());
    }

    [Fact]
    public async Task Customer_cannot_open_another_customer_event()
    {
        var store = new InMemoryDeploymentStore();
        var item = Event("riddle", 401);
        await store.RegisterAsync(item);
        var result = Assert.IsType<ObjectResult>(await Functions(store).Deployment(Request("DeploymentStatus.Customer.tappers"), item.EventId, CancellationToken.None));
        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
    }

    [Fact]
    public async Task Artifact_sources_are_adaptive_only_and_idempotent()
    {
        var store = new InMemoryDeploymentStore();
        var source = new ArtifactSource
        {
            SourceId = "AdaptiveBS~AJEApps:32007490438:1:main:available", Repository = "AdaptiveBS/AJEApps",
            Workflow = "CI/CD", Branch = "main", BcVersion = "25", RunId = 32007490438,
            CompletedAt = DateTimeOffset.Parse("2026-08-17T09:41:00Z"), Conclusion = "failure",
            ArtifactName = "AJEApps-main-Apps-1.0.1205.0", PackageVersion = "1.0.1205.0",
            ArtifactAvailable = true, Usable = true, Warning = "Usable with warning: artifact is available."
        };
        Assert.True(await store.RegisterArtifactSourceAsync(source));
        Assert.False(await store.RegisterArtifactSourceAsync(source));
        var customer = Assert.IsType<ObjectResult>(await Functions(store).ArtifactSources(Request("DeploymentStatus.Customer.tappers"), CancellationToken.None));
        Assert.Equal(StatusCodes.Status403Forbidden, customer.StatusCode);
        var adaptive = Assert.IsType<OkObjectResult>(await Functions(store).ArtifactSources(Request("DeploymentStatus.Adaptive.All"), CancellationToken.None));
        Assert.NotNull(adaptive.Value);
        Assert.Equal("1.0.1205.0", (await store.GetArtifactSourcesAsync()).Single().PackageVersion);
    }

    private static DeploymentFunctions Functions(IDeploymentStore store)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authorization:AllowDevelopmentHeaders"] = "true"
        }).Build();
        return new DeploymentFunctions(store, new CallerContextFactory(configuration), NullLogger<DeploymentFunctions>.Instance);
    }

    private static HttpRequest Request(string roles)
    {
        var request = new DefaultHttpContext().Request;
        request.Headers["X-Development-User"] = "user@example.test";
        request.Headers["X-Development-Roles"] = roles;
        return request;
    }

    private static DeploymentEvent Event(string customerId, long runId)
    {
        var started = DateTimeOffset.Parse("2026-08-17T10:00:00Z");
        return new DeploymentEvent
        {
            EventId = $"AdaptiveBS~CIApp:{runId}:1:{customerId}:execute",
            Source = new DeploymentSource { Repository = "AdaptiveBS/CIApp", Workflow = "Update", RunId = runId },
            Customer = new DeploymentCustomer { Id = customerId, Name = customerId },
            Mode = DeploymentMode.Execute, Status = DeploymentRunStatus.Success,
            StartedAt = started, CompletedAt = started.AddMinutes(1),
            Summary = new DeploymentSummary(), Operations = []
        };
    }
}
