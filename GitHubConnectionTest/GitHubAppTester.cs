using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Net.Http.Headers;
using System.Text.Json;

// Simple GitHub App authenticator for testing
public class GitHubAppTester
{
    private readonly IConfiguration _configuration;
    private readonly ILogger _logger;
    private readonly HttpClient _httpClient;

    public GitHubAppTester(IConfiguration configuration, ILogger logger)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("GitHubConnectionTest", "1.0"));
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        _httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            _logger.LogInformation("=== GitHub App Connection Test ===");
            _logger.LogInformation("");

            // Step 1: Load configuration
            _logger.LogInformation("1. Loading configuration...");
            var appIdStr = _configuration["GitHub:AppId"];
            var installationIdStr = _configuration["GitHub:InstallationId"];
            var pemPath = _configuration["GitHub:PrivateKeyPath"];
            var owner = _configuration["GitHub:Owner"];
            var repo = _configuration["GitHub:Repository"];

            if (string.IsNullOrEmpty(appIdStr) || string.IsNullOrEmpty(installationIdStr))
            {
                _logger.LogError("   ✗ Configuration missing!");
                return false;
            }

            var appId = long.Parse(appIdStr);
            var installationId = long.Parse(installationIdStr);

            _logger.LogInformation($"   ✓ App ID: {appId}");
            _logger.LogInformation($"   ✓ Installation ID: {installationId}");
            _logger.LogInformation($"   ✓ Owner: {owner}");
            _logger.LogInformation($"   ✓ Repository: {repo}");
            _logger.LogInformation($"   ✓ PEM Path: {pemPath}");
            _logger.LogInformation("");

            // Step 2: Load PEM file
            _logger.LogInformation("2. Loading PEM file...");
            if (!File.Exists(pemPath))
            {
                _logger.LogError($"   ✗ PEM file not found: {pemPath}");
                _logger.LogError($"   Current directory: {Directory.GetCurrentDirectory()}");
                return false;
            }

            var pemContent = await File.ReadAllTextAsync(pemPath);
            _logger.LogInformation($"   ✓ PEM file loaded ({pemContent.Length} characters)");

            if (!pemContent.Contains("-----BEGIN") || !pemContent.Contains("-----END"))
            {
                _logger.LogError("   ✗ PEM file format invalid!");
                return false;
            }
            _logger.LogInformation("   ✓ PEM format valid");
            _logger.LogInformation("");

            // Step 3: Generate JWT
            _logger.LogInformation("3. Generating JWT token...");
            var jwt = GenerateJwtToken(appId, pemContent);
            _logger.LogInformation($"   ✓ JWT generated: {jwt.Substring(0, Math.Min(50, jwt.Length))}...");
            _logger.LogInformation("");

            // Step 4: Verify GitHub App with JWT
            _logger.LogInformation("4. Verifying GitHub App...");
            var appInfo = await VerifyGitHubAppAsync(jwt);
            if (appInfo == null)
            {
                _logger.LogError("   ✗ Failed to verify GitHub App!");
                return false;
            }
            _logger.LogInformation($"   ✓ App Name: {appInfo.Name}");
            _logger.LogInformation($"   ✓ App Owner: {appInfo.Owner}");
            _logger.LogInformation("");

            // Step 5: Get installation token
            _logger.LogInformation("5. Getting installation access token...");
            var installationToken = await GetInstallationTokenAsync(jwt, installationId);
            if (string.IsNullOrEmpty(installationToken))
            {
                _logger.LogError("   ✗ Failed to get installation token!");
                return false;
            }
            _logger.LogInformation($"   ✓ Installation token obtained: {installationToken.Substring(0, 20)}...");
            _logger.LogInformation("");

            // Step 6: Test API with installation token
            _logger.LogInformation("6. Testing GitHub API...");
            var workflows = await GetWorkflowRunsAsync(installationToken, owner, repo);
            if (workflows == null)
            {
                _logger.LogError("   ✗ Failed to fetch workflow runs!");
                return false;
            }

            _logger.LogInformation($"   ✓ Successfully retrieved {workflows.Count} workflow runs!");
            _logger.LogInformation("");

            if (workflows.Count > 0)
            {
                _logger.LogInformation("   Recent workflows:");
                foreach (var wf in workflows.Take(5))
                {
                    var icon = wf.Conclusion switch
                    {
                        "success" => "✓",
                        "failure" => "✗",
                        _ => "•"
                    };
                    _logger.LogInformation($"   {icon} {wf.Name} - {wf.Status}/{wf.Conclusion}");
                }
                _logger.LogInformation("");
            }

            _logger.LogInformation("=== ✓ SUCCESS! GitHub App authentication is working! ===");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "=== ✗ FAILED! ===");
            _logger.LogError($"Error: {ex.Message}");
            if (ex.InnerException != null)
            {
                _logger.LogError($"Inner: {ex.InnerException.Message}");
            }
            return false;
        }
    }

    private string GenerateJwtToken(long appId, string privateKeyPem)
    {
        var now = DateTimeOffset.UtcNow;
        var expires = now.AddMinutes(10);

        // Parse the private key
        var privateKeyContent = privateKeyPem
            .Replace("-----BEGIN RSA PRIVATE KEY-----", "")
            .Replace("-----END RSA PRIVATE KEY-----", "")
            .Replace("-----BEGIN PRIVATE KEY-----", "")
            .Replace("-----END PRIVATE KEY-----", "")
            .Replace("\n", "")
            .Replace("\r", "")
            .Trim();

        var privateKeyBytes = Convert.FromBase64String(privateKeyContent);

        var rsa = RSA.Create();
        rsa.ImportRSAPrivateKey(privateKeyBytes, out _);

        var signingCredentials = new SigningCredentials(
            new RsaSecurityKey(rsa),
            SecurityAlgorithms.RsaSha256);

        // Create claims including 'iat' which GitHub requires
        var claims = new[]
        {
            new System.Security.Claims.Claim("iat", new DateTimeOffset(now.UtcDateTime).ToUnixTimeSeconds().ToString())
        };

        var jwt = new JwtSecurityToken(
            issuer: appId.ToString(),
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: signingCredentials);

        var tokenHandler = new JwtSecurityTokenHandler();
        return tokenHandler.WriteToken(jwt);
    }

    private async Task<GitHubAppInfo?> VerifyGitHubAppAsync(string jwt)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/app");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError($"   GitHub API Error: {response.StatusCode}");
                _logger.LogError($"   Response: {error}");
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var appData = JsonSerializer.Deserialize<JsonElement>(content);

            return new GitHubAppInfo
            {
                Name = appData.GetProperty("name").GetString() ?? "Unknown",
                Owner = appData.GetProperty("owner").GetProperty("login").GetString() ?? "Unknown"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"   Exception verifying app: {ex.Message}");
            return null;
        }
    }

    private async Task<string?> GetInstallationTokenAsync(string jwt, long installationId)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, 
                $"https://api.github.com/app/installations/{installationId}/access_tokens");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError($"   GitHub API Error: {response.StatusCode}");
                _logger.LogError($"   Response: {error}");

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogError($"   → Installation ID {installationId} not found!");
                    _logger.LogError("   → Check: https://github.com/settings/installations");
                }

                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var tokenData = JsonSerializer.Deserialize<JsonElement>(content);
            return tokenData.GetProperty("token").GetString();
        }
        catch (Exception ex)
        {
            _logger.LogError($"   Exception getting installation token: {ex.Message}");
            return null;
        }
    }

    private async Task<List<WorkflowRun>?> GetWorkflowRunsAsync(string token, string owner, string repo)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, 
                $"https://api.github.com/repos/{owner}/{repo}/actions/runs?per_page=10");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError($"   GitHub API Error: {response.StatusCode}");
                _logger.LogError($"   Response: {error}");

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogError($"   → Repository {owner}/{repo} not found or not accessible!");
                }

                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(content);
            var workflows = new List<WorkflowRun>();

            if (data.TryGetProperty("workflow_runs", out var runs))
            {
                foreach (var run in runs.EnumerateArray())
                {
                    workflows.Add(new WorkflowRun
                    {
                        Name = run.GetProperty("name").GetString() ?? "Unknown",
                        Status = run.GetProperty("status").GetString() ?? "Unknown",
                        Conclusion = run.TryGetProperty("conclusion", out var c) ? c.GetString() : null
                    });
                }
            }

            return workflows;
        }
        catch (Exception ex)
        {
            _logger.LogError($"   Exception fetching workflows: {ex.Message}");
            return null;
        }
    }

    private class GitHubAppInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Owner { get; set; } = string.Empty;
    }

    private class WorkflowRun
    {
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Conclusion { get; set; }
    }
}
