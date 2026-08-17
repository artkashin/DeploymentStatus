using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace DeploymentStatus.Api.Security;

public sealed record CallerContext(string Name, bool IsAuthenticated, bool IsAdaptive, IReadOnlySet<string> CustomerIds)
{
    public bool CanAccess(string customerId) => IsAdaptive || CustomerIds.Contains(customerId);
}

public sealed class CallerContextFactory(IConfiguration configuration)
{
    private const string AdaptiveRole = "DeploymentStatus.Adaptive.All";
    private const string CustomerPrefix = "DeploymentStatus.Customer.";

    public CallerContext Create(HttpRequest request)
    {
        var roles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var name = "";
        if (request.Headers.TryGetValue("X-MS-CLIENT-PRINCIPAL", out var encoded) && !string.IsNullOrWhiteSpace(encoded))
        {
            try
            {
                var json = Encoding.UTF8.GetString(Convert.FromBase64String(encoded!));
                var principal = JsonSerializer.Deserialize<ClientPrincipal>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                name = principal?.UserDetails ?? "";
                foreach (var role in principal?.UserRoles ?? []) roles.Add(role);
                foreach (var claim in principal?.Claims ?? [])
                    if (claim.Type.EndsWith("/role", StringComparison.OrdinalIgnoreCase) || claim.Type.Equals("roles", StringComparison.OrdinalIgnoreCase))
                        roles.Add(claim.Value);
            }
            catch (FormatException) { }
            catch (JsonException) { }
        }
        if (configuration.GetValue("Authorization:AllowDevelopmentHeaders", false))
        {
            name = request.Headers["X-Development-User"].FirstOrDefault() ?? name;
            foreach (var role in request.Headers["X-Development-Roles"].ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                roles.Add(role);
        }
        var customerIds = roles.Where(role => role.StartsWith(CustomerPrefix, StringComparison.OrdinalIgnoreCase))
            .Select(role => role[CustomerPrefix.Length..].ToLowerInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return new CallerContext(name, !string.IsNullOrWhiteSpace(name), roles.Contains(AdaptiveRole), customerIds);
    }

    private sealed class ClientPrincipal
    {
        [System.Text.Json.Serialization.JsonPropertyName("user_details")] public string? UserDetails { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("user_roles")] public string[]? UserRoles { get; set; }
        public ClientClaim[]? Claims { get; set; }
    }
    private sealed class ClientClaim
    {
        [System.Text.Json.Serialization.JsonPropertyName("typ")] public string Type { get; set; } = "";
        [System.Text.Json.Serialization.JsonPropertyName("val")] public string Value { get; set; } = "";
    }
}
