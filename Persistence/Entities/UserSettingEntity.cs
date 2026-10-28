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
        public int LastTranslationId { get; set; } = 1;
        public bool ShowWordsOfJesus { get; set; } = true;
        public double LineSpacing { get; set; } = 1.45;
        public bool ShowVerseNumbers { get; set; } = true;
        public ProjectorTheme ProjectorTheme { get; set; } = ProjectorTheme.SameAsApp;
        public ProjectorMargins ProjectorMargins { get; set; } = ProjectorMargins.Normal;
        public bool ShowAnnotationsWhenProjecting { get; set; }
        public bool MarkJumpWhenProjecting { get; set; } = true;
    }
}
