using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TWCF_BibleApp.Application.Interfaces;
using TWCF_BibleApp.Domain;

namespace TWCF_BibleApp.Application.Services
{
    public sealed class UserService
    {
        private readonly IUserRepository _repo;

        public UserService(IUserRepository repo)
        {
            _repo = repo;
        }

        public Task<UserSetting?> GetUserSettingAsync() => _repo.GetUserSettingAsync();
        public Task SaveUserSettingAsync(UserSetting setting) => _repo.SaveUserSettingAsync(setting);

        public Task<IEnumerable<Note>> GetNotesAsync(int bookId, int chapterNumber) => _repo.GetNotesAsync(bookId, chapterNumber);
        public Task<Note> AddNoteAsync(Note note) => _repo.AddNoteAsync(note);
        public Task UpdateNoteAsync(Note note) => _repo.UpdateNoteAsync(note);
        public Task DeleteNoteAsync(int noteId) => _repo.DeleteNoteAsync(noteId);

        public Task<IEnumerable<Highlight>> GetHighlightsAsync(int bookId, int chapterNumber) => _repo.GetHighlightsAsync(bookId, chapterNumber);
        public Task<Highlight> AddHighlightAsync(Highlight highlight) => _repo.AddHighlightAsync(highlight);
        public Task DeleteHighlightAsync(int highlightId) => _repo.DeleteHighlightAsync(highlightId);

        /// <summary>
        /// Highlights the given verses of a chapter in one colour, or removes their highlight when
        /// colorHex is null. Existing highlights that only partly overlap keep their other verses.
        /// Returns the chapter's highlights afterwards.
        /// </summary>
        public async Task<IReadOnlyList<Highlight>> SetHighlightAsync(int bookId, int chapterNumber, IReadOnlyCollection<int> verses, string? colorHex)
        {
            var selected = verses.ToHashSet();
            foreach (var h in await _repo.GetHighlightsAsync(bookId, chapterNumber))
            {
                var covered = Enumerable.Range(h.StartVerseNumber, h.EndVerseNumber - h.StartVerseNumber + 1).ToList();
                if (!covered.Any(selected.Contains)) continue;

                await _repo.DeleteHighlightAsync(h.Id);
                foreach (var rest in VerseRange.FromVerses(covered.Where(v => !selected.Contains(v))))
                    await _repo.AddHighlightAsync(Copy(h, rest, h.ColorHex));
            }

            if (colorHex != null)
            {
                foreach (var run in VerseRange.FromVerses(selected))
                    await _repo.AddHighlightAsync(new Highlight(0, bookId, chapterNumber, run.Start, run.End, colorHex));
            }

            return (await _repo.GetHighlightsAsync(bookId, chapterNumber)).ToList();
        }

        private static Highlight Copy(Highlight h, VerseRange verses, string colorHex) => new()
        {
            BookId = h.BookId, ChapterNumber = h.ChapterNumber,
            StartVerseNumber = verses.Start, EndVerseNumber = verses.End,
            ColorHex = colorHex, CreatedUtc = h.CreatedUtc,
        };
    }
}
