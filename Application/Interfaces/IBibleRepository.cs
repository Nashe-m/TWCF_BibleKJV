using System.Collections.Generic;
using System.Threading.Tasks;
using TWCF_BibleApp.Application.DTOs;
using TWCF_BibleApp.Domain;

namespace TWCF_BibleApp.Application.Interfaces
{
    public interface IBibleRepository
    {
        Task<IEnumerable<BookDto>> GetBooksAsync();
        Task<IEnumerable<ChapterDto>> GetChaptersForBookAsync(int bookId);
        /// <summary>
        /// Load full chapter (all verses) in a single query. Always read-only.
        /// </summary>
        Task<IEnumerable<VerseDto>> GetChapterVersesAsync(int bookId, int chapterNumber, int translationId);
        /// <summary>Full-text search of verse text; one page of results plus the total.</summary>
        Task<SearchResultsDto> SearchVersesAsync(SearchRequest request);
        /// <summary>Matches per book for a search, across the whole Bible (the request's scope is ignored).</summary>
        Task<IReadOnlyList<BookMatchCount>> CountMatchesByBookAsync(SearchRequest request);
        Task<IEnumerable<TranslationDto>> GetTranslationsAsync();
        /// <summary>All verses of the given chapters in one translation.</summary>
        Task<IReadOnlyList<VerseDto>> GetVersesForChaptersAsync(IEnumerable<(int BookId, int Chapter)> chapters, int translationId);
        Task<TranslationDto?> GetTranslationAsync(int translationId);
    }
}
