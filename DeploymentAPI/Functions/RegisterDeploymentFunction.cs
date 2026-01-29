using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using DeploymentAPI.Models;
using DeploymentAPI.Repositories;
using System.Text.Json;

namespace DeploymentAPI.Functions;

public class RegisterDeploymentFunction
{
    private readonly ILogger<RegisterDeploymentFunction> _logger;
    private readonly IDeploymentRepository _repository;

    public RegisterDeploymentFunction(
        ILogger<RegisterDeploymentFunction> logger,
        IDeploymentRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    [Function("RegisterDeployment")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "deployments")] HttpRequest req)
    {
        try
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var request = JsonSerializer.Deserialize<RegisterDeploymentRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (request == null)
            {
                return new BadRequestObjectResult(new { error = "Invalid request body" });
            }

            if (string.IsNullOrEmpty(request.ClientId) || 
                string.IsNullOrEmpty(request.ApplicationId) || 
                string.IsNullOrEmpty(request.Version))
            {
                return new BadRequestObjectResult(new { error = "ClientId, ApplicationId, and Version are required" });
            }

            var deployment = new DeploymentRecord
            {
                ClientId = request.ClientId,
                ClientName = request.ClientName,
                ApplicationId = request.ApplicationId,
                ApplicationName = request.ApplicationName,
                Version = request.Version,
                Status = request.Status,
                DeploymentTime = DateTime.UtcNow
            };

            await _repository.RegisterDeploymentAsync(deployment);

            _logger.LogInformation(
                "Registered deployment for Client: {ClientId}, Application: {ApplicationId}, Version: {Version}",
                request.ClientId, request.ApplicationId, request.Version);

            return new OkObjectResult(new
            {
                message = "Deployment registered successfully",
                deployment = new
                {
                    deployment.ClientId,
                    deployment.ApplicationId,
                    deployment.Version,
                    deployment.DeploymentTime
                }
            });
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse request body");
            return new BadRequestObjectResult(new { error = "Invalid JSON format" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering deployment");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
}
