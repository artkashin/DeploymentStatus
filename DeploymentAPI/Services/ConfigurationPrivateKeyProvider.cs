using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DeploymentAPI.Services;

public class ConfigurationPrivateKeyProvider : IGitHubPrivateKeyProvider
{
    private readonly string _privateKey;
    private readonly ILogger<ConfigurationPrivateKeyProvider> _logger;

    public ConfigurationPrivateKeyProvider(IConfiguration configuration, ILogger<ConfigurationPrivateKeyProvider> logger)
    {
        _privateKey = configuration["GitHub:PrivateKey"] 
            ?? throw new InvalidOperationException("GitHub:PrivateKey is not configured");
        _logger = logger;
        
        _logger.LogInformation("Using configuration-based private key provider");
    }

    public Task<string> GetPrivateKeyAsync()
    {
        return Task.FromResult(_privateKey);
    }

    public string GetProviderType()
    {
        return "Configuration";
    }
}
