public static class GitHubAuthRoutes
{
    public static IEndpointRouteBuilder MapGitHubAuthRoutes(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api/github");

        api.MapGet("auth-status", async (GitHubAuthService auth, CancellationToken cancellationToken) =>
            Results.Ok(await auth.GetStatusAsync(cancellationToken)));

        api.MapPost("login/start", async (
            HttpContext context,
            GitHubOAuthDeviceFlow deviceFlow,
            CancellationToken cancellationToken) =>
        {
            if (!IsBrowserMutationRequest(context))
            {
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            }

            if (!GitHubOAuthDeviceFlow.IsConfigured)
            {
                return Results.Problem(
                    title: "GitHub login is not configured",
                    detail: "Set GITHUB_CLIENT_ID to a GitHub OAuth App client ID with Device Flow enabled.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            return Results.Ok(await deviceFlow.StartAsync(cancellationToken));
        });

        api.MapPost("login/poll", async (
            HttpContext context,
            GitHubOAuthDeviceFlow deviceFlow,
            CancellationToken cancellationToken) =>
        {
            if (!IsBrowserMutationRequest(context))
            {
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            }

            return Results.Ok(await deviceFlow.PollAsync(cancellationToken));
        });

        api.MapPost("logout", (HttpContext context, GitHubAuthService auth) =>
        {
            if (!IsBrowserMutationRequest(context))
            {
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            }

            auth.Logout();
            return Results.Ok(new { authenticated = false });
        });

        return endpoints;
    }

    private static bool IsBrowserMutationRequest(HttpContext context)
    {
        if (!context.Request.HasJsonContentType())
        {
            return false;
        }

        var origin = context.Request.Headers.Origin.ToString();
        return string.IsNullOrEmpty(origin)
            || Uri.TryCreate(origin, UriKind.Absolute, out var uri)
            && (uri.IsLoopback || uri.Host.Equals(context.Request.Host.Host, StringComparison.OrdinalIgnoreCase));
    }
}
