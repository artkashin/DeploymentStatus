using System.Text.Json;
using System.Text.Json.Serialization;
using DeploymentStatus.Api.Models;
using DeploymentStatus.Api.Security;
using DeploymentStatus.Api.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace DeploymentStatus.Api.Functions;

public sealed class DeploymentFunctions(IDeploymentStore store, CallerContextFactory callerFactory,
    ILogger<DeploymentFunctions> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    [Function("RegisterDeploymentEvent")]
    public async Task<IActionResult> Register(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "v1/deployment-events")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        DeploymentEvent? item;
        try { item = await JsonSerializer.DeserializeAsync<DeploymentEvent>(request.Body, JsonOptions, cancellationToken); }
        catch (JsonException exception) { return new BadRequestObjectResult(new { error = "Invalid JSON.", detail = exception.Message }); }
        var errors = DeploymentValidation.Validate(item);
        if (errors.Count > 0) return new BadRequestObjectResult(new { error = "Validation failed.", errors });
        var created = await store.RegisterAsync(item!, cancellationToken);
        logger.LogInformation("Deployment event {EventId} accepted; created={Created}", item!.EventId, created);
        return new ObjectResult(new { eventId = item.EventId, duplicate = !created, acceptedAt = DateTimeOffset.UtcNow })
        {
            StatusCode = created ? StatusCodes.Status201Created : StatusCodes.Status200OK
        };
    }

    [Function("RegisterArtifactSource")]
    public async Task<IActionResult> RegisterArtifactSource(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "v1/artifact-sources")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        ArtifactSource? item;
        try { item = await JsonSerializer.DeserializeAsync<ArtifactSource>(request.Body, JsonOptions, cancellationToken); }
        catch (JsonException exception) { return new BadRequestObjectResult(new { error = "Invalid JSON.", detail = exception.Message }); }
        var errors = ArtifactSourceValidation.Validate(item);
        if (errors.Count > 0) return new BadRequestObjectResult(new { error = "Validation failed.", errors });
        var created = await store.RegisterArtifactSourceAsync(item!, cancellationToken);
        logger.LogInformation("Artifact source {SourceId} accepted; created={Created}", item!.SourceId, created);
        return new ObjectResult(new { sourceId = item.SourceId, duplicate = !created, acceptedAt = DateTimeOffset.UtcNow })
        { StatusCode = created ? StatusCodes.Status201Created : StatusCodes.Status200OK };
    }

    [Function("GetCurrentUser")]
    public IActionResult Me([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/me")] HttpRequest request)
    {
        var caller = callerFactory.Create(request);
        if (!caller.IsAuthenticated) return new UnauthorizedObjectResult(new { error = "Authentication is required." });
        if (!caller.IsAdaptive && caller.CustomerIds.Count == 0) return new ObjectResult(new { error = "No DeploymentStatus role is assigned." }) { StatusCode = 403 };
        return new OkObjectResult(new MeResponse(caller.Name, caller.IsAdaptive, caller.CustomerIds.Order().ToList()));
    }

    [Function("GetCustomersV1")]
    public async Task<IActionResult> Customers(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/customers")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        var caller = callerFactory.Create(request);
        var denied = Authorize(caller);
        if (denied is not null) return denied;
        var items = await store.GetCustomersAsync(caller.IsAdaptive ? null : caller.CustomerIds, cancellationToken);
        return new OkObjectResult(new { items, generatedAt = DateTimeOffset.UtcNow });
    }

    [Function("GetCustomerV1")]
    public async Task<IActionResult> Customer(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/customers/{customerId}")] HttpRequest request,
        string customerId, CancellationToken cancellationToken)
    {
        var caller = callerFactory.Create(request);
        var denied = Authorize(caller);
        if (denied is not null) return denied;
        customerId = customerId.ToLowerInvariant();
        if (!caller.CanAccess(customerId)) return new ObjectResult(new { error = "Customer access is not allowed." }) { StatusCode = 403 };
        var latest = (await store.GetCustomersAsync(new HashSet<string>([customerId], StringComparer.OrdinalIgnoreCase), cancellationToken)).SingleOrDefault();
        if (latest is null) return new NotFoundObjectResult(new { error = "Customer was not found." });
        var desiredAppState = await store.GetDesiredAppStateAsync(customerId, cancellationToken);
        return new OkObjectResult(new { customer = latest, desiredAppState });
    }

    [Function("GetDeploymentsV1")]
    public async Task<IActionResult> Deployments(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/deployments")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        var caller = callerFactory.Create(request);
        var denied = Authorize(caller);
        if (denied is not null) return denied;
        var requestedCustomer = request.Query["customerId"].FirstOrDefault()?.ToLowerInvariant();
        IReadOnlySet<string>? customerIds;
        if (caller.IsAdaptive)
            customerIds = requestedCustomer is null ? null : new HashSet<string>([requestedCustomer], StringComparer.OrdinalIgnoreCase);
        else
        {
            if (requestedCustomer is not null && !caller.CanAccess(requestedCustomer))
                return new ObjectResult(new { error = "Customer access is not allowed." }) { StatusCode = 403 };
            customerIds = requestedCustomer is null ? caller.CustomerIds : new HashSet<string>([requestedCustomer], StringComparer.OrdinalIgnoreCase);
        }
        var pageSize = int.TryParse(request.Query["pageSize"], out var parsedSize) ? Math.Clamp(parsedSize, 1, 100) : 25;
        var query = new DeploymentQuery(customerIds, Text(request, "status"), Text(request, "mode"),
            Text(request, "workflow"), Text(request, "branch"), Date(request, "from"), Date(request, "to"),
            Cursor.Decode(Text(request, "cursor")), pageSize);
        var result = await store.QueryAsync(query, cancellationToken);
        return new OkObjectResult(new DeploymentPageResponse(result.Items.Select(item => item.ToDto(caller.IsAdaptive, false)).ToList(), result.NextCursor));
    }

    [Function("GetDeploymentV1")]
    public async Task<IActionResult> Deployment(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/deployments/{eventId}")] HttpRequest request,
        string eventId, CancellationToken cancellationToken)
    {
        var caller = callerFactory.Create(request);
        var denied = Authorize(caller);
        if (denied is not null) return denied;
        var item = await store.GetAsync(eventId, cancellationToken);
        if (item is null) return new NotFoundObjectResult(new { error = "Deployment event was not found." });
        if (!caller.CanAccess(item.Customer.Id)) return new ObjectResult(new { error = "Customer access is not allowed." }) { StatusCode = 403 };
        return new OkObjectResult(item.ToDto(caller.IsAdaptive, true));
    }

    [Function("GetArtifactSourcesV1")]
    public async Task<IActionResult> ArtifactSources(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/artifact-sources")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        var caller = callerFactory.Create(request);
        var denied = Authorize(caller);
        if (denied is not null) return denied;
        if (!caller.IsAdaptive) return new ObjectResult(new { error = "Adaptive access is required." }) { StatusCode = 403 };
        var items = await store.GetArtifactSourcesAsync(cancellationToken);
        return new OkObjectResult(new { items = items.Select(item => item.ToDto()).ToList(), generatedAt = DateTimeOffset.UtcNow });
    }

    [Function("DeploymentStatusHealth")]
    public IActionResult Health([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/health")] HttpRequest request)
        => new OkObjectResult(new { status = "ok", timestamp = DateTimeOffset.UtcNow });

    private static IActionResult? Authorize(CallerContext caller)
    {
        if (!caller.IsAuthenticated) return new UnauthorizedObjectResult(new { error = "Authentication is required." });
        return !caller.IsAdaptive && caller.CustomerIds.Count == 0
            ? new ObjectResult(new { error = "No DeploymentStatus role is assigned." }) { StatusCode = 403 }
            : null;
    }
    private static string? Text(HttpRequest request, string key) => request.Query[key].FirstOrDefault() is { Length: > 0 } value ? value : null;
    private static DateTimeOffset? Date(HttpRequest request, string key) => DateTimeOffset.TryParse(Text(request, key), out var value) ? value : null;
}
