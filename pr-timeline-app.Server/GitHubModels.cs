using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

readonly partial record struct RepositoryName(string Owner, string Name)
{
    public static bool TryParse(string value, out RepositoryName repositoryName)
    {
        repositoryName = default;

        if (RepositoryRegex().Match(value.Trim()) is not { Success: true } match)
        {
            return false;
        }

        repositoryName = new RepositoryName(match.Groups["owner"].Value, match.Groups["repo"].Value);
        return true;
    }

    public override string ToString() => $"{Owner}/{Name}";

    [GeneratedRegex("^(?<owner>[A-Za-z0-9._-]+)/(?<repo>[A-Za-z0-9._-]+)$")]
    private static partial Regex RepositoryRegex();
}

sealed class GitHubApiException(HttpStatusCode statusCode, string message) : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}

record TokenResult(string Value, string Source);

record AuthStatusResponse(bool Authenticated, bool Configured, bool CanLogin, string? Source, string? Login, string Message);

record DeviceLoginResponse(
    string Status,
    string? UserCode,
    string? VerificationUri,
    string? VerificationUriComplete,
    int IntervalSeconds,
    DateTimeOffset? ExpiresAt,
    string Message);

record PullRequestListResponse(string Repository, IReadOnlyList<PullRequestSummary> PullRequests);

record PullRequestSummary(
    int Number,
    string Title,
    string State,
    bool Draft,
    string Author,
    string HtmlUrl,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<string> Labels,
    IReadOnlyList<string> RequestedReviewers,
    ReviewStatus Review)
{
    public static PullRequestSummary FromJson(JsonElement element) =>
        new(
            element.GetProperty("number").GetInt32(),
            element.GetProperty("title").GetString() ?? "",
            element.GetProperty("state").GetString() ?? "",
            element.GetProperty("draft").GetBoolean(),
            GetNestedString(element, "user", "login") ?? "unknown",
            element.GetProperty("html_url").GetString() ?? "",
            element.GetProperty("created_at").GetDateTimeOffset(),
            element.GetProperty("updated_at").GetDateTimeOffset(),
            element.GetProperty("labels")
                .EnumerateArray()
                .Select(label => label.GetProperty("name").GetString())
                .Where(label => !string.IsNullOrWhiteSpace(label))
                .Select(label => label!)
                .ToArray(),
            element.GetProperty("requested_reviewers")
                .EnumerateArray()
                .Select(reviewer => reviewer.GetProperty("login").GetString())
                .Concat(element.GetProperty("requested_teams")
                    .EnumerateArray()
                    .Select(team => team.GetProperty("name").GetString()))
                .Where(reviewer => !string.IsNullOrWhiteSpace(reviewer))
                .Select(reviewer => reviewer!)
                .ToArray(),
            ReviewStatus.Waiting);

    private static string? GetNestedString(JsonElement element, string propertyName, string nestedPropertyName) =>
        element.TryGetProperty(propertyName, out var nested)
        && nested.ValueKind == JsonValueKind.Object
        && nested.TryGetProperty(nestedPropertyName, out var nestedValue)
        && nestedValue.ValueKind == JsonValueKind.String
            ? nestedValue.GetString()
            : null;
}

record ReviewStatus(
    string State,
    string? LatestState,
    int ReviewerCount,
    int ApprovalCount,
    int ChangesRequestedCount,
    int CommentedReviewCount,
    DateTimeOffset? LastReviewedAt)
{
    public static ReviewStatus Waiting { get; } = new(
        State: "waiting",
        LatestState: null,
        ReviewerCount: 0,
        ApprovalCount: 0,
        ChangesRequestedCount: 0,
        CommentedReviewCount: 0,
        LastReviewedAt: null);
}

record ReviewEvent(string Actor, string State, DateTimeOffset SubmittedAt)
{
    public static ReviewEvent FromJson(JsonElement element) =>
        new(
            Actor: GetNestedString(element, "user", "login") ?? "unknown",
            State: element.GetProperty("state").GetString() ?? "UNKNOWN",
            SubmittedAt: element.GetProperty("submitted_at").GetDateTimeOffset());

