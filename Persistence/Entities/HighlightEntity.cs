using System;

namespace TWCF_BibleApp.Persistence.Entities
{
    public sealed class HighlightEntity
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        public int ChapterNumber { get; set; }
        public int StartVerseNumber { get; set; }
        public int EndVerseNumber { get; set; }
        public string ColorHex { get; set; } = "#FFFF00";
        public DateTime CreatedUtc { get; set; }
    }
}
