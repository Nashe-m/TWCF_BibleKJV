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
            return new UserSetting(e.Id, e.LastBookId, e.LastChapterNumber, e.FontScale, e.Theme, e.LastTranslationId, e.ShowWordsOfJesus)
            {
                LineSpacing = e.LineSpacing,
                ShowVerseNumbers = e.ShowVerseNumbers,
                ProjectorTheme = e.ProjectorTheme,
                ProjectorMargins = e.ProjectorMargins,
                ShowAnnotationsWhenProjecting = e.ShowAnnotationsWhenProjecting,
                MarkJumpWhenProjecting = e.MarkJumpWhenProjecting,
            };
        }

        // There is a single settings row.
        public async Task SaveUserSettingAsync(UserSetting setting)
        {
            var entity = await _db.UserSettings.FirstOrDefaultAsync();
            if (entity == null)
            {
                entity = new UserSettingEntity();
                _db.UserSettings.Add(entity);
            }

            entity.LastBookId = setting.LastBookId;
            entity.LastChapterNumber = setting.LastChapterNumber;
            entity.FontScale = setting.FontScale;
            entity.Theme = setting.Theme;
            entity.LastTranslationId = setting.LastTranslationId;
            entity.ShowWordsOfJesus = setting.ShowWordsOfJesus;
            entity.LineSpacing = setting.LineSpacing;
            entity.ShowVerseNumbers = setting.ShowVerseNumbers;
            entity.ProjectorTheme = setting.ProjectorTheme;
            entity.ProjectorMargins = setting.ProjectorMargins;
            entity.ShowAnnotationsWhenProjecting = setting.ShowAnnotationsWhenProjecting;
            entity.MarkJumpWhenProjecting = setting.MarkJumpWhenProjecting;

            await _db.SaveChangesAsync();
        }

        public async Task<IEnumerable<Note>> GetNotesAsync(int bookId, int chapterNumber)
        {
            return await _db.Notes
                .AsNoTracking()
                .Where(n => n.BookId == bookId && n.ChapterNumber == chapterNumber)
                .OrderBy(n => n.CreatedUtc)
                .Select(n => ToNote(n))
                .ToListAsync();
        }

        public async Task<IEnumerable<Note>> GetAllNotesAsync()
        {
            return await _db.Notes.AsNoTracking().Select(n => ToNote(n)).ToListAsync();
        }

        public async Task<Note> AddNoteAsync(Note note)
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
            _db.Entry(entity).State = EntityState.Detached;
            return ToNote(entity);
        }

        public async Task UpdateNoteAsync(Note note)
        {
            var existing = await _db.Notes.FirstOrDefaultAsync(n => n.Id == note.Id);
            if (existing == null) return;
            existing.Text = note.Text;
            existing.ModifiedUtc = note.ModifiedUtc ?? System.DateTime.UtcNow;
            _db.Notes.Update(existing);
            await _db.SaveChangesAsync();
            _db.Entry(existing).State = EntityState.Detached;
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
                .Select(h => ToHighlight(h))
                .ToListAsync();
        }

        public async Task<IEnumerable<Highlight>> GetAllHighlightsAsync()
        {
            return await _db.Highlights.AsNoTracking().Select(h => ToHighlight(h)).ToListAsync();
        }

        public async Task<Highlight> AddHighlightAsync(Highlight highlight)
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
            _db.Entry(entity).State = EntityState.Detached;
            return ToHighlight(entity);
        }

        public async Task DeleteHighlightAsync(int highlightId)
        {
            var existing = await _db.Highlights.FirstOrDefaultAsync(h => h.Id == highlightId);
            if (existing == null) return;
            _db.Highlights.Remove(existing);
            await _db.SaveChangesAsync();
        }

        private static Note ToNote(NoteEntity n) => new()
        {
            Id = n.Id, BookId = n.BookId, ChapterNumber = n.ChapterNumber,
            StartVerseNumber = n.StartVerseNumber, EndVerseNumber = n.EndVerseNumber,
            Text = n.Text, CreatedUtc = n.CreatedUtc, ModifiedUtc = n.ModifiedUtc,
        };

        private static Highlight ToHighlight(HighlightEntity h) => new()
        {
            Id = h.Id, BookId = h.BookId, ChapterNumber = h.ChapterNumber,
            StartVerseNumber = h.StartVerseNumber, EndVerseNumber = h.EndVerseNumber,
            ColorHex = h.ColorHex, CreatedUtc = h.CreatedUtc,
        };
    }
}
