using DeploymentStatus.Api.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace DeploymentAPI.Tests;

public sealed class CallerContextTests
{
    [Fact]
    public void Easy_auth_principal_maps_group_assigned_app_roles()
    {
        var configuration = new ConfigurationBuilder().Build();
        var principal = """{"user_details":"guest@customer.test","user_roles":["authenticated"],"claims":[{"typ":"http://schemas.microsoft.com/ws/2008/06/identity/claims/role","val":"DeploymentStatus.Customer.Riddle"},{"typ":"roles","val":"DeploymentStatus.Customer.Tappers"}]}""";
        var request = new DefaultHttpContext().Request;
        request.Headers["X-MS-CLIENT-PRINCIPAL"] = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(principal));

        var caller = new CallerContextFactory(configuration).Create(request);

        Assert.True(caller.IsAuthenticated);
        Assert.Equal("guest@customer.test", caller.Name);
        Assert.Equal(["riddle", "tappers"], caller.CustomerIds.Order().ToArray());
    }

    [Fact]
    public void Development_roles_map_to_adaptive_and_customer_scopes()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authorization:AllowDevelopmentHeaders"] = "true"
        }).Build();
        var request = new DefaultHttpContext().Request;
        request.Headers["X-Development-User"] = "tester@example.com";
        request.Headers["X-Development-Roles"] = "DeploymentStatus.Adaptive.All,DeploymentStatus.Customer.Tappers";
        var caller = new CallerContextFactory(configuration).Create(request);
        Assert.True(caller.IsAuthenticated);
        Assert.True(caller.IsAdaptive);
        Assert.Contains("tappers", caller.CustomerIds);
    }
}
