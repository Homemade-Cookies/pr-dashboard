sealed class GitHubAuthService(GitHubTokenProvider tokenProvider, GitHubClient gitHub)
{
    public async Task<AuthStatusResponse> GetStatusAsync(CancellationToken cancellationToken)
    {
        var token = await tokenProvider.GetTokenAsync(cancellationToken);
        var login = token is null ? null : await gitHub.GetCurrentUserLoginAsync(cancellationToken);

        return new AuthStatusResponse(
            Authenticated: token is not null,
            Configured: GitHubOAuthDeviceFlow.IsConfigured,
            CanLogin: GitHubOAuthDeviceFlow.IsConfigured,
            Source: token?.Source,
            Login: login,
            Message: token is null
                ? GitHubOAuthDeviceFlow.IsConfigured
                    ? "Sign in with GitHub to let the dashboard call the GitHub API."
                    : "Set GITHUB_CLIENT_ID for GitHub login, or set GITHUB_TOKEN/GH_TOKEN, or run `gh auth login`."
                : token.Source == "oauth"
                    ? "Signed in with GitHub for this local session."
                    : "GitHub API token is available to the local backend.");
    }

    public void Logout() => tokenProvider.Logout();
}
