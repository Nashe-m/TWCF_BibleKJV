namespace TWCF_BibleApp.Persistence.Entities
{
    public sealed class TranslationEntity
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string Copyright { get; set; } = string.Empty;
        public bool HasWordsOfJesus { get; set; }
    }
}
