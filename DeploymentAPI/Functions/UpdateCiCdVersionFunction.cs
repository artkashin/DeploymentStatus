using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using DeploymentAPI.Models;
using DeploymentAPI.Repositories;
using System.Text.Json;

namespace DeploymentAPI.Functions;

public class UpdateCiCdVersionFunction
{
    private readonly ILogger<UpdateCiCdVersionFunction> _logger;
    private readonly IDeploymentRepository _repository;

    public UpdateCiCdVersionFunction(
        ILogger<UpdateCiCdVersionFunction> logger,
        IDeploymentRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    [Function("UpdateCiCdVersion")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", "put", Route = "cicd/version")] HttpRequest req)
    {
        try
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var request = JsonSerializer.Deserialize<UpdateCiCdVersionRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (request == null)
            {
                return new BadRequestObjectResult(new { error = "Invalid request body" });
            }

            if (string.IsNullOrEmpty(request.Version))
            {
                return new BadRequestObjectResult(new { error = "Version is required" });
            }

            var ciCdVersion = new CiCdVersion
            {
                Version = request.Version,
                UpdatedBy = request.UpdatedBy,
                Notes = request.Notes,
                UpdatedAt = DateTime.UtcNow
            };

            await _repository.UpdateCiCdVersionAsync(ciCdVersion);

            _logger.LogInformation(
                "Updated CI/CD version to {Version} by {UpdatedBy}",
                request.Version, request.UpdatedBy ?? "unknown");

            return new OkObjectResult(new
            {
                message = "CI/CD version updated successfully",
                version = ciCdVersion
            });
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse request body");
            return new BadRequestObjectResult(new { error = "Invalid JSON format" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating CI/CD version");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
}
