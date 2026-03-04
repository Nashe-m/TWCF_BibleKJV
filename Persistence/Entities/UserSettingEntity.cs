using TWCF_BibleApp.Domain.Enums;

namespace TWCF_BibleApp.Persistence.Entities
{
    public sealed class UserSettingEntity
    {
        public int Id { get; set; }
        public int LastBookId { get; set; }
        public int LastChapterNumber { get; set; }
        public double FontScale { get; set; } = 1.0;
        public ThemePreference Theme { get; set; } = ThemePreference.System;
    }
}
