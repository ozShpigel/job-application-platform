using ApplicationTracker.Core.Models;
using ApplicationTracker.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace ApplicationTracker.Infrastructure.Repositories;

public sealed class NoteRepository : INoteRepository
{
    private readonly TrackerDbContext _db;

    public NoteRepository(TrackerDbContext db) => _db = db;

    public async Task<Note> CreateAsync(Note note, CancellationToken ct = default)
    {
        _db.Notes.Add(note);
        await _db.SaveChangesAsync(ct);
        return note;
    }

    public async Task<Note?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Notes.AsNoTracking().FirstOrDefaultAsync(n => n.Id == id, ct);
    }

    public async Task<List<Note>> GetByApplicationIdAsync(Guid applicationId, CancellationToken ct = default)
    {
        return await _db.Notes.AsNoTracking()
            .Where(n => n.ApplicationId == applicationId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<Note> UpdateAsync(Note note, CancellationToken ct = default)
    {
        _db.Notes.Update(note);
        await _db.SaveChangesAsync(ct);
        return note;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var note = await _db.Notes.FindAsync([id], ct);
        if (note is not null)
        {
            _db.Notes.Remove(note);
            await _db.SaveChangesAsync(ct);
        }
    }
}
