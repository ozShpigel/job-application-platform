using ApplicationTracker.EmailSync.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

try
{
    // Set content root to the directory where the executable lives,
    // so appsettings.json and credentials.json are found regardless of cwd
    var exeDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)!;

    var credentialsSecretPath = "/etc/secrets/credentials.json";
    var credentialsLocalPath = Path.Combine(exeDir, "credentials.json");

    Console.WriteLine($"EmailSync starting. exeDir: {exeDir}");
    Console.WriteLine($"Checking credentials secret path: {credentialsSecretPath}");
    Console.WriteLine($"Secret credentials.json exists: {File.Exists(credentialsSecretPath)}");
    Console.WriteLine($"Checking local credentials path: {credentialsLocalPath}");
    Console.WriteLine($"Local credentials.json exists: {File.Exists(credentialsLocalPath)}");

    var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
    {
        Args = args,
        ContentRootPath = exeDir
    });

    // Logging
    builder.Logging.ClearProviders();
    builder.Logging.AddConsole();

    // Services
    builder.Services.AddSingleton<IGmailEmailService, GmailEmailService>();
    builder.Services.AddSingleton<IEmailParser, ClaudeEmailParser>();
    builder.Services.AddSingleton<EmailSyncOrchestrator>();

    builder.Services.AddHttpClient<ITrackerApiClient, TrackerApiClient>(client =>
    {
        var trackerUrl = builder.Configuration["Tracker:BaseUrl"] ?? "http://localhost:5002";
        client.BaseAddress = new Uri(trackerUrl);
        client.Timeout = TimeSpan.FromSeconds(30);
    });

    var host = builder.Build();

    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    var orchestrator = host.Services.GetRequiredService<EmailSyncOrchestrator>();

    logger.LogInformation("ApplicationTracker Email Sync Service started");
    logger.LogInformation("Current time: {Time}", DateTime.Now);

    // Run sync immediately on start
    var result = await orchestrator.RunSyncAsync();

    logger.LogInformation("Sync Result: {Result}",
        System.Text.Json.JsonSerializer.Serialize(result));

    logger.LogInformation("Email sync completed. Service will exit.");
    logger.LogInformation("Schedule this to run daily using Windows Task Scheduler, cron, or systemd timer");

    // Exit after one run (will be scheduled externally)
    return result.Success ? 0 : 1;
}
catch (Exception ex)
{
    Console.Error.WriteLine("Fatal error in EmailSync worker:");
    Console.Error.WriteLine(ex.ToString());
    return 1;
}
