using ApplicationTracker.EmailSync.Models;

namespace ApplicationTracker.EmailSync.Services;

public interface IEmailParser
{
    Task<EmailUpdate?> ParseEmailAsync(EmailMessage email, List<string> knownCompanies, CancellationToken ct = default);
}
