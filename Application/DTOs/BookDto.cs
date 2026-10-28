using TWCF_BibleApp.Domain.Enums;

namespace TWCF_BibleApp.Application.DTOs
{
    public sealed class BookDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Order { get; set; }
        public Testament Testament { get; set; }
    }
}
