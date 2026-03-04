using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TWCF_BibleApp.Application.DTOs;
using TWCF_BibleApp.Application.Interfaces;
using TWCF_BibleApp.Domain;
using TWCF_BibleApp.Persistence.Contexts;

namespace TWCF_BibleApp.Persistence.Repositories
{
    public sealed class BibleRepository : IBibleRepository
    {
        private readonly BibleDataContext _db;

        public BibleRepository(BibleDataContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<BookDto>> GetBooksAsync()
        {
            return await _db.Books
                .AsNoTracking()
                .OrderBy(b => b.OrderNumber)
                .Select(b => new BookDto { Id = b.Id, Name = b.Name, Order = b.OrderNumber, Testament = b.Testament.ToString() })
                .ToListAsync();
        }

        public async Task<IEnumerable<ChapterDto>> GetChaptersForBookAsync(int bookId)
        {
            return await _db.Chapters
                .AsNoTracking()
                .Where(c => c.BookId == bookId)
                .OrderBy(c => c.Number)
                .Select(c => new ChapterDto { BookId = c.BookId, Number = c.Number, VerseCount = c.VerseCount })
                .ToListAsync();
        }

        public async Task<IEnumerable<VerseDto>> GetChapterVersesAsync(int bookId, int chapterNumber, int translationId)
        {
            return await _db.Verses
                .AsNoTracking()
                .Where(v => v.BookId == bookId && v.ChapterNumber == chapterNumber && v.TranslationId == translationId)
                .OrderBy(v => v.VerseNumber)
                .Select(v => new VerseDto { Id = v.Id, BookId = v.BookId, ChapterNumber = v.ChapterNumber, VerseNumber = v.VerseNumber, Text = v.Text })
                .ToListAsync();
        }

        public async Task<List<Verse>> SearchVersesAsync(string query)
        {
            var entities = await _db.Verses
                .Include(v => v.Book)
                .Where(v => EF.Functions.Like(v.Text, $"%{query}%"))
                .OrderBy(v => v.Book.OrderNumber)
                .ThenBy(v => v.ChapterNumber)
                .ThenBy(v => v.VerseNumber)
                .ToListAsync();

            return entities.Select(v => new Verse(
                v.Id,
                v.TranslationId,
                v.BookId,
                v.ChapterNumber,
                v.VerseNumber,
                v.Text,
                new Domain.Book(
                           v.Book.Id,
                           v.Book.Name,
                           v.Book.OrderNumber,
                           v.Book.Testament
                               )
                  )).ToList();
        }

        public async Task<IEnumerable<TranslationDto>> GetTranslationsAsync()
        {
            return await _db.Translations
                .AsNoTracking()
                .Select(t => new TranslationDto { Id = t.Id, Code = t.Code, Name = t.Name })
                .ToListAsync();
        }

        public async Task<TranslationDto?> GetTranslationAsync(int translationId)
        {
            var t = await _db.Translations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == translationId);
            if (t == null) return null;
            return new TranslationDto { Id = t.Id, Code = t.Code, Name = t.Name };
        }
    }
}
