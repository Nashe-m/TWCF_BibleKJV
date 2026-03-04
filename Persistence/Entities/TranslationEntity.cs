namespace TWCF_BibleApp.Persistence.Entities
{
    public sealed class TranslationEntity
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
