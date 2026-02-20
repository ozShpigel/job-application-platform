using JobMatchService.Core.AI;
using JobMatchService.Core.Models;
using JobMatchService.Core.Profile;
using Microsoft.Extensions.Logging;

namespace JobMatchService.Core.Services;

public sealed class JobMatchService : IJobMatchService
{
    private readonly IProfileProvider _profileProvider;
    private readonly IClaudeClient _claudeClient;
    private readonly ILogger<JobMatchService> _logger;

    public JobMatchService(
        IProfileProvider profileProvider,
        IClaudeClient claudeClient,
        ILogger<JobMatchService> logger)
    {
        _profileProvider = profileProvider;
        _claudeClient = claudeClient;
        _logger = logger;
    }

    public async Task<MatchResponse> AnalyzeMatchAsync(string jobDescription, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting job match analysis");

        try
        {
            var profile = await _profileProvider.GetProfileAsync(cancellationToken);
            _logger.LogDebug("Profile loaded successfully");

            var parsedJob = await _claudeClient.ParseJobDescriptionAsync(jobDescription, cancellationToken);
            _logger.LogDebug("Job description parsed successfully. Job Title: {JobTitle}", parsedJob.JobTitle);

            var matchResponse = await _claudeClient.EvaluateMatchAsync(profile, parsedJob, cancellationToken);
            _logger.LogInformation("Match evaluation completed. Verdict: {Verdict}, Score: {Score}", 
                matchResponse.Verdict, matchResponse.OverallScore);

            // Enrich response with parsed job info for the UI
            return matchResponse with
            {
                JobTitle = parsedJob.JobTitle,
                Company = parsedJob.Company
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during job match analysis");
            throw;
        }
    }
}
