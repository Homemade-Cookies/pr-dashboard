sealed class GitHubPullRequestService(GitHubClient gitHub)
{
    public Task<IReadOnlyList<PullRequestSummary>> GetPullRequestsAsync(
        RepositoryName repositoryName,
        string state,
        CancellationToken cancellationToken) =>
        gitHub.GetPullRequestsAsync(repositoryName, state, cancellationToken);

    public async Task<TimelineResponse> GetTimelineAsync(
        RepositoryName repositoryName,
        int number,
        CancellationToken cancellationToken)
    {
        var pullRequest = await gitHub.GetPullRequestDetailsAsync(repositoryName, number, cancellationToken);
        var timeline = await gitHub.GetPullRequestTimelineAsync(repositoryName, number, cancellationToken);
        var stats = TimelineStats.Create(pullRequest, timeline);

        return new TimelineResponse(repositoryName.ToString(), number, stats, timeline);
    }
}
