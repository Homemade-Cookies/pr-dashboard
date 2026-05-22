static class GitHubOAuthConfiguration
{
    public static string? ClientId => Environment.GetEnvironmentVariable("GITHUB_CLIENT_ID");

    public static string? ClientSecret => Environment.GetEnvironmentVariable("GITHUB_CLIENT_SECRET");

    public static bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ClientId)
        && !string.IsNullOrWhiteSpace(ClientSecret);
}
