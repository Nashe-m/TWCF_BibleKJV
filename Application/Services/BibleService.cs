using System.Collections.Generic;
using System.Linq;
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

        public Task<IEnumerable<BookDto>> GetBooksAsync() => _repo.GetBooksAsync();
        public Task<IEnumerable<ChapterDto>> GetChaptersForBookAsync(int bookId) => _repo.GetChaptersForBookAsync(bookId);
        public Task<IEnumerable<VerseDto>> GetChapterVersesAsync(int bookId, int chapterNumber, int translationId) => _repo.GetChapterVersesAsync(bookId, chapterNumber, translationId);
        public Task<IEnumerable<TranslationDto>> GetTranslationsAsync() => _repo.GetTranslationsAsync();
    }
}
