using ApplicationTracker.EmailSync.Models;

namespace ApplicationTracker.EmailSync.Services;

public interface ITrackerApiClient
{
    Task<List<TrackerApplication>> GetActiveApplicationsAsync(CancellationToken ct = default);
    Task<bool> UpdateApplicationStatusAsync(Guid appId, string newStatus, string? note = null, CancellationToken ct = default);
    Task<bool> AddInterviewAsync(Guid appId, AddInterviewRequest interview, CancellationToken ct = default);
}

public sealed record AddInterviewRequest
{
    public required DateTime ScheduledAt { get; init; }
    public required string Type { get; init; }
    public string? Interviewer { get; init; }
    public string? Topics { get; init; }
    public string? Notes { get; init; }
}
