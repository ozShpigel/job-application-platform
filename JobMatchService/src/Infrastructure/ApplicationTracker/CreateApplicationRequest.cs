namespace JobMatchService.Infrastructure.ApplicationTracker;

public sealed record CreateApplicationRequest
{
    public required string JobTitle { get; init; }
    public required string Company { get; init; }
    public required string Status { get; init; }
    public int? MatchScore { get; init; }
    public string? MatchVerdict { get; init; }
    public string? JobDescription { get; init; }
    public string? MatchAnalysis { get; init; }
}
