using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using DeploymentAPI.Services;

namespace DeploymentAPI.Functions;

public class GetLatestUpdateCustomersStatusFunction
{
    private readonly ILogger<GetLatestUpdateCustomersStatusFunction> _logger;
    private readonly IGitHubService _gitHubService;

    public GetLatestUpdateCustomersStatusFunction(
        ILogger<GetLatestUpdateCustomersStatusFunction> logger,
        IGitHubService gitHubService)
    {
        _logger = logger;
        _gitHubService = gitHubService;
    }

    [Function("GetLatestUpdateCustomersStatus")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "update-all-customers/latest")] HttpRequest req)
    {
        _logger.LogInformation("Getting latest 'Update all customers' workflow run status");

        try
        {
            // Get all workflow runs
            var runs = await _gitHubService.GetWorkflowRunsAsync();

            // Filter for "Update all customers" workflow and get the latest
            var updateCustomersRun = runs
                .Where(r => r.Name == "Update all customers")
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefault();

            if (updateCustomersRun == null)
            {
                _logger.LogWarning("No 'Update all customers' workflow runs found");
                return new NotFoundObjectResult(new { error = "No 'Update all customers' workflow runs found" });
            }

            _logger.LogInformation("Found latest 'Update all customers' run: {RunId} (Run #{RunNumber})", 
                updateCustomersRun.Id, updateCustomersRun.RunNumber);

            // Get customer status for this run
            var customerStatus = await _gitHubService.GetWorkflowRunCustomerStatusAsync(updateCustomersRun.Id);

            _logger.LogInformation("Successfully retrieved customer status: {Total} customers, {Success} installed, {Failed} failed",
                customerStatus.TotalCustomers, customerStatus.SuccessfulInstallations, customerStatus.FailedInstallations);

            return new OkObjectResult(customerStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting latest 'Update all customers' status");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
}