    private static string? GetNestedString(JsonElement element, string propertyName, string nestedPropertyName) =>
        element.TryGetProperty(propertyName, out var nested)
        && nested.ValueKind == JsonValueKind.Object
        && nested.TryGetProperty(nestedPropertyName, out var nestedValue)
        && nestedValue.ValueKind == JsonValueKind.String
            ? nestedValue.GetString()
            : null;
}

record PullRequestDetails(
    DateTimeOffset CreatedAt,
    string Author,
    DateTimeOffset? MergedAt,
    int CommitCount)
{
    public static PullRequestDetails FromJson(JsonElement element) =>
        new(
            element.GetProperty("created_at").GetDateTimeOffset(),
            GetNestedString(element, "user", "login") ?? "unknown",
            GetNullableDate(element, "merged_at"),
            element.GetProperty("commits").GetInt32());

    private static DateTimeOffset? GetNullableDate(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var value)
        && value.ValueKind == JsonValueKind.String
        && value.TryGetDateTimeOffset(out var date)
            ? date
            : null;

    private static string? GetNestedString(JsonElement element, string propertyName, string nestedPropertyName) =>
        element.TryGetProperty(propertyName, out var nested)
        && nested.ValueKind == JsonValueKind.Object
        && nested.TryGetProperty(nestedPropertyName, out var nestedValue)
        && nestedValue.ValueKind == JsonValueKind.String
            ? nestedValue.GetString()
            : null;
}

record TimelineResponse(string Repository, int Number, TimelineStats Stats, IReadOnlyList<TimelineItem> Items);

