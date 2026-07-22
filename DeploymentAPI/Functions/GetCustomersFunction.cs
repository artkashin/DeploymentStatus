using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using DeploymentAPI.Repositories;
using System.Net;
using System.Text.Json;

namespace DeploymentAPI.Functions;

public class GetCustomersFunction
{
    private readonly IDeploymentRepository _repository;
    private readonly ILogger<GetCustomersFunction> _logger;

    public GetCustomersFunction(
        IDeploymentRepository repository,
        ILogger<GetCustomersFunction> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [Function("GetCustomers")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "customers")] HttpRequestData req)
    {
        _logger.LogInformation("Getting all customers");

        try
        {
            var customers = await _repository.GetAllCustomersAsync();

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(customers);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customers");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error getting customers: {ex.Message}");
            return errorResponse;
        }
    }
}
