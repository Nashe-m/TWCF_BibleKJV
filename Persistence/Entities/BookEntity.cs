using TWCF_BibleApp.Domain.Enums;

namespace TWCF_BibleApp.Persistence.Entities
{
    public sealed class BookEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int OrderNumber { get; set; }
        public Testament Testament { get; set; }
    }
}
