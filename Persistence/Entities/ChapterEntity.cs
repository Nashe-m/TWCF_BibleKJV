namespace TWCF_BibleApp.Persistence.Entities
{
    public sealed class ChapterEntity
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        public int Number { get; set; }
        public int VerseCount { get; set; }
    }
}
