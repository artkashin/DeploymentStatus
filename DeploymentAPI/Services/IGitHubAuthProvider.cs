namespace DeploymentAPI.Services;

public interface IGitHubAuthProvider
{
    Task<string> GetAuthenticationTokenAsync();
    string GetAuthenticationType();
}
