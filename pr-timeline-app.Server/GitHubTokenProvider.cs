using System.ComponentModel;
using System.Diagnostics;

sealed class GitHubTokenProvider
{
    private readonly SemaphoreSlim semaphore = new(1, 1);
    private TokenResult? oauthToken;
    private TokenResult? cachedGitHubCliToken;
    private bool attemptedGitHubCli;
    private bool suppressFallback;

    public long AuthGeneration { get; private set; }

    public void SetOAuthToken(string token)
    {
        oauthToken = new TokenResult(token, "oauth");
        suppressFallback = false;
        AuthGeneration++;
    }

    public void Logout()
    {
        oauthToken = null;
        suppressFallback = true;
        AuthGeneration++;
    }

    public async Task<TokenResult?> GetTokenAsync(CancellationToken cancellationToken)
    {
        if (oauthToken is not null)
        {
            return oauthToken;
        }

        if (suppressFallback)
        {
            return null;
        }

        var environmentToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN")
            ?? Environment.GetEnvironmentVariable("GH_TOKEN");
        if (!string.IsNullOrWhiteSpace(environmentToken))
        {
            return new TokenResult(environmentToken.Trim(), "environment");
        }

        await semaphore.WaitAsync(cancellationToken);
        try
        {
            if (cachedGitHubCliToken is not null)
            {
                return cachedGitHubCliToken;
            }

            if (attemptedGitHubCli)
            {
                return null;
            }

            attemptedGitHubCli = true;
            var ghToken = await GetGitHubCliTokenAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(ghToken))
            {
                cachedGitHubCliToken = new TokenResult(ghToken.Trim(), "gh");
                return cachedGitHubCliToken;
            }

            return null;
        }
        finally
        {
            semaphore.Release();
        }
    }

    private static async Task<string?> GetGitHubCliTokenAsync(CancellationToken cancellationToken)
    {
        Process? process;
        try
        {
            process = Process.Start(new ProcessStartInfo
            {
                FileName = "gh",
                ArgumentList = { "auth", "token" },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            });
        }
        catch (Win32Exception)
        {
            return null;
        }

        using (process)
        {
            if (process is null)
            {
                return null;
            }

            try
            {
                await process.WaitForExitAsync(cancellationToken).WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
            }
            catch (TimeoutException)
            {
                process.Kill(entireProcessTree: true);
                return null;
            }

            if (process.ExitCode != 0)
            {
                return null;
            }

            return await process.StandardOutput.ReadToEndAsync(cancellationToken);
        }
    }
}
