using ApplicationTracker.Core.Models;
using ApplicationTracker.Infrastructure.Database;
using ApplicationTracker.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<TrackerDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repositories
builder.Services.AddScoped<IApplicationRepository, ApplicationRepository>();
builder.Services.AddScoped<IInterviewRepository, InterviewRepository>();
builder.Services.AddScoped<INoteRepository, NoteRepository>();

// JSON: accept enum values as strings
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

// OpenAPI
builder.Services.AddOpenApi();

// CORS for development
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TrackerDbContext>();
    db.Database.EnsureCreated();
}

// Middleware
app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// ============================================================
// APPLICATION ENDPOINTS
// ============================================================

app.MapPost("/api/applications", async (
    [FromBody] Application application,
    IApplicationRepository repo,
    TrackerDbContext db,
    ILogger<Program> logger,
    CancellationToken ct) =>
{
    try
    {
        var created = await repo.CreateAsync(application, ct);

        // Record initial status
        db.StatusUpdates.Add(new StatusUpdate
        {
            ApplicationId = created.Id,
            FromStatus = ApplicationStatus.Analyzing,
            ToStatus = created.Status,
            Note = "משרה נוספה למעקב"
        });
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Application created: {Id} - {Title} at {Company}", created.Id, created.JobTitle, created.Company);
        return Results.Created($"/api/applications/{created.Id}", created);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error creating application");
        return Results.Problem("Error creating application");
    }
})
.WithName("CreateApplication")
.WithSummary("Create a new application");

app.MapGet("/api/applications", async (
    IApplicationRepository repo,
    CancellationToken ct) =>
{
    var apps = await repo.GetAllAsync(ct);
    return Results.Ok(apps);
})
.WithName("GetAllApplications")
.WithSummary("Get all applications");

app.MapGet("/api/applications/{id:guid}", async (
    Guid id,
    IApplicationRepository appRepo,
    IInterviewRepository interviewRepo,
    INoteRepository noteRepo,
    TrackerDbContext db,
    CancellationToken ct) =>
{
    var application = await appRepo.GetByIdAsync(id, ct);
    if (application is null) return Results.NotFound();

    var interviews = await interviewRepo.GetByApplicationIdAsync(id, ct);
    var notes = await noteRepo.GetByApplicationIdAsync(id, ct);
    var statusUpdates = await db.StatusUpdates.AsNoTracking()
        .Where(s => s.ApplicationId == id)
        .OrderBy(s => s.Timestamp)
        .ToListAsync(ct);

    return Results.Ok(new
    {
        application,
        interviews,
        notes,
        statusUpdates
    });
})
.WithName("GetApplication")
.WithSummary("Get application with details");

app.MapPut("/api/applications/{id:guid}/status", async (
    Guid id,
    [FromBody] StatusUpdateRequest request,
    IApplicationRepository repo,
    TrackerDbContext db,
    ILogger<Program> logger,
    CancellationToken ct) =>
{
    try
    {
        var app = await repo.GetByIdAsync(id, ct);
        if (app is null) return Results.NotFound();

        var oldStatus = app.Status;
        var updated = app with
        {
            Status = request.NewStatus,
            UpdatedAt = DateTime.UtcNow,
            AppliedAt = request.NewStatus == ApplicationStatus.Applied ? DateTime.UtcNow : app.AppliedAt
        };

        // EF Core needs us to detach and re-attach for records
        db.Entry(app).State = EntityState.Detached;
        await repo.UpdateAsync(updated, ct);

        // Record status change
        db.StatusUpdates.Add(new StatusUpdate
        {
            ApplicationId = id,
            FromStatus = oldStatus,
            ToStatus = request.NewStatus,
            Note = request.Note
        });
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Application {Id} status changed: {From} -> {To}", id, oldStatus, request.NewStatus);
        return Results.Ok(updated);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error updating application status");
        return Results.Problem("Error updating application status");
    }
})
.WithName("UpdateApplicationStatus")
.WithSummary("Update application status");

