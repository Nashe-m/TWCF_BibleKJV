using System;

namespace TWCF_BibleApp.Domain
{
    public sealed class Chapter
    {
        public int BookId { get; init; }
        public int Number { get; init; }
        public int VerseCount { get; init; }

        public Chapter() { }

        public Chapter(int bookId, int number, int verseCount)
        {
            if (number < 1) throw new ArgumentOutOfRangeException(nameof(number));
            if (verseCount < 0) throw new ArgumentOutOfRangeException(nameof(verseCount));

            BookId = bookId;
            Number = number;
            VerseCount = verseCount;
        }
    }
}
