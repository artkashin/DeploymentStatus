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
        // App Service Easy Auth validates the bearer token before this Function is invoked.
        // Flex/Linux can omit X-MS-CLIENT-PRINCIPAL for an otherwise authenticated bearer
        // request, so recover only the name and app-role claims from that validated token.
        if (string.IsNullOrWhiteSpace(name)) AddValidatedBearerClaims(request, roles, ref name);
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

    private static void AddValidatedBearerClaims(HttpRequest request, ISet<string> roles, ref string name)
    {
        var authorization = request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(authorization) || !authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return;
        var parts = authorization[7..].Split('.');
        if (parts.Length != 3) return;
        try
        {
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            using var document = JsonDocument.Parse(Convert.FromBase64String(payload));
            var root = document.RootElement;
            foreach (var role in Strings(root, "roles")) roles.Add(role);
            if (string.IsNullOrWhiteSpace(name))
                name = Strings(root, "preferred_username").FirstOrDefault()
                    ?? Strings(root, "email").FirstOrDefault()
                    ?? Strings(root, "name").FirstOrDefault()
                    ?? "";
        }
        catch (FormatException) { }
        catch (JsonException) { }
    }

    private static IEnumerable<string> Strings(JsonElement root, string property)
    {
        if (!root.TryGetProperty(property, out var value)) yield break;
        if (value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString())) yield return value.GetString()!;
        if (value.ValueKind != JsonValueKind.Array) yield break;
        foreach (var item in value.EnumerateArray())
            if (item.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString())) yield return item.GetString()!;
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
