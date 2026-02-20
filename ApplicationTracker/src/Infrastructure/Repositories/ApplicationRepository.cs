using ApplicationTracker.Core.Models;
using ApplicationTracker.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace ApplicationTracker.Infrastructure.Repositories;

public sealed class ApplicationRepository : IApplicationRepository
{
    private readonly TrackerDbContext _db;

    public ApplicationRepository(TrackerDbContext db) => _db = db;

    public async Task<Application> CreateAsync(Application app, CancellationToken ct = default)
    {
        _db.Applications.Add(app);
        await _db.SaveChangesAsync(ct);
        return app;
    }

    public async Task<Application?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Applications.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct);
    }

    public async Task<List<Application>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Applications.AsNoTracking().OrderByDescending(a => a.CreatedAt).ToListAsync(ct);
    }

    public async Task<Application> UpdateAsync(Application app, CancellationToken ct = default)
    {
        _db.Applications.Update(app);
        await _db.SaveChangesAsync(ct);
        return app;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var app = await _db.Applications.FindAsync([id], ct);
        if (app is not null)
        {
            // Delete related records
            var interviews = await _db.Interviews.Where(i => i.ApplicationId == id).ToListAsync(ct);
            var notes = await _db.Notes.Where(n => n.ApplicationId == id).ToListAsync(ct);
            var updates = await _db.StatusUpdates.Where(s => s.ApplicationId == id).ToListAsync(ct);

            _db.Interviews.RemoveRange(interviews);
            _db.Notes.RemoveRange(notes);
            _db.StatusUpdates.RemoveRange(updates);
            _db.Applications.Remove(app);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<bool> ExistsAsync(string company, string jobTitle, CancellationToken ct = default)
    {
        return await _db.Applications.AsNoTracking()
            .AnyAsync(a =>
                a.Company.ToLower() == company.ToLower() &&
                a.JobTitle.ToLower() == jobTitle.ToLower(), ct);
    }
}
