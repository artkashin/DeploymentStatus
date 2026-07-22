using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DeploymentAPI.Services;

public class GitHubPersonalTokenAuthProvider : IGitHubAuthProvider
{
    private readonly string _token;
    private readonly ILogger<GitHubPersonalTokenAuthProvider> _logger;

    public GitHubPersonalTokenAuthProvider(IConfiguration configuration, ILogger<GitHubPersonalTokenAuthProvider> logger)
    {
        _token = configuration["GitHub:Token"] 
            ?? throw new InvalidOperationException("GitHub:Token is not configured");
        _logger = logger;
        _logger.LogInformation("Using Personal Access Token authentication for GitHub");
    }

    public Task<string> GetAuthenticationTokenAsync()
    {
        return Task.FromResult(_token);
    }

    public string GetAuthenticationType()
    {
        return "PersonalAccessToken";
    }
}
