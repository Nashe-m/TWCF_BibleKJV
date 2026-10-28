using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TWCF_BibleApp.Application.DTOs;
using TWCF_BibleApp.Application.Interfaces;
using TWCF_BibleApp.Application.Services;
using TWCF_BibleApp.Domain.Enums;
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
                .Select(b => new BookDto { Id = b.Id, Name = b.Name, Order = b.OrderNumber, Testament = b.Testament })
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
                .Select(v => new VerseDto { Id = v.Id, BookId = v.BookId, ChapterNumber = v.ChapterNumber, VerseNumber = v.VerseNumber, Text = v.Text, MarkedText = v.MarkedText })
                .ToListAsync();
        }

        public async Task<SearchResultsDto> SearchVersesAsync(string query, int translationId, SearchScope scope, int skip, int take, bool matchPrefix = false)
        {
            var match = BuildMatchExpression(query, matchPrefix);
            if (match == null) return SearchResultsDto.Empty;

            // VerseSearch is an FTS5 index over Verses.Text (rowid = Verses.Id), built by Tools/import_usfm.py.
            var where = "VerseSearch MATCH @match AND v.TranslationId = @translation";
            if (scope.BookId != null) where += " AND v.BookId = @book";
            if (scope.Testament != null) where += " AND b.Testament = @testament";
            const string from = "FROM VerseSearch JOIN Verses v ON v.Id = VerseSearch.rowid JOIN Books b ON b.Id = v.BookId";

            // A parameter can only belong to one command, so each query gets its own set.
            SqliteParameter[] Parameters() => new[]
            {
                new SqliteParameter("@match", match),
                new SqliteParameter("@translation", translationId),
                new SqliteParameter("@book", scope.BookId ?? 0),
                new SqliteParameter("@testament", scope.Testament == Testament.New ? "NT" : "OT"),
                new SqliteParameter("@take", take),
                new SqliteParameter("@skip", skip),
            };

            var total = await _db.Database
                .SqlQueryRaw<int>($"SELECT COUNT(*) AS Value {from} WHERE {where}", Parameters())
                .SingleAsync();
            if (total == 0) return SearchResultsDto.Empty;

            var rows = await _db.Database
                .SqlQueryRaw<SearchRow>(
                    "SELECT v.BookId, b.Name AS BookName, v.ChapterNumber, v.VerseNumber, v.Text, " +
                    "highlight(VerseSearch, 0, '<m>', '</m>') AS HighlightedText " +
                    $"{from} WHERE {where} ORDER BY b.OrderNumber, v.ChapterNumber, v.VerseNumber LIMIT @take OFFSET @skip",
                    Parameters())
                .ToListAsync();

            return new SearchResultsDto
            {
                TotalCount = total,
                Items = rows.Select(r => new SearchResultDto
                {
                    BookId = r.BookId,
                    Chapter = r.ChapterNumber,
                    Verse = r.VerseNumber,
                    Reference = $"{ReferenceParser.ReferenceBookName(r.BookName)} {r.ChapterNumber}:{r.VerseNumber}",
                    Text = r.Text,
                    HighlightedText = r.HighlightedText,
                }).ToList(),
            };
        }

        /// <summary>
        /// Turns what the user typed into an FTS5 query: every word must appear, "quoted text" must
        /// appear as a phrase. Punctuation is dropped so it can never be read as FTS syntax.
        /// </summary>
        internal static string? BuildMatchExpression(string input, bool matchPrefix)
        {
            var terms = new List<string>();
            var lastIsOpenWord = false;
            foreach (Match m in Regex.Matches(input ?? "", @"""([^""]*)""?|([^\s""]+)"))
            {
                var words = Regex.Matches(m.Value, @"[\p{L}\p{N}\p{M}]+").Select(w => w.Value).ToList();
                if (words.Count == 0) continue;
                terms.Add("\"" + string.Join(" ", words) + "\"");
                lastIsOpenWord = m.Groups[2].Success;
            }
            if (terms.Count == 0) return null;

            // Only extend a word that is still being typed, not one followed by a space.
            if (matchPrefix && lastIsOpenWord && !char.IsWhiteSpace(input![^1]))
                terms[^1] += "*";
            return string.Join(" ", terms);
        }

        private sealed class SearchRow
        {
            public int BookId { get; set; }
            public string BookName { get; set; } = string.Empty;
            public int ChapterNumber { get; set; }
            public int VerseNumber { get; set; }
            public string Text { get; set; } = string.Empty;
            public string HighlightedText { get; set; } = string.Empty;
        }

        public async Task<IReadOnlyList<VerseDto>> GetVersesForChaptersAsync(IEnumerable<(int BookId, int Chapter)> chapters, int translationId)
        {
            // One query per book: EF can't translate a Contains over (book, chapter) pairs.
            var result = new List<VerseDto>();
            foreach (var book in chapters.GroupBy(c => c.BookId))
            {
                var numbers = book.Select(c => c.Chapter).Distinct().ToList();
                result.AddRange(await _db.Verses
                    .AsNoTracking()
                    .Where(v => v.TranslationId == translationId && v.BookId == book.Key && numbers.Contains(v.ChapterNumber))
                    .Select(v => new VerseDto { Id = v.Id, BookId = v.BookId, ChapterNumber = v.ChapterNumber, VerseNumber = v.VerseNumber, Text = v.Text, MarkedText = v.MarkedText })
                    .ToListAsync());
            }
            return result;
        }

        public async Task<IEnumerable<TranslationDto>> GetTranslationsAsync()
        {
            return await _db.Translations
                .AsNoTracking()
                .OrderBy(t => t.Id)
                .Select(t => new TranslationDto { Id = t.Id, Code = t.Code, Name = t.Name, Language = t.Language, Copyright = t.Copyright, HasWordsOfJesus = t.HasWordsOfJesus })
                .ToListAsync();
        }

        public async Task<TranslationDto?> GetTranslationAsync(int translationId)
        {
            var t = await _db.Translations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == translationId);
            if (t == null) return null;
            return new TranslationDto { Id = t.Id, Code = t.Code, Name = t.Name, Language = t.Language, Copyright = t.Copyright, HasWordsOfJesus = t.HasWordsOfJesus };
        }
    }
}
