namespace TWCF_BibleApp.Application.DTOs
{
    public sealed class TranslationDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string Copyright { get; set; } = string.Empty;
        public bool HasWordsOfJesus { get; set; }

        public string DisplayName => $"{Code} · {Name}";
    }
}
