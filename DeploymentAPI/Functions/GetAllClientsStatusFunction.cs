using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using DeploymentAPI.Repositories;

namespace DeploymentAPI.Functions;

public class GetAllClientsStatusFunction
{
    private readonly ILogger<GetAllClientsStatusFunction> _logger;
    private readonly IDeploymentRepository _repository;

    public GetAllClientsStatusFunction(
        ILogger<GetAllClientsStatusFunction> logger,
        IDeploymentRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    [Function("GetAllClientsStatus")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "clients/status")] HttpRequest req)
    {
        try
        {
            var status = await _repository.GetAllClientsStatusAsync();

            _logger.LogInformation("Retrieved status for {Count} clients", status.TotalClients);

            return new OkObjectResult(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all clients status");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
}
