using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TWCF_BibleApp.Application.Interfaces;
using TWCF_BibleApp.Domain;
using TWCF_BibleApp.Persistence.Contexts;
using TWCF_BibleApp.Persistence.Entities;

namespace TWCF_BibleApp.Persistence.Repositories
{
    public sealed class UserRepository : IUserRepository
    {
        private readonly UserDataContext _db;

        public UserRepository(UserDataContext db)
        {
            _db = db;
        }

        public async Task<UserSetting?> GetUserSettingAsync()
        {
            var e = await _db.UserSettings.AsNoTracking().FirstOrDefaultAsync();
            if (e == null) return null;
            return new UserSetting(e.Id, e.LastBookId, e.LastChapterNumber, e.FontScale, e.Theme);
        }

        public async Task SaveUserSettingAsync(UserSetting setting)
        {
            var existing = await _db.UserSettings.FirstOrDefaultAsync();
            if (existing == null)
            {
                var entity = new UserSettingEntity
                {
                    LastBookId = setting.LastBookId,
                    LastChapterNumber = setting.LastChapterNumber,
                    FontScale = setting.FontScale,
                    Theme = setting.Theme
                };
                _db.UserSettings.Add(entity);
            }
            else
            {
                existing.LastBookId = setting.LastBookId;
                existing.LastChapterNumber = setting.LastChapterNumber;
                existing.FontScale = setting.FontScale;
                existing.Theme = setting.Theme;
                _db.UserSettings.Update(existing);
            }

            await _db.SaveChangesAsync();
        }

        public async Task<IEnumerable<Note>> GetNotesAsync(int bookId, int chapterNumber)
        {
            return await _db.Notes
                .AsNoTracking()
                .Where(n => n.BookId == bookId && n.ChapterNumber == chapterNumber)
                .OrderBy(n => n.CreatedUtc)
                .Select(n => new Note(n.Id, n.BookId, n.ChapterNumber, n.StartVerseNumber, n.EndVerseNumber, n.Text))
                .ToListAsync();
        }

        public async Task AddNoteAsync(Note note)
        {
            var entity = new NoteEntity
            {
                BookId = note.BookId,
                ChapterNumber = note.ChapterNumber,
                StartVerseNumber = note.StartVerseNumber,
                EndVerseNumber = note.EndVerseNumber,
                Text = note.Text,
                CreatedUtc = note.CreatedUtc
            };
            _db.Notes.Add(entity);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateNoteAsync(Note note)
        {
            var existing = await _db.Notes.FirstOrDefaultAsync(n => n.Id == note.Id);
            if (existing == null) return;
            existing.Text = note.Text;
            existing.ModifiedUtc = note.ModifiedUtc ?? System.DateTime.UtcNow;
            _db.Notes.Update(existing);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteNoteAsync(int noteId)
        {
            var existing = await _db.Notes.FirstOrDefaultAsync(n => n.Id == noteId);
            if (existing == null) return;
            _db.Notes.Remove(existing);
            await _db.SaveChangesAsync();
        }

        public async Task<IEnumerable<Highlight>> GetHighlightsAsync(int bookId, int chapterNumber)
        {
            return await _db.Highlights
                .AsNoTracking()
                .Where(h => h.BookId == bookId && h.ChapterNumber == chapterNumber)
                .OrderBy(h => h.CreatedUtc)
                .Select(h => new Highlight(h.Id, h.BookId, h.ChapterNumber, h.StartVerseNumber, h.EndVerseNumber, h.ColorHex))
                .ToListAsync();
        }

        public async Task AddHighlightAsync(Highlight highlight)
        {
            var entity = new HighlightEntity
            {
                BookId = highlight.BookId,
                ChapterNumber = highlight.ChapterNumber,
                StartVerseNumber = highlight.StartVerseNumber,
                EndVerseNumber = highlight.EndVerseNumber,
                ColorHex = highlight.ColorHex,
                CreatedUtc = highlight.CreatedUtc
            };
            _db.Highlights.Add(entity);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteHighlightAsync(int highlightId)
        {
            var existing = await _db.Highlights.FirstOrDefaultAsync(h => h.Id == highlightId);
            if (existing == null) return;
            _db.Highlights.Remove(existing);
            await _db.SaveChangesAsync();
        }
    }
}
