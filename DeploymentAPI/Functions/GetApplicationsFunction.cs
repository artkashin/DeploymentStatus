using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using DeploymentAPI.Repositories;
using System.Net;
using System.Text.Json;

namespace DeploymentAPI.Functions;

public class GetApplicationsFunction
{
    private readonly IDeploymentRepository _repository;
    private readonly ILogger<GetApplicationsFunction> _logger;

    public GetApplicationsFunction(
        IDeploymentRepository repository,
        ILogger<GetApplicationsFunction> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [Function("GetApplications")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "applications")] HttpRequestData req)
    {
        _logger.LogInformation("Getting all applications");

        try
        {
            var applications = await _repository.GetAllApplicationsAsync();

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(applications);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting applications");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error getting applications: {ex.Message}");
            return errorResponse;
        }
    }
}
