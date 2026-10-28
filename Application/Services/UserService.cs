using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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


        // The repositories share one DbContext, which can't run two operations at once, and the
        // reader, Search, Notes and Highlights pages call in independently. Every call takes this lock.
        private readonly SemaphoreSlim _lock = new(1, 1);

        private async Task<T> Locked<T>(Func<Task<T>> work)
        {
            await _lock.WaitAsync();
            try { return await work(); }
            finally { _lock.Release(); }
        }

        private async Task Locked(Func<Task> work)
        {
            await _lock.WaitAsync();
            try { await work(); }
            finally { _lock.Release(); }
        }

        public Task<UserSetting?> GetUserSettingAsync() => Locked(() => _repo.GetUserSettingAsync());
        public Task SaveUserSettingAsync(UserSetting setting) => Locked(() => _repo.SaveUserSettingAsync(setting));

        public Task<IEnumerable<Note>> GetNotesAsync(int bookId, int chapterNumber) => Locked(() => _repo.GetNotesAsync(bookId, chapterNumber));
        public Task<IEnumerable<Note>> GetAllNotesAsync() => Locked(() => _repo.GetAllNotesAsync());
        public Task<Note> AddNoteAsync(Note note) => Locked(() => _repo.AddNoteAsync(note));
        public Task UpdateNoteAsync(Note note) => Locked(() => _repo.UpdateNoteAsync(note));
        public Task DeleteNoteAsync(int noteId) => Locked(() => _repo.DeleteNoteAsync(noteId));

        public Task<IEnumerable<Highlight>> GetHighlightsAsync(int bookId, int chapterNumber) => Locked(() => _repo.GetHighlightsAsync(bookId, chapterNumber));
        public Task<IEnumerable<Highlight>> GetAllHighlightsAsync() => Locked(() => _repo.GetAllHighlightsAsync());
        public Task<Highlight> AddHighlightAsync(Highlight highlight) => Locked(() => _repo.AddHighlightAsync(highlight));
        public Task DeleteHighlightAsync(int highlightId) => Locked(() => _repo.DeleteHighlightAsync(highlightId));

        /// <summary>
        /// Highlights the given verses of a chapter in one colour, or removes their highlight when
        /// colorHex is null. Existing highlights that only partly overlap keep their other verses.
        /// Returns the chapter's highlights afterwards.
        /// </summary>
        public Task<IReadOnlyList<Highlight>> SetHighlightAsync(int bookId, int chapterNumber, IReadOnlyCollection<int> verses, string? colorHex) =>
            Locked(() => SetHighlightCoreAsync(bookId, chapterNumber, verses, colorHex));

        private async Task<IReadOnlyList<Highlight>> SetHighlightCoreAsync(int bookId, int chapterNumber, IReadOnlyCollection<int> verses, string? colorHex)
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

        /// <summary>
        /// Replaces a chapter's highlights with an earlier snapshot (from GetHighlightsAsync), for undo.
        /// Returns the chapter's highlights afterwards.
        /// </summary>
        public Task<IReadOnlyList<Highlight>> RestoreChapterHighlightsAsync(int bookId, int chapterNumber, IReadOnlyCollection<Highlight> snapshot) =>
            Locked<IReadOnlyList<Highlight>>(async () =>
            {
                foreach (var h in await _repo.GetHighlightsAsync(bookId, chapterNumber))
                    await _repo.DeleteHighlightAsync(h.Id);
                foreach (var h in snapshot)
                    await _repo.AddHighlightAsync(Copy(h, new VerseRange(h.StartVerseNumber, h.EndVerseNumber), h.ColorHex));
                return (await _repo.GetHighlightsAsync(bookId, chapterNumber)).ToList();
            });

        private static Highlight Copy(Highlight h, VerseRange verses, string colorHex) => new()
        {
            BookId = h.BookId, ChapterNumber = h.ChapterNumber,
            StartVerseNumber = verses.Start, EndVerseNumber = verses.End,
            ColorHex = colorHex, CreatedUtc = h.CreatedUtc,
        };
    }
}
