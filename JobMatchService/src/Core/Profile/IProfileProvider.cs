namespace JobMatchService.Core.Profile;

public interface IProfileProvider
{
    Task<string> GetProfileAsync(CancellationToken cancellationToken = default);
}
