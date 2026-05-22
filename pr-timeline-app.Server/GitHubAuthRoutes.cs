using AspNet.Security.OAuth.GitHub;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

public static class GitHubAuthRoutes
{
    public static IEndpointRouteBuilder MapGitHubAuthRoutes(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api/github");

        api.MapGet("auth-status", async (GitHubAuthService auth, CancellationToken cancellationToken) =>
            Results.Ok(await auth.GetStatusAsync(cancellationToken)));

        api.MapGet("login", ([FromQuery] string? returnUrl) =>
        {
            if (!GitHubOAuthConfiguration.IsConfigured)
            {
                return Results.Problem(
                    title: "GitHub login is not configured",
                    detail: "Set GITHUB_CLIENT_ID and GITHUB_CLIENT_SECRET for a GitHub OAuth App.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            if (!TryNormalizeLocalReturnUrl(returnUrl, out var localReturnUrl))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["returnUrl"] = ["Return URL must be a local path."]
                });
            }

            return Results.Challenge(
                new AuthenticationProperties { RedirectUri = localReturnUrl },
                [GitHubAuthenticationDefaults.AuthenticationScheme]);
        });

        api.MapPost("logout", async (HttpContext context, GitHubAuthService auth) =>
        {
            if (!IsBrowserMutationRequest(context))
            {
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            }

            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
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

    private static bool TryNormalizeLocalReturnUrl(string? returnUrl, out string localReturnUrl)
    {
        localReturnUrl = "/";

        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return true;
        }

        if (!Uri.TryCreate(returnUrl, UriKind.Relative, out _)
            || !returnUrl.StartsWith("/", StringComparison.Ordinal)
            || returnUrl.StartsWith("//", StringComparison.Ordinal)
            || returnUrl.Contains("\\", StringComparison.Ordinal))
        {
            return false;
        }

        localReturnUrl = returnUrl;
        return true;
    }
}
