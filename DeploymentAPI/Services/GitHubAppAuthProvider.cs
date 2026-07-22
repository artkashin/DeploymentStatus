using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace DeploymentAPI.Services;

public class GitHubAppAuthProvider : IGitHubAuthProvider
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GitHubAppAuthProvider> _logger;
    private readonly IGitHubPrivateKeyProvider _privateKeyProvider;
    private readonly long _appId;
    private readonly long _installationId;
    
    private string? _cachedToken;
    private DateTime _tokenExpiration = DateTime.MinValue;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    public GitHubAppAuthProvider(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GitHubAppAuthProvider> logger,
        IGitHubPrivateKeyProvider privateKeyProvider)
    {
        _httpClient = httpClient;
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

        _httpClient.BaseAddress = new Uri("https://api.github.com/");
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("DeploymentAPI", "1.0"));
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        _httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");

        _logger.LogInformation("Using GitHub App authentication (App ID: {AppId}, Installation ID: {InstallationId}, Key Provider: {KeyProvider})", 
            _appId, _installationId, _privateKeyProvider.GetProviderType());
    }

    public async Task<string> GetAuthenticationTokenAsync()
    {
        // Check if we have a valid cached token
        if (!string.IsNullOrEmpty(_cachedToken) && _tokenExpiration > DateTime.MinValue && DateTime.UtcNow < _tokenExpiration.AddMinutes(-5))
        {
            return _cachedToken;
        }

        await _tokenLock.WaitAsync();
        try
        {
            // Double-check after acquiring lock
            if (!string.IsNullOrEmpty(_cachedToken) && _tokenExpiration > DateTime.MinValue && DateTime.UtcNow < _tokenExpiration.AddMinutes(-5))
            {
                return _cachedToken;
            }

            _logger.LogInformation("Generating new GitHub App installation token");

            // Generate JWT for GitHub App authentication
            var jwt = GenerateJwtToken();

            // Get installation access token
            var request = new HttpRequestMessage(HttpMethod.Post, 
                $"app/installations/{_installationId}/access_tokens");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("GitHub API returned {StatusCode}: {ErrorContent}", 
                    response.StatusCode, errorContent);
                response.EnsureSuccessStatusCode(); // This will throw with the status code
            }

            var content = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<GitHubInstallationTokenResponse>(content, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (tokenResponse?.Token == null)
                throw new InvalidOperationException("Failed to get installation access token from GitHub");

            _cachedToken = tokenResponse.Token;
            _tokenExpiration = tokenResponse.ExpiresAt;

            _logger.LogInformation("Successfully obtained GitHub App installation token (expires at {ExpiresAt})", 
                _tokenExpiration);

            return _cachedToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to obtain GitHub App installation token");
            throw;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    public string GetAuthenticationType()
    {
        return "GitHubApp";
    }

    private string GenerateJwtToken()
    {
        // Get current time and subtract a small buffer to account for clock skew
        var now = DateTimeOffset.UtcNow.AddSeconds(-30);  // Subtract 30 seconds for clock skew
        var expires = now.AddMinutes(9); // GitHub requires JWT to be valid for max 10 minutes, use 9 to be safe

        // Get private key from provider
        var privateKey = _privateKeyProvider.GetPrivateKeyAsync().GetAwaiter().GetResult();

        // Parse the private key
        RSA rsa;
        try
        {
            // Get the raw PEM content
            var privateKeyPem = privateKey.Trim();

            // Determine the format and extract the base64 content
            bool isPkcs1 = privateKeyPem.Contains("-----BEGIN RSA PRIVATE KEY-----");
            bool isPkcs8 = privateKeyPem.Contains("-----BEGIN PRIVATE KEY-----");

            if (!isPkcs1 && !isPkcs8)
            {
                throw new InvalidOperationException("Private key must be in PEM format (PKCS#1 or PKCS#8)");
            }

            // Remove headers, footers and whitespace
            var base64Key = privateKeyPem
                .Replace("-----BEGIN RSA PRIVATE KEY-----", "")
                .Replace("-----END RSA PRIVATE KEY-----", "")
                .Replace("-----BEGIN PRIVATE KEY-----", "")
                .Replace("-----END PRIVATE KEY-----", "")
                .Replace("\n", "")
                .Replace("\r", "")
                .Replace(" ", "")
                .Trim();

            var privateKeyBytes = Convert.FromBase64String(base64Key);

            rsa = RSA.Create();

            // Try PKCS#8 first (more common for GitHub Apps), then fall back to PKCS#1
            try
            {
                if (isPkcs8)
                {
                    rsa.ImportPkcs8PrivateKey(privateKeyBytes, out _);
                    _logger.LogInformation("Successfully imported PKCS#8 private key");
                }
                else
                {
                    rsa.ImportRSAPrivateKey(privateKeyBytes, out _);
                    _logger.LogInformation("Successfully imported PKCS#1 (RSA) private key");
                }
            }
            catch
            {
                // If the detected format fails, try the other one
                _logger.LogWarning("Failed to import with detected format, trying alternative...");
                if (isPkcs8)
                {
                    rsa.ImportRSAPrivateKey(privateKeyBytes, out _);
                    _logger.LogInformation("Successfully imported as PKCS#1 despite PKCS#8 header");
                }
                else
                {
                    rsa.ImportPkcs8PrivateKey(privateKeyBytes, out _);
                    _logger.LogInformation("Successfully imported as PKCS#8 despite RSA header");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse GitHub App private key");
            throw new InvalidOperationException("Invalid GitHub App private key format", ex);
        }

        var signingCredentials = new SigningCredentials(
            new RsaSecurityKey(rsa), 
            SecurityAlgorithms.RsaSha256);

        // Create claims including 'iat' (issued at) which GitHub requires
        var claims = new[]
        {
            new System.Security.Claims.Claim("iat", new DateTimeOffset(now.UtcDateTime).ToUnixTimeSeconds().ToString())
        };

        var jwt = new JwtSecurityToken(
            issuer: _appId.ToString(),
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: signingCredentials);

        var tokenHandler = new JwtSecurityTokenHandler();
        return tokenHandler.WriteToken(jwt);
    }

    private class GitHubInstallationTokenResponse
    {
        public string? Token { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
