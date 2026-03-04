using System;

namespace TWCF_BibleApp.Persistence.Entities
{
    public sealed class NoteEntity
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        public int ChapterNumber { get; set; }
        public int StartVerseNumber { get; set; }
        public int? EndVerseNumber { get; set; }
        public string Text { get; set; } = string.Empty;
        public DateTime CreatedUtc { get; set; }
        public DateTime? ModifiedUtc { get; set; }
    }
}