record TimelineStats(
    int CommitCount,
    int HumanCommenterCount,
    int HumanCommentCount,
    int ReviewCount,
    int ApprovalCount,
    double? FirstHumanCommentDelayMs,
    double? FirstReviewDelayMs,
    double? FirstApprovalDelayMs,
    double? ApprovalToMergeDelayMs,
    double? CreatedToMergeDelayMs,
    double? AverageHumanCommentGapMs,
    double? LongestHumanCommentGapMs,
    DateTimeOffset? MergedAt,
    IReadOnlyList<DeveloperStats> Developers)
{
    public static TimelineStats Create(PullRequestDetails pullRequest, IReadOnlyList<TimelineItem> timeline)
    {
        var humanComments = timeline
            .Where(item => item.Event == "commented"
                && IsHuman(item.Actor)
                && !SameActor(item.Actor, pullRequest.Author))
            .OrderBy(item => item.OccurredAt)
            .ToArray();

        var humanReviews = timeline
            .Where(item => item.Event == "reviewed" && IsHuman(item.Actor))
            .OrderBy(item => item.OccurredAt)
            .ToArray();

        var approvals = humanReviews
            .Where(item => item.State?.Equals("APPROVED", StringComparison.OrdinalIgnoreCase) is true)
            .ToArray();

        var mergedAt = pullRequest.MergedAt
            ?? timeline.FirstOrDefault(item => item.Event == "merged")?.OccurredAt;
        var lastApprovalBeforeMerge = mergedAt is null
            ? null
            : approvals.LastOrDefault(item => item.OccurredAt <= mergedAt.Value);

        return new TimelineStats(
            CommitCount: pullRequest.CommitCount,
            HumanCommenterCount: humanComments.Select(item => NormalizeActorIdentity(item.Actor)).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            HumanCommentCount: humanComments.Length,
            ReviewCount: humanReviews.Length,
            ApprovalCount: approvals.Length,
            FirstHumanCommentDelayMs: DelayMs(pullRequest.CreatedAt, humanComments.FirstOrDefault()?.OccurredAt),
            FirstReviewDelayMs: DelayMs(pullRequest.CreatedAt, humanReviews.FirstOrDefault()?.OccurredAt),
            FirstApprovalDelayMs: DelayMs(pullRequest.CreatedAt, approvals.FirstOrDefault()?.OccurredAt),
            ApprovalToMergeDelayMs: DelayMs(lastApprovalBeforeMerge?.OccurredAt, mergedAt),
            CreatedToMergeDelayMs: DelayMs(pullRequest.CreatedAt, mergedAt),
            AverageHumanCommentGapMs: AverageGapMs(humanComments),
            LongestHumanCommentGapMs: LongestGapMs(humanComments),
            MergedAt: mergedAt,
            Developers: CreateDeveloperStats(timeline));
    }

    private static IReadOnlyList<DeveloperStats> CreateDeveloperStats(IReadOnlyList<TimelineItem> timeline) =>
        timeline
            .Where(item => IsHuman(item.Actor))
            .GroupBy(item => NormalizeActorIdentity(item.Actor), StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var ordered = group.OrderBy(item => item.OccurredAt).ToArray();
                return new DeveloperStats(
                    Actor: PreferredActorName(ordered.Select(item => item.Actor)),
                    ActivityCount: ordered.Length,
                    CommitCount: ordered.Count(item => item.Event == "committed"),
                    CommentCount: ordered.Count(item => item.Event == "commented"),
                    ReviewCount: ordered.Count(item => item.Event == "reviewed"),
                    ApprovalCount: ordered.Count(item => item.Event == "reviewed" && item.State?.Equals("APPROVED", StringComparison.OrdinalIgnoreCase) is true),
                    ChangesRequestedCount: ordered.Count(item => item.Event == "reviewed" && item.State?.Equals("CHANGES_REQUESTED", StringComparison.OrdinalIgnoreCase) is true),
                    FirstActivityAt: ordered.First().OccurredAt,
                    LastActivityAt: ordered.Last().OccurredAt);
            })
            .OrderByDescending(developer => developer.ActivityCount)
            .ThenBy(developer => developer.Actor)
            .ToArray();

    private static bool IsHuman(string actor) =>
        !string.IsNullOrWhiteSpace(actor)
        && !actor.EndsWith("[bot]", StringComparison.OrdinalIgnoreCase)
        && !s_knownBotActors.Contains(actor);

    private static bool SameActor(string first, string second) =>
        NormalizeActorIdentity(first).Equals(NormalizeActorIdentity(second), StringComparison.OrdinalIgnoreCase);

    private static string NormalizeActorIdentity(string actor) =>
        string.Concat(actor.Where(char.IsLetterOrDigit)).ToLowerInvariant();

    private static string PreferredActorName(IEnumerable<string> actors) =>
        actors
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(actor => actor.Any(char.IsWhiteSpace))
            .ThenBy(actor => actor.Length)
            .First();

    private static double? DelayMs(DateTimeOffset? start, DateTimeOffset? end) =>
        start is null || end is null ? null : Math.Max(0, (end.Value - start.Value).TotalMilliseconds);

    private static double? AverageGapMs(IReadOnlyList<TimelineItem> items)
    {
        if (items.Count < 2)
        {
            return null;
        }

        return items.Zip(items.Skip(1), (first, second) => (second.OccurredAt - first.OccurredAt).TotalMilliseconds)
            .Average();
    }

    private static double? LongestGapMs(IReadOnlyList<TimelineItem> items)
    {
        if (items.Count < 2)
        {
            return null;
        }

        return items.Zip(items.Skip(1), (first, second) => (second.OccurredAt - first.OccurredAt).TotalMilliseconds)
            .Max();
    }

    private static readonly HashSet<string> s_knownBotActors = new(StringComparer.OrdinalIgnoreCase)
    {
        "Copilot",
        "dependabot",
        "dependabot-preview",
        "github-actions",
        "renovate"
    };
}

record DeveloperStats(
    string Actor,
    int ActivityCount,
    int CommitCount,
    int CommentCount,
    int ReviewCount,
    int ApprovalCount,
    int ChangesRequestedCount,
    DateTimeOffset FirstActivityAt,
    DateTimeOffset LastActivityAt);

