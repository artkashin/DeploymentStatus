using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using DeploymentAPI.Repositories;

namespace DeploymentAPI.Functions;

public class GetCiCdVersionFunction
{
    private readonly ILogger<GetCiCdVersionFunction> _logger;
    private readonly IDeploymentRepository _repository;

    public GetCiCdVersionFunction(
        ILogger<GetCiCdVersionFunction> logger,
        IDeploymentRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    [Function("GetCiCdVersion")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "cicd/version")] HttpRequest req)
    {
        try
        {
            var ciCdVersion = await _repository.GetCurrentCiCdVersionAsync();

            if (ciCdVersion == null)
            {
                return new NotFoundObjectResult(new 
                { 
                    error = "CI/CD version not set",
                    message = "No CI/CD version has been configured yet"
                });
            }

            _logger.LogInformation("Retrieved CI/CD version: {Version}", ciCdVersion.Version);

            return new OkObjectResult(ciCdVersion);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving CI/CD version");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
}
