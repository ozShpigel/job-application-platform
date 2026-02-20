using JobMatchService.Core.Models;

namespace JobMatchService.Core.Services;

public interface IJobMatchService
{
    Task<MatchResponse> AnalyzeMatchAsync(string jobDescription, CancellationToken cancellationToken = default);
}
