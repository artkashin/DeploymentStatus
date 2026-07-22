using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.IO;

namespace DeploymentAPI.Functions;

/// <summary>
/// Provides OpenAPI specification endpoint
/// </summary>
public class GetOpenApiSpecFunction
{
    private readonly ILogger<GetOpenApiSpecFunction> _logger;

    public GetOpenApiSpecFunction(ILogger<GetOpenApiSpecFunction> logger)
    {
        _logger = logger;
    }

    [Function("GetOpenApiSpec")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "swagger.json")] HttpRequest req)
    {
        _logger.LogInformation("OpenAPI specification requested");

        try
        {
            // Read the openapi.json file
            var openApiPath = Path.Combine(Directory.GetCurrentDirectory(), "openapi.json");

            if (!File.Exists(openApiPath))
            {
                _logger.LogError("openapi.json file not found at {Path}", openApiPath);
                return new NotFoundObjectResult(new { error = "OpenAPI specification not found" });
            }

            var openApiContent = await File.ReadAllTextAsync(openApiPath);

            return new ContentResult
            {
                Content = openApiContent,
                ContentType = "application/json",
                StatusCode = 200
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error serving OpenAPI specification");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
}
