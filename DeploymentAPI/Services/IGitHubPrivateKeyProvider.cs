namespace DeploymentAPI.Services;

public interface IGitHubPrivateKeyProvider
{
    Task<string> GetPrivateKeyAsync();
    string GetProviderType();
}

