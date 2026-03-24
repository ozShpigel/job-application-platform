using System.Net.Http.Json;
using ApplicationTracker.EmailSync.Models;
using Microsoft.Extensions.Logging;

namespace ApplicationTracker.EmailSync.Services;

/// <summary>
/// HTTP client for the ApplicationTracker API.
/// Aligned with actual API endpoints:
///   GET  /api/applications              -> all apps (filtered client-side for active)
///   PUT  /api/applications/{id}/status  -> { "newStatus": "...", "note": "..." }
///   POST /api/applications/{id}/interviews -> Interview object
/// </summary>
public sealed class TrackerApiClient : ITrackerApiClient
{
    private readonly HttpClient _http;
    private readonly ILogger<TrackerApiClient> _logger;

    // Statuses that are considered "active" (not terminal)
    private static readonly HashSet<string> TerminalStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Rejected", "Withdrawn", "Accepted"
    };

    public TrackerApiClient(HttpClient http, ILogger<TrackerApiClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<List<TrackerApplication>?> GetActiveApplicationsAsync(CancellationToken ct = default)
    {
        const int maxAttempts = 4;
        var delay = TimeSpan.FromSeconds(5);

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                _logger.LogInformation(
                    "Fetching applications from Tracker API (attempt {Attempt}/{Max})",
                    attempt, maxAttempts);

                var apps = await _http.GetFromJsonAsync<List<TrackerApplication>>("/api/applications", ct)
                    ?? new List<TrackerApplication>();

                var active = apps.Where(a => !TerminalStatuses.Contains(a.Status)).ToList();

                _logger.LogInformation("Retrieved {Total} applications, {Active} active", apps.Count, active.Count);
                return active;
            }
            catch (Exception ex) when (attempt < maxAttempts && IsTransientTrackerFailure(ex))
            {
                _logger.LogWarning(ex,
                    "Tracker API attempt {Attempt}/{Max} failed (cold start or network); retrying in {Delay}s...",
                    attempt, maxAttempts, delay.TotalSeconds);
                await Task.Delay(delay, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get applications from Tracker API");
                return null;
            }
        }

        return null;
    }

    private static bool IsTransientTrackerFailure(Exception ex)
    {
        if (ex is HttpRequestException or TaskCanceledException)
            return true;

        if (ex is TimeoutException)
            return true;

        return ex.InnerException != null && IsTransientTrackerFailure(ex.InnerException);
    }

    public async Task<bool> UpdateApplicationStatusAsync(Guid appId, string newStatus, string? note = null, CancellationToken ct = default)
    {
        try
        {
            // The API expects { "newStatus": "Applied", "note": "..." }
            var response = await _http.PutAsJsonAsync(
                $"/api/applications/{appId}/status",
                new { newStatus, note },
                ct);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Updated application {AppId} to status {Status}", appId, newStatus);
                return true;
            }

            _logger.LogWarning("Failed to update application {AppId}. Status: {HttpStatus}",
                appId, response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating application {AppId}", appId);
            return false;
        }
    }

    public async Task<bool> AddInterviewAsync(Guid appId, AddInterviewRequest interview, CancellationToken ct = default)
    {
        try
        {
            // The API expects a full Interview object at POST /api/applications/{id}/interviews
            var response = await _http.PostAsJsonAsync(
                $"/api/applications/{appId}/interviews",
                new
                {
                    applicationId = appId,
                    scheduledAt = interview.ScheduledAt,
                    type = interview.Type,
                    interviewer = interview.Interviewer,
                    topics = interview.Topics,
                    notes = interview.Notes
                },
                ct);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Added {Type} interview for application {AppId}", interview.Type, appId);
                return true;
            }

            _logger.LogWarning("Failed to add interview for {AppId}. Status: {HttpStatus}",
                appId, response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding interview for {AppId}", appId);
            return false;
        }
    }
}
