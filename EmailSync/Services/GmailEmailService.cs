using ApplicationTracker.EmailSync.Models;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ApplicationTracker.EmailSync.Services;

/// <summary>
/// Fetches emails from Gmail API. Uses OAuth2 for authentication.
/// Named GmailEmailService to avoid conflict with Google.Apis.Gmail.v1.GmailService.
/// </summary>
public sealed class GmailEmailService : IGmailEmailService
{
    private readonly GmailService _gmail;
    private readonly ILogger<GmailEmailService> _logger;
    private readonly string? _query;

    public GmailEmailService(IConfiguration config, IHostEnvironment env, ILogger<GmailEmailService> logger)
    {
        _logger = logger;

        // Resolve credentials path relative to the content root (where the exe lives)
        var relativePath = config["Gmail:CredentialsPath"] ?? "credentials.json";
        var credentialPath = Path.Combine(env.ContentRootPath, relativePath);

        UserCredential credential;
        using (var stream = new FileStream(credentialPath, FileMode.Open, FileAccess.Read))
        {
            credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.FromStream(stream).Secrets,
                new[] { GmailService.Scope.GmailReadonly },
                "user",
                CancellationToken.None,
                new FileDataStore("token.json", true)).Result;
        }

        _gmail = new GmailService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "ApplicationTracker.EmailSync"
        });

        _query = config["Gmail:Query"];

        _logger.LogInformation("Gmail service initialized");
    }

    public async Task<List<EmailMessage>> GetEmailsFromLast24HoursAsync(CancellationToken ct = default)
    {
        var yesterday = DateTime.UtcNow.AddHours(-24);
        var yesterdayFormatted = yesterday.ToString("yyyy/MM/dd");

        var effectiveQuery = string.IsNullOrWhiteSpace(_query)
            ? $"after:{yesterdayFormatted}"
            : _query;

        if (string.IsNullOrWhiteSpace(_query))
        {
            _logger.LogInformation("Fetching emails since {Date} (last 24 hours) with default query: {Query}", yesterday, effectiveQuery);
        }
        else
        {
            _logger.LogInformation("Fetching emails using configured Gmail query: {Query}", effectiveQuery);
        }

        var request = _gmail.Users.Messages.List("me");
        request.Q = effectiveQuery;

        var response = await request.ExecuteAsync(ct);

        if (response.Messages == null || !response.Messages.Any())
        {
            _logger.LogInformation("No emails found in last 24 hours");
            return new List<EmailMessage>();
        }

        var emails = new List<EmailMessage>();

        foreach (var message in response.Messages)
        {
            var fullMessage = await _gmail.Users.Messages.Get("me", message.Id).ExecuteAsync(ct);
            var email = ParseGmailMessage(fullMessage);

            // Double-check it's actually from last 24 hours
            if (email.ReceivedAt >= yesterday)
            {
                emails.Add(email);
            }
        }

        _logger.LogInformation("Found {Count} emails from last 24 hours", emails.Count);
        return emails;
    }

    private EmailMessage ParseGmailMessage(Message message)
    {
        var headers = message.Payload.Headers;
        var subject = headers.FirstOrDefault(h => h.Name == "Subject")?.Value ?? "";
        var from = headers.FirstOrDefault(h => h.Name == "From")?.Value ?? "";
        var dateStr = headers.FirstOrDefault(h => h.Name == "Date")?.Value ?? "";

        var body = GetEmailBody(message.Payload);

        return new EmailMessage
        {
            Subject = subject,
            From = from,
            Body = body,
            ReceivedAt = ParseEmailDate(dateStr)
        };
    }

    private static string GetEmailBody(MessagePart payload)
    {
        if (payload.Body?.Data != null)
        {
            var bytes = Convert.FromBase64String(
                payload.Body.Data.Replace('-', '+').Replace('_', '/'));
            return System.Text.Encoding.UTF8.GetString(bytes);
        }

        if (payload.Parts != null)
        {
            foreach (var part in payload.Parts)
            {
                if (part.MimeType is "text/plain" or "text/html")
                {
                    return GetEmailBody(part);
                }
            }
        }

        return string.Empty;
    }

    private static DateTime ParseEmailDate(string dateString)
    {
        if (DateTime.TryParse(dateString, out var date))
            return date;

        return DateTime.UtcNow;
    }
}
