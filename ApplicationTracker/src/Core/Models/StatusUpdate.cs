namespace ApplicationTracker.Core.Models;

public sealed record StatusUpdate
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required Guid ApplicationId { get; init; }
    public required ApplicationStatus FromStatus { get; init; }
    public required ApplicationStatus ToStatus { get; init; }
    public string? Note { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
