using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace JobMatchService.Infrastructure.ApplicationTracker;

public sealed class ApplicationTrackerClient : IApplicationTrackerClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApplicationTrackerClient> _logger;

    public ApplicationTrackerClient(HttpClient httpClient, ILogger<ApplicationTrackerClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> CreateApplicationAsync(CreateApplicationRequest request, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/applications", request, ct);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Application saved to tracker: {Company} - {JobTitle}",
                    request.Company, request.JobTitle);
                return true;
            }

            _logger.LogWarning("Failed to save application to tracker. Status: {Status}",
                response.StatusCode);
            return false;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "ApplicationTracker service is not available");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving application to tracker");
            return false;
        }
    }

    public async Task<bool> IsApplicationExistsAsync(string company, string jobTitle, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"/api/applications/exists?company={Uri.EscapeDataString(company)}&jobTitle={Uri.EscapeDataString(jobTitle)}",
                ct);

            if (response.IsSuccessStatusCode)
            {
                var exists = await response.Content.ReadFromJsonAsync<bool>(cancellationToken: ct);
                return exists;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not check if application exists");
            return false;
        }
    }
}
