namespace ApplicationTracker.Core.Models;

public sealed record Application
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string JobTitle { get; init; }
    public required string Company { get; init; }
    public required ApplicationStatus Status { get; init; }
    public int? MatchScore { get; init; }
    public string? MatchVerdict { get; init; }
    public string? JobDescription { get; init; }
    public string? MatchAnalysis { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? AppliedAt { get; init; }
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}

public enum ApplicationStatus
{
    Analyzing,
    DecidedToApply,
    Applied,
    PhoneScreen,
    TechnicalInterview,
    FinalRound,
    OfferReceived,
    Accepted,
    Rejected,
    Withdrawn
}
