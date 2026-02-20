using ApplicationTracker.EmailSync.Models;

namespace ApplicationTracker.EmailSync.Services;

public interface IGmailEmailService
{
    Task<List<EmailMessage>> GetEmailsFromLast24HoursAsync(CancellationToken ct = default);
}
