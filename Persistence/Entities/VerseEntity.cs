using TWCF_BibleApp.Domain;

namespace TWCF_BibleApp.Persistence.Entities
{
    public sealed class VerseEntity
    {
        public int Id { get; set; }
        public int TranslationId { get; set; }
        public int BookId { get; set; }
        public int ChapterNumber { get; set; }
        public int VerseNumber { get; set; }
        public string Text { get; set; } = string.Empty;
        public BookEntity Book { get; set; } = null!;
    }
}
