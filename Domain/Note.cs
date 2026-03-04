using System;

namespace TWCF_BibleApp.Domain
{
    public sealed class Note
    {
        public int Id { get; init; }
        public int BookId { get; init; }
        public int ChapterNumber { get; init; }
        public int StartVerseNumber { get; init; }
        public int? EndVerseNumber { get; init; }
        public string Text { get; init; } = string.Empty;
        public DateTime CreatedUtc { get; init; }
        public DateTime? ModifiedUtc { get; init; }

        public Note() { }

        public Note(int id, int bookId, int chapterNumber, int startVerse, int? endVerse, string text)
        {
            if (chapterNumber < 1) throw new ArgumentOutOfRangeException(nameof(chapterNumber));
            if (startVerse < 1) throw new ArgumentOutOfRangeException(nameof(startVerse));
            if (endVerse.HasValue && endVerse.Value < startVerse) throw new ArgumentException("End verse must be >= start verse", nameof(endVerse));
            if (text is null) throw new ArgumentNullException(nameof(text));

            Id = id;
            BookId = bookId;
            ChapterNumber = chapterNumber;
            StartVerseNumber = startVerse;
            EndVerseNumber = endVerse;
            Text = text;
            CreatedUtc = DateTime.UtcNow;
        }
    }
}
