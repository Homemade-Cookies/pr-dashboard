var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddGitHubApiServices();

var app = builder.Build();

app.UseGitHubApiExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGitHubAuthRoutes();
app.MapGitHubPullRequestRoutes();

app.MapDefaultEndpoints();

app.UseFileServer();

app.Run();

public partial class Program;
