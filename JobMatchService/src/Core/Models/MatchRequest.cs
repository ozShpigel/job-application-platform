namespace JobMatchService.Core.Models;

public sealed record MatchRequest
{
    public required string JobDescription { get; init; }
}
