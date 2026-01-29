var builder = DistributedApplication.CreateBuilder(args);

// Azure Functions project - run as external process
builder.AddProject<Projects.DeploymentAPI>("deploymentapi")
    .WithHttpEndpoint(port: 7071, name: "http");

builder.Build().Run();