app.MapDelete("/api/applications/{id:guid}", async (
    Guid id,
    IApplicationRepository repo,
    CancellationToken ct) =>
{
    await repo.DeleteAsync(id, ct);
    return Results.NoContent();
})
.WithName("DeleteApplication")
.WithSummary("Delete application");

// ============================================================
// INTERVIEW ENDPOINTS
// ============================================================

app.MapPost("/api/applications/{id:guid}/interviews", async (
    Guid id,
    [FromBody] Interview interview,
    IApplicationRepository appRepo,
    IInterviewRepository repo,
    CancellationToken ct) =>
{
    var app = await appRepo.GetByIdAsync(id, ct);
    if (app is null) return Results.NotFound();

    var created = interview with { ApplicationId = id };
    await repo.CreateAsync(created, ct);
    return Results.Created($"/api/interviews/{created.Id}", created);
})
.WithName("CreateInterview")
.WithSummary("Add interview to application");

app.MapPut("/api/interviews/{id:guid}", async (
    Guid id,
    [FromBody] Interview interview,
    IInterviewRepository repo,
    TrackerDbContext db,
    CancellationToken ct) =>
{
    var existing = await repo.GetByIdAsync(id, ct);
    if (existing is null) return Results.NotFound();

    var updated = interview with { Id = id, ApplicationId = existing.ApplicationId };
    db.Entry(existing).State = EntityState.Detached;
    await repo.UpdateAsync(updated, ct);
    return Results.Ok(updated);
})
.WithName("UpdateInterview")
.WithSummary("Update interview");

app.MapDelete("/api/interviews/{id:guid}", async (
    Guid id,
    IInterviewRepository repo,
    CancellationToken ct) =>
{
    await repo.DeleteAsync(id, ct);
    return Results.NoContent();
})
.WithName("DeleteInterview")
.WithSummary("Delete interview");

// ============================================================
// NOTE ENDPOINTS
// ============================================================

app.MapPost("/api/applications/{id:guid}/notes", async (
    Guid id,
    [FromBody] Note note,
    IApplicationRepository appRepo,
    INoteRepository repo,
    CancellationToken ct) =>
{
    var app = await appRepo.GetByIdAsync(id, ct);
    if (app is null) return Results.NotFound();

    var created = note with { ApplicationId = id };
    await repo.CreateAsync(created, ct);
    return Results.Created($"/api/notes/{created.Id}", created);
})
.WithName("CreateNote")
.WithSummary("Add note to application");

app.MapPut("/api/notes/{id:guid}", async (
    Guid id,
    [FromBody] Note note,
    INoteRepository repo,
    TrackerDbContext db,
    CancellationToken ct) =>
{
    var existing = await repo.GetByIdAsync(id, ct);
    if (existing is null) return Results.NotFound();

    var updated = note with { Id = id, ApplicationId = existing.ApplicationId };
    db.Entry(existing).State = EntityState.Detached;
    await repo.UpdateAsync(updated, ct);
    return Results.Ok(updated);
})
.WithName("UpdateNote")
.WithSummary("Update note");

app.MapDelete("/api/notes/{id:guid}", async (
    Guid id,
    INoteRepository repo,
    CancellationToken ct) =>
{
    await repo.DeleteAsync(id, ct);
    return Results.NoContent();
})
.WithName("DeleteNote")
.WithSummary("Delete note");

// ============================================================
// STATS ENDPOINT
// ============================================================