record TimelineItem(
    string Id,
    string Event,
    string Actor,
    DateTimeOffset OccurredAt,
    string? State,
    string Summary,
    string? Body,
    string? HtmlUrl)
{
    public static TimelineItem FromJson(JsonElement element)
    {
        var eventName = GetString(element, "event") ?? "event";
        var occurredAt = GetDate(element, "created_at")
            ?? GetDate(element, "submitted_at")
            ?? GetDate(element, "committed_at")
            ?? GetNestedDate(element, "author", "date")
            ?? GetNestedDate(element, "committer", "date")
            ?? DateTimeOffset.MinValue;
        var actor = GetNestedString(element, "actor", "login")
            ?? GetNestedString(element, "user", "login")
            ?? GetNestedString(element, "author", "login")
            ?? GetNestedString(element, "author", "name")
            ?? GetNestedString(element, "committer", "login")
            ?? GetNestedString(element, "committer", "name")
            ?? "unknown";

        return new TimelineItem(
            Id: GetString(element, "id") ?? GetString(element, "sha") ?? $"{eventName}-{occurredAt.ToUnixTimeMilliseconds()}",
            Event: eventName,
            Actor: actor,
            OccurredAt: occurredAt,
            State: GetString(element, "state"),
            Summary: BuildSummary(element, eventName, actor),
            Body: GetString(element, "body"),
            HtmlUrl: GetString(element, "html_url"));
    }

    private static string BuildSummary(JsonElement element, string eventName, string actor)
    {
        var normalizedEvent = eventName.Replace('_', ' ');
        return eventName switch
        {
            "commented" => $"{actor} commented",
            "committed" => $"{actor} pushed commit {ShortSha(GetString(element, "sha") ?? GetString(element, "commit_id"))}",
            "reviewed" => $"{actor} reviewed with state {GetString(element, "state") ?? "unknown"}",
            "review_requested" => $"{actor} requested review from {GetNestedString(element, "requested_reviewer", "login") ?? GetNestedString(element, "requested_team", "name") ?? "someone"}",
            "ready_for_review" => $"{actor} marked the PR ready for review",
            "converted_to_draft" => $"{actor} converted the PR to draft",
            "labeled" => $"{actor} added label {GetNestedString(element, "label", "name") ?? "unknown"}",
            "unlabeled" => $"{actor} removed label {GetNestedString(element, "label", "name") ?? "unknown"}",
            "assigned" => $"{actor} assigned {GetNestedString(element, "assignee", "login") ?? "someone"}",
            "unassigned" => $"{actor} unassigned {GetNestedString(element, "assignee", "login") ?? "someone"}",
            "cross-referenced" => $"{actor} cross-referenced another issue or PR",
            "renamed" => $"{actor} renamed the title",
            "closed" => $"{actor} closed the PR",
            "reopened" => $"{actor} reopened the PR",
            "merged" => $"{actor} merged the PR",
            _ => $"{actor} {normalizedEvent}"
        };
    }

    private static string ShortSha(string? sha) => string.IsNullOrWhiteSpace(sha)
        ? "unknown"
        : sha[..Math.Min(7, sha.Length)];

    private static string? GetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            _ => null
        };
    }

    private static DateTimeOffset? GetDate(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var value)
        && value.ValueKind == JsonValueKind.String
        && value.TryGetDateTimeOffset(out var date)
            ? date
            : null;

    private static DateTimeOffset? GetNestedDate(JsonElement element, string propertyName, string nestedPropertyName) =>
        element.TryGetProperty(propertyName, out var nested)
        && nested.ValueKind == JsonValueKind.Object
        && nested.TryGetProperty(nestedPropertyName, out var nestedValue)
        && nestedValue.ValueKind == JsonValueKind.String
        && nestedValue.TryGetDateTimeOffset(out var date)
            ? date
            : null;

    private static string? GetNestedString(JsonElement element, string propertyName, string nestedPropertyName) =>
        element.TryGetProperty(propertyName, out var nested)
        && nested.ValueKind == JsonValueKind.Object
        && nested.TryGetProperty(nestedPropertyName, out var nestedValue)
        && nestedValue.ValueKind == JsonValueKind.String
            ? nestedValue.GetString()
            : null;
}
