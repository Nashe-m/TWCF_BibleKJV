using System;
using TWCF_BibleApp.Persistence.Entities;

namespace TWCF_BibleApp.Domain
{
    public sealed class Verse
    {
        public int Id { get; init; }
        public int TranslationId { get; init; }
        public int BookId { get; init; }
        public int ChapterNumber { get; init; }
        public int VerseNumber { get; init; }
        public string Text { get; init; } = string.Empty;
        public Book Book { get; init; }

        public Verse() { }

        public Verse(int id, int translationId, int bookId, int chapterNumber, int verseNumber, string text, Book book)
        {
            if (chapterNumber < 1) throw new ArgumentOutOfRangeException(nameof(chapterNumber));
            if (verseNumber < 1) throw new ArgumentOutOfRangeException(nameof(verseNumber));
            if (text is null) throw new ArgumentNullException(nameof(text));

            Id = id;
            TranslationId = translationId;
            BookId = bookId;
            ChapterNumber = chapterNumber;
            VerseNumber = verseNumber;
            Text = text;
            Book = book;
        }
    }
}
