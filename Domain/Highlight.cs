using System;

namespace TWCF_BibleApp.Domain
{
    public sealed class Highlight
    {
        public int Id { get; init; }
        public int BookId { get; init; }
        public int ChapterNumber { get; init; }
        public int StartVerseNumber { get; init; }
        public int EndVerseNumber { get; init; }
        public string ColorHex { get; init; } = "#FFFF00"; // default yellow
        public DateTime CreatedUtc { get; init; }

        public Highlight() { }

        public Highlight(int id, int bookId, int chapterNumber, int startVerse, int endVerse, string colorHex)
        {
            if (chapterNumber < 1) throw new ArgumentOutOfRangeException(nameof(chapterNumber));
            if (startVerse < 1) throw new ArgumentOutOfRangeException(nameof(startVerse));
            if (endVerse < startVerse) throw new ArgumentException("End verse must be >= start verse", nameof(endVerse));
            if (string.IsNullOrWhiteSpace(colorHex)) throw new ArgumentException("Color is required", nameof(colorHex));

            Id = id;
            BookId = bookId;
            ChapterNumber = chapterNumber;
            StartVerseNumber = startVerse;
            EndVerseNumber = endVerse;
            ColorHex = colorHex;
            CreatedUtc = DateTime.UtcNow;
        }
    }
}
