using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DeploymentAPI.Services;

public class KeyVaultPrivateKeyProvider : IGitHubPrivateKeyProvider
{
    private readonly string _keyVaultUrl;
    private readonly string _secretName;
    private readonly ILogger<KeyVaultPrivateKeyProvider> _logger;
    private string? _cachedKey;

    public KeyVaultPrivateKeyProvider(IConfiguration configuration, ILogger<KeyVaultPrivateKeyProvider> logger)
    {
        _keyVaultUrl = configuration["GitHub:KeyVaultUrl"] 
            ?? throw new InvalidOperationException("GitHub:KeyVaultUrl is not configured");
        _secretName = configuration["GitHub:KeyVaultSecretName"] ?? "GitHubAppPrivateKey";
        _logger = logger;
        
        _logger.LogInformation("Using Azure Key Vault private key provider. Vault: {KeyVaultUrl}, Secret: {SecretName}", 
            _keyVaultUrl, _secretName);
    }

    public async Task<string> GetPrivateKeyAsync()
    {
        if (!string.IsNullOrEmpty(_cachedKey))
        {
            return _cachedKey;
        }

        try
        {
            _logger.LogInformation("Fetching GitHub App private key from Azure Key Vault");

            var client = new SecretClient(new Uri(_keyVaultUrl), new DefaultAzureCredential());
            var secret = await client.GetSecretAsync(_secretName);

            _cachedKey = secret.Value.Value;
            
            _logger.LogInformation("Successfully retrieved private key from Azure Key Vault");
            return _cachedKey;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve GitHub App private key from Azure Key Vault");
            throw;
        }
    }

    public string GetProviderType()
    {
        return "KeyVault";
    }
}
