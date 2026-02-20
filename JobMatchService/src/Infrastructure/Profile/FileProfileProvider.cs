using JobMatchService.Core.Profile;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JobMatchService.Infrastructure.Profile;

public sealed class FileProfileProvider : IProfileProvider
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<FileProfileProvider> _logger;

    public FileProfileProvider(IConfiguration configuration, ILogger<FileProfileProvider> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> GetProfileAsync(CancellationToken cancellationToken = default)
    {
        var basePath = _configuration["ContentRoot"] ?? Directory.GetCurrentDirectory();
        var relativePath = _configuration["Profile:FilePath"] ?? "Data/professional-profile.md";
        var filePath = Path.Combine(basePath, relativePath);
        
        _logger.LogInformation("Loading profile from: {FilePath}", filePath);

        if (!File.Exists(filePath))
        {
            _logger.LogError("Profile file not found: {FilePath}", filePath);
            throw new FileNotFoundException($"Profile file not found: {filePath}");
        }

        try
        {
            var content = await File.ReadAllTextAsync(filePath, cancellationToken);
            _logger.LogDebug("Profile loaded successfully. Length: {Length} characters", content.Length);
            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading profile file: {FilePath}", filePath);
            throw;
        }
    }
}
