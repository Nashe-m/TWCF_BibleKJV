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
        /// <summary>
        /// Full-text search of verse text in Bible order. With matchPrefix the last word also
        /// matches longer words ("shep" finds "shepherd"), for search-as-you-type.
        /// </summary>
        Task<SearchResultsDto> SearchVersesAsync(string query, int translationId, SearchScope scope, int skip, int take, bool matchPrefix = false);
        Task<IEnumerable<TranslationDto>> GetTranslationsAsync();
        /// <summary>All verses of the given chapters in one translation.</summary>
        Task<IReadOnlyList<VerseDto>> GetVersesForChaptersAsync(IEnumerable<(int BookId, int Chapter)> chapters, int translationId);
        Task<TranslationDto?> GetTranslationAsync(int translationId);
    }
}
