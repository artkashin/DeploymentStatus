using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using DeploymentAPI.Repositories;

namespace DeploymentAPI.Functions;

public class GetDeploymentHistoryFunction
{
    private readonly ILogger<GetDeploymentHistoryFunction> _logger;
    private readonly IDeploymentRepository _repository;

    public GetDeploymentHistoryFunction(
        ILogger<GetDeploymentHistoryFunction> logger,
        IDeploymentRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    [Function("GetDeploymentHistory")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "clients/{clientId}/history")] HttpRequest req,
        string clientId)
    {
        try
        {
            if (string.IsNullOrEmpty(clientId))
            {
                return new BadRequestObjectResult(new { error = "ClientId is required" });
            }

            var applicationId = req.Query["applicationId"].FirstOrDefault();
            var limitStr = req.Query["limit"].FirstOrDefault();
            var limit = int.TryParse(limitStr, out var l) ? l : 100;

            var history = await _repository.GetDeploymentHistoryAsync(clientId, applicationId, limit);

            _logger.LogInformation(
                "Retrieved {Count} deployment records for client: {ClientId}", 
                history.Count, 
                clientId);

            return new OkObjectResult(new
            {
                clientId,
                applicationId = applicationId ?? "all",
                count = history.Count,
                deployments = history
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving deployment history for {ClientId}", clientId);
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
}
