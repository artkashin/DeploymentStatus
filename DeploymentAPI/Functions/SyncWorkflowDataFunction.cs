using DeploymentAPI.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace DeploymentAPI.Functions;

public class SyncWorkflowDataFunction
{
    private readonly IWorkflowSyncService _syncService;
    private readonly ILogger<SyncWorkflowDataFunction> _logger;

    public SyncWorkflowDataFunction(
        IWorkflowSyncService syncService,
        ILogger<SyncWorkflowDataFunction> logger)
    {
        _syncService = syncService;
        _logger = logger;
    }

    [Function("SyncWorkflowData")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "sync/workflow-data")] HttpRequestData req)
    {
        _logger.LogInformation("Sync workflow data endpoint called");

        try
        {
            var result = await _syncService.SyncLatestWorkflowRunAsync();

            var response = req.CreateResponse(result.Success ? HttpStatusCode.OK : HttpStatusCode.PartialContent);
            await response.WriteAsJsonAsync(result);

            _logger.LogInformation(
                "Sync completed: {Customers} customers, {Deployments} deployments, {Errors} errors",
                result.CustomersProcessed, result.DeploymentsRecorded, result.Errors.Count);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing workflow data");

            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new
            {
                error = "Failed to sync workflow data",
                message = ex.Message
            });
            return errorResponse;
        }
    }

    [Function("SyncSpecificWorkflowRun")]
    public async Task<HttpResponseData> RunSpecific(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "sync/workflow-data/{runId}")] HttpRequestData req,
        long runId)
    {
        _logger.LogInformation("Sync specific workflow run {RunId} endpoint called", runId);

        try
        {
            var result = await _syncService.SyncWorkflowRunByIdAsync(runId);

            var response = req.CreateResponse(result.Success ? HttpStatusCode.OK : HttpStatusCode.PartialContent);
            await response.WriteAsJsonAsync(result);

            _logger.LogInformation(
                "Sync completed for run {RunId}: {Customers} customers, {Deployments} deployments, {Errors} errors",
                runId, result.CustomersProcessed, result.DeploymentsRecorded, result.Errors.Count);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing workflow run {RunId}", runId);

            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new
            {
                error = $"Failed to sync workflow run {runId}",
                message = ex.Message
            });
            return errorResponse;
        }
    }
}
