using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TWCF_BibleApp.Application.DTOs;
using TWCF_BibleApp.Application.Interfaces;

namespace TWCF_BibleApp.Application.Services
{
    public sealed class BibleService
    {
        private readonly IBibleRepository _repo;

        public BibleService(IBibleRepository repo)
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

        public Task<IEnumerable<BookDto>> GetBooksAsync() => Locked(() => _repo.GetBooksAsync());
        public Task<IEnumerable<ChapterDto>> GetChaptersForBookAsync(int bookId) => Locked(() => _repo.GetChaptersForBookAsync(bookId));
        public Task<IEnumerable<VerseDto>> GetChapterVersesAsync(int bookId, int chapterNumber, int translationId) => Locked(() => _repo.GetChapterVersesAsync(bookId, chapterNumber, translationId));
        public Task<IEnumerable<TranslationDto>> GetTranslationsAsync() => Locked(() => _repo.GetTranslationsAsync());
        public Task<SearchResultsDto> SearchVersesAsync(SearchRequest request) => Locked(() => _repo.SearchVersesAsync(request));
        public Task<IReadOnlyList<BookMatchCount>> CountMatchesByBookAsync(SearchRequest request) => Locked(() => _repo.CountMatchesByBookAsync(request));

        /// <summary>
        /// The text of the given verse ranges, e.g. for listing notes and highlights.
        /// Keyed by (book, chapter, start verse, end verse).
        /// </summary>
        public Task<IReadOnlyDictionary<(int BookId, int Chapter, int Start, int End), string>> GetPassageTextsAsync(
            IEnumerable<(int BookId, int Chapter, int Start, int End)> passages, int translationId) =>
            Locked(async () =>
            {
                var wanted = passages.Distinct().ToList();
                var verses = await _repo.GetVersesForChaptersAsync(wanted.Select(p => (p.BookId, p.Chapter)).Distinct(), translationId);
                var byChapter = verses.ToLookup(v => (v.BookId, v.ChapterNumber));
                return (IReadOnlyDictionary<(int, int, int, int), string>)wanted.ToDictionary(p => p, p => string.Join(" ",
                    byChapter[(p.BookId, p.Chapter)].Where(v => v.VerseNumber >= p.Start && v.VerseNumber <= p.End)
                        .OrderBy(v => v.VerseNumber).Select(v => v.Text)));
            });
    }
}