app.MapGet("/api/stats", async (
    TrackerDbContext db,
    CancellationToken ct) =>
{
    var apps = await db.Applications.AsNoTracking().ToListAsync(ct);
    var total = apps.Count;
    var withScore = apps.Where(a => a.MatchScore.HasValue).ToList();
    var avgScore = withScore.Count > 0 ? (int)withScore.Average(a => a.MatchScore!.Value) : 0;

    var applied = apps.Count(a => a.Status >= ApplicationStatus.Applied);
    var heardBack = apps.Count(a =>
        a.Status is ApplicationStatus.PhoneScreen or ApplicationStatus.TechnicalInterview
        or ApplicationStatus.FinalRound or ApplicationStatus.OfferReceived
        or ApplicationStatus.Accepted or ApplicationStatus.Rejected);
    var responseRate = applied > 0 ? (int)(heardBack * 100.0 / applied) : 0;

    var inProgress = apps.Count(a =>
        a.Status is not ApplicationStatus.Rejected
        and not ApplicationStatus.Withdrawn
        and not ApplicationStatus.Accepted);

    var statusBreakdown = apps
        .GroupBy(a => a.Status)
        .ToDictionary(g => g.Key.ToString(), g => g.Count());

    return Results.Ok(new
    {
        total,
        inProgress,
        avgScore,
        responseRate,
        applied,
        heardBack,
        statusBreakdown
    });
})
.WithName("GetStats")
.WithSummary("Get application statistics");

// ============================================================
// TIMELINE ENDPOINT
// ============================================================

app.MapGet("/api/applications/{id:guid}/timeline", async (
    Guid id,
    TrackerDbContext db,
    CancellationToken ct) =>
{
    var statusUpdates = await db.StatusUpdates.AsNoTracking()
        .Where(s => s.ApplicationId == id)
        .OrderBy(s => s.Timestamp)
        .ToListAsync(ct);

    var interviews = await db.Interviews.AsNoTracking()
        .Where(i => i.ApplicationId == id)
        .OrderBy(i => i.ScheduledAt)
        .ToListAsync(ct);

    var notes = await db.Notes.AsNoTracking()
        .Where(n => n.ApplicationId == id)
        .OrderBy(n => n.CreatedAt)
        .ToListAsync(ct);

    var timeline = new List<object>();

    foreach (var s in statusUpdates)
        timeline.Add(new { type = "status", date = s.Timestamp, s.FromStatus, s.ToStatus, s.Note });

    foreach (var i in interviews)
        timeline.Add(new { type = "interview", date = i.ScheduledAt, i.Type, i.Interviewer, i.Completed, i.Notes });

    foreach (var n in notes)
        timeline.Add(new { type = "note", date = n.CreatedAt, n.Content, n.Category });

    var sorted = timeline.OrderBy(t => ((dynamic)t).date).ToList();
    return Results.Ok(sorted);
})
.WithName("GetTimeline")
.WithSummary("Get application timeline");

// ============================================================
// UPCOMING INTERVIEWS
// ============================================================

app.MapGet("/api/interviews/upcoming", async (
    IInterviewRepository repo,
    TrackerDbContext db,
    CancellationToken ct) =>
{
    var interviews = await repo.GetUpcomingAsync(10, ct);

    // Enrich with application info
    var result = new List<object>();
    foreach (var interview in interviews)
    {
        var app = await db.Applications.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == interview.ApplicationId, ct);
        result.Add(new
        {
            interview,
            jobTitle = app?.JobTitle,
            company = app?.Company
        });
    }

    return Results.Ok(result);
})
.WithName("GetUpcomingInterviews")
.WithSummary("Get upcoming interviews");

// ============================================================
// EXISTS CHECK (for JobMatchService integration)
// ============================================================

app.MapGet("/api/applications/exists", async (
    [FromQuery] string company,
    [FromQuery] string jobTitle,
    IApplicationRepository repo,
    CancellationToken ct) =>
{
    var exists = await repo.ExistsAsync(company, jobTitle, ct);
    return Results.Ok(exists);
})
.WithName("ApplicationExists")
.WithSummary("Check if application exists by company and job title");

app.Run();

// ============================================================
// REQUEST DTOs
// ============================================================

public record StatusUpdateRequest
{
    public required ApplicationStatus NewStatus { get; init; }
    public string? Note { get; init; }
}
