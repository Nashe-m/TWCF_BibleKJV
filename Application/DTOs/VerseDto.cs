namespace TWCF_BibleApp.Application.DTOs
{
    public sealed class VerseDto
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        public int ChapterNumber { get; set; }
        public int VerseNumber { get; set; }
        public string Text { get; set; } = string.Empty;
    }
}
