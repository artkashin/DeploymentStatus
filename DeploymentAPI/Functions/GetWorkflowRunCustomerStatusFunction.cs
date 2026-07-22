using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using DeploymentAPI.Services;

namespace DeploymentAPI.Functions;

public class GetWorkflowRunCustomerStatusFunction
{
    private readonly ILogger<GetWorkflowRunCustomerStatusFunction> _logger;
    private readonly IGitHubService _gitHubService;

    public GetWorkflowRunCustomerStatusFunction(
        ILogger<GetWorkflowRunCustomerStatusFunction> logger,
        IGitHubService gitHubService)
    {
        _logger = logger;
        _gitHubService = gitHubService;
    }

    [Function("GetWorkflowRunCustomerStatus")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "workflow-runs/{runId}/customer-status")] HttpRequest req,
        long runId)
    {
        _logger.LogInformation("Getting customer installation status for workflow run {RunId}", runId);

        try
        {
            var customerStatus = await _gitHubService.GetWorkflowRunCustomerStatusAsync(runId);

            _logger.LogInformation("Successfully retrieved customer status for run {RunId}: {Total} customers, {Success} installed, {Failed} failed",
                runId, customerStatus.TotalCustomers, customerStatus.SuccessfulInstallations, customerStatus.FailedInstallations);

            return new OkObjectResult(customerStatus);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Workflow run {RunId} not found", runId);
            return new NotFoundObjectResult(new { error = $"Workflow run {runId} not found", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer status for workflow run {RunId}", runId);
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
}
