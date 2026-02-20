namespace JobMatchService.Infrastructure.ApplicationTracker;

public interface IApplicationTrackerClient
{
    Task<bool> CreateApplicationAsync(CreateApplicationRequest request, CancellationToken ct = default);
    Task<bool> IsApplicationExistsAsync(string company, string jobTitle, CancellationToken ct = default);
}
