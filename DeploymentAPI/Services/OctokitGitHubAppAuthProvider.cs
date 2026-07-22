using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Octokit;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace DeploymentAPI.Services;

public class OctokitGitHubAppAuthProvider : IGitHubAuthProvider
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<OctokitGitHubAppAuthProvider> _logger;
    private readonly IGitHubPrivateKeyProvider _privateKeyProvider;
    private readonly long _appId;
    private readonly long _installationId;

    private string? _cachedToken;
    private DateTimeOffset _tokenExpiration = DateTimeOffset.MinValue;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    public OctokitGitHubAppAuthProvider(
        IConfiguration configuration,
        ILogger<OctokitGitHubAppAuthProvider> logger,
        IGitHubPrivateKeyProvider privateKeyProvider)
    {
        _configuration = configuration;
        _logger = logger;
        _privateKeyProvider = privateKeyProvider;

        // Get configuration
        var appIdStr = configuration["GitHub:AppId"] 
            ?? throw new InvalidOperationException("GitHub:AppId is not configured");
        var installationIdStr = configuration["GitHub:InstallationId"] 
            ?? throw new InvalidOperationException("GitHub:InstallationId is not configured");

        if (!long.TryParse(appIdStr, out _appId))
            throw new InvalidOperationException("GitHub:AppId must be a valid number");

        if (!long.TryParse(installationIdStr, out _installationId))
            throw new InvalidOperationException("GitHub:InstallationId must be a valid number");

        _logger.LogInformation("Using Octokit GitHub App authentication (App ID: {AppId}, Installation ID: {InstallationId}, Key Provider: {KeyProvider})", 
            _appId, _installationId, _privateKeyProvider.GetProviderType());
    }

    public async Task<string> GetAuthenticationTokenAsync()
    {
        // Check if we have a valid cached token (with 5 minute buffer)
        if (!string.IsNullOrEmpty(_cachedToken) && DateTimeOffset.UtcNow < _tokenExpiration.AddMinutes(-5))
        {
            return _cachedToken;
        }

        await _tokenLock.WaitAsync();
        try
        {
            // Double-check after acquiring lock
            if (!string.IsNullOrEmpty(_cachedToken) && DateTimeOffset.UtcNow < _tokenExpiration.AddMinutes(-5))
            {
                return _cachedToken;
            }

            _logger.LogInformation("Generating new GitHub App installation token using Octokit");

            // Generate JWT token manually (same logic as old GitHubAppAuthProvider)
            var jwtToken = await GenerateJwtTokenAsync();

            // Create a client authenticated as the GitHub App
            var appClient = new GitHubClient(new ProductHeaderValue("DeploymentAPI"))
            {
                Credentials = new Credentials(jwtToken, AuthenticationType.Bearer)
            };

            // Get installation token
            var installationToken = await appClient.GitHubApps.CreateInstallationToken(_installationId);

            _cachedToken = installationToken.Token;
            _tokenExpiration = installationToken.ExpiresAt;

            _logger.LogInformation("Successfully obtained installation token. Expires at: {Expiration}", _tokenExpiration);

            return _cachedToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating GitHub App installation token");
            throw;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private async Task<string> GenerateJwtTokenAsync()
    {
        var privateKeyPem = await _privateKeyProvider.GetPrivateKeyAsync();

        // Parse the PEM key
        var rsa = ParseRsaPrivateKey(privateKeyPem);

        // Create JWT claims
        var now = DateTimeOffset.UtcNow;
        var issuedAt = now.AddSeconds(-30); // 30 second clock skew buffer
        var expiresAt = now.AddMinutes(9); // 9 minutes (well under the 10 minute GitHub maximum)

        var securityKey = new RsaSecurityKey(rsa);
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256);

        var jwtToken = new JwtSecurityToken(
            issuer: _appId.ToString(),
            claims: new[]
            {
                new Claim(JwtRegisteredClaimNames.Iat, issuedAt.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new Claim(JwtRegisteredClaimNames.Exp, expiresAt.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new Claim(JwtRegisteredClaimNames.Iss, _appId.ToString())
            },
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials
        );

        var handler = new JwtSecurityTokenHandler();
        return handler.WriteToken(jwtToken);
    }

    private static RSA ParseRsaPrivateKey(string privateKeyPem)
    {
        // Remove headers and whitespace
        var keyText = privateKeyPem
            .Replace("-----BEGIN RSA PRIVATE KEY-----", "")
            .Replace("-----END RSA PRIVATE KEY-----", "")
            .Replace("-----BEGIN PRIVATE KEY-----", "")
            .Replace("-----END PRIVATE KEY-----", "")
            .Replace("\r", "")
            .Replace("\n", "")
            .Replace(" ", "");

        var keyBytes = Convert.FromBase64String(keyText);
        var rsa = RSA.Create();

        try
        {
            // Try PKCS#1 first (RSA PRIVATE KEY)
            rsa.ImportRSAPrivateKey(keyBytes, out _);
        }
        catch
        {
            try
            {
                // Try PKCS#8 if PKCS#1 fails (PRIVATE KEY)
                rsa.ImportPkcs8PrivateKey(keyBytes, out _);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Unable to parse RSA private key. Ensure it's in PEM format (PKCS#1 or PKCS#8)", ex);
            }
        }

        return rsa;
    }

    public string GetAuthenticationType() => "GitHubApp (Octokit)";
}
