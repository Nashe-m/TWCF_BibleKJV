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
        Task<List<Verse>> SearchVersesAsync(string query, int translationId);
        Task<IEnumerable<TranslationDto>> GetTranslationsAsync();
        Task<TranslationDto?> GetTranslationAsync(int translationId);
    }
}
