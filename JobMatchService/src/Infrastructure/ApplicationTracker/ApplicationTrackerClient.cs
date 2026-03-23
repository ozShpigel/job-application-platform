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

    public async Task<bool> IsTrackerHealthyAsync(CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Application Tracker health check failed");
            return false;
        }
    }

    public async Task<bool> CreateApplicationAsync(CreateApplicationRequest request, CancellationToken ct = default)
    {
        return await RetryAsync(async () =>
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
        }, "CreateApplicationAsync");
    }

    public async Task<bool> IsApplicationExistsAsync(string company, string jobTitle, CancellationToken ct = default)
    {
        return await RetryAsync(async () =>
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
        }, "IsApplicationExistsAsync");
    }

    private async Task<T> RetryAsync<T>(Func<Task<T>> operation, string operationName, int maxRetries = 3, int delayMs = 1000)
    {
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (HttpRequestException ex) when (attempt < maxRetries)
            {
                _logger.LogWarning(ex, "{OperationName} attempt {Attempt}/{MaxRetries} failed. Retrying in {DelayMs}ms...",
                    operationName, attempt, maxRetries, delayMs);
                await Task.Delay(delayMs, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{OperationName} failed after {Attempt} attempts", operationName, attempt);
                return default!;
            }
        }

        return default!;
    }
}
