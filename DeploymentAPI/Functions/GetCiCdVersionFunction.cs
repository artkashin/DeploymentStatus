using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using DeploymentAPI.Repositories;
using System.Net;

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
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cicd/version")] HttpRequestData req)
    {
        try
        {
            var ciCdVersion = await _repository.GetCurrentCiCdVersionAsync();

            if (ciCdVersion == null)
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteAsJsonAsync(new 
                { 
                    error = "CI/CD version not set",
                    message = "No CI/CD version has been configured yet"
                });
                return notFoundResponse;
            }

            _logger.LogInformation("Retrieved CI/CD version: {Version}", ciCdVersion.Version);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(ciCdVersion);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving CI/CD version");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync("Error retrieving CI/CD version");
            return errorResponse;
        }
    }
}
