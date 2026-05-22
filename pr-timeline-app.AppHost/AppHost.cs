using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

var server = builder.AddProject<Projects.pr_timeline_app_Server>("server")
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints();

if (builder.Configuration.GetValue("IncludeFrontend", true))
{
    var webfrontend = builder.AddViteApp("webfrontend", "../frontend")
        .WithHttpEndpoint(port: 5173)
        .WithExternalHttpEndpoints()
        .WithReference(server)
        .WaitFor(server);

    server.PublishWithContainerFiles(webfrontend, "wwwroot");
}

builder.Build().Run();
