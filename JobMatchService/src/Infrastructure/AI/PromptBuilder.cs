using System.Text.Json;
using JobMatchService.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JobMatchService.Infrastructure.AI;

public sealed class PromptBuilder
{
    private readonly string _jobMatchTemplate;
    private readonly string _systemContext;
    private readonly string _analystSkill;
    private readonly string _evaluatorSkill;
    private readonly ILogger<PromptBuilder> _logger;

    public PromptBuilder(IConfiguration configuration, ILogger<PromptBuilder> logger)
    {
        _logger = logger;

        var basePath = configuration["ContentRoot"] ?? Directory.GetCurrentDirectory();
        _logger.LogInformation("Loading prompt templates from base path: {BasePath}", basePath);

        try
        {
            _jobMatchTemplate = LoadFile(Path.Combine(basePath, "Templates", "job-match-prompt-template.md"));
            _systemContext = LoadFile(Path.Combine(basePath, "Config", "system-context.md"));
            _analystSkill = LoadFile(Path.Combine(basePath, "Skills", "analyst.md"));
            _evaluatorSkill = LoadFile(Path.Combine(basePath, "Skills", "evaluator.md"));

            _logger.LogInformation("PromptBuilder initialized successfully. All templates loaded.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading prompt templates");
            throw;
        }
    }

    public string BuildAnalysisPrompt(string jobDescription)
    {
        return $"{_analystSkill}\n\n---\n\n## JOB DESCRIPTION TO PARSE\n\n{jobDescription}\n\n---\n\nParse this job description and return valid JSON matching the schema defined in your skill.";
    }

    public string BuildEvaluationPrompt(string profile, ParsedJob parsedJob)
    {
        var parsedJobJson = JsonSerializer.Serialize(parsedJob, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        var prompt = _jobMatchTemplate
            .Replace("{{SYSTEM_CONTEXT}}", _systemContext)
            .Replace("{{EVALUATOR_SKILL}}", _evaluatorSkill)
            .Replace("{{USER_PROFILE}}", profile)
            .Replace("{{PARSED_JOB}}", parsedJobJson);

        return prompt;
    }

    private static string LoadFile(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Required template file not found: {path}");
        }

        return File.ReadAllText(path);
    }
}
