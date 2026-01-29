using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using DeploymentAPI.Repositories;

namespace DeploymentAPI.Functions;

public class GetClientStatusFunction
{
    private readonly ILogger<GetClientStatusFunction> _logger;
    private readonly IDeploymentRepository _repository;

    public GetClientStatusFunction(
        ILogger<GetClientStatusFunction> logger,
        IDeploymentRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    [Function("GetClientStatus")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "clients/{clientId}/status")] HttpRequest req,
        string clientId)
    {
        try
        {
            if (string.IsNullOrEmpty(clientId))
            {
                return new BadRequestObjectResult(new { error = "ClientId is required" });
            }

            var status = await _repository.GetClientStatusAsync(clientId);

            if (status == null)
            {
                return new NotFoundObjectResult(new { error = $"Client '{clientId}' not found" });
            }

            _logger.LogInformation("Retrieved status for client: {ClientId}", clientId);

            return new OkObjectResult(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving client status for {ClientId}", clientId);
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
}
