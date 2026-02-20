using ApplicationTracker.Core.Models;
using ApplicationTracker.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace ApplicationTracker.Infrastructure.Repositories;

public sealed class InterviewRepository : IInterviewRepository
{
    private readonly TrackerDbContext _db;

    public InterviewRepository(TrackerDbContext db) => _db = db;

    public async Task<Interview> CreateAsync(Interview interview, CancellationToken ct = default)
    {
        _db.Interviews.Add(interview);
        await _db.SaveChangesAsync(ct);
        return interview;
    }

    public async Task<Interview?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Interviews.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id, ct);
    }

    public async Task<List<Interview>> GetByApplicationIdAsync(Guid applicationId, CancellationToken ct = default)
    {
        return await _db.Interviews.AsNoTracking()
            .Where(i => i.ApplicationId == applicationId)
            .OrderBy(i => i.ScheduledAt)
            .ToListAsync(ct);
    }

    public async Task<List<Interview>> GetUpcomingAsync(int count = 5, CancellationToken ct = default)
    {
        return await _db.Interviews.AsNoTracking()
            .Where(i => !i.Completed && i.ScheduledAt >= DateTime.UtcNow)
            .OrderBy(i => i.ScheduledAt)
            .Take(count)
            .ToListAsync(ct);
    }

    public async Task<Interview> UpdateAsync(Interview interview, CancellationToken ct = default)
    {
        _db.Interviews.Update(interview);
        await _db.SaveChangesAsync(ct);
        return interview;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var interview = await _db.Interviews.FindAsync([id], ct);
        if (interview is not null)
        {
            _db.Interviews.Remove(interview);
            await _db.SaveChangesAsync(ct);
        }
    }
}
