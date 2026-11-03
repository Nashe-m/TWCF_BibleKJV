using System;
using TWCF_BibleApp.Domain.Enums;

namespace TWCF_BibleApp.Domain
{
    public sealed class UserSetting
    {
        public int Id { get; init; }
        public int LastBookId { get; init; }
        public int LastChapterNumber { get; init; }
        public double FontScale { get; init; } = 1.0; // 1.0 = default
        public ThemePreference Theme { get; init; } = ThemePreference.System;
        public int LastTranslationId { get; init; } = 1; // 1 = KJV
        public bool ShowWordsOfJesus { get; init; } = true; // red-letter text
        public double LineSpacing { get; init; } = 1.45;
        public bool ShowVerseNumbers { get; init; } = true;
        public ReadingFont ReadingFont { get; init; } = ReadingFont.Serif;
        public ProjectorTheme ProjectorTheme { get; init; } = ProjectorTheme.SameAsApp;
        public ProjectorMargins ProjectorMargins { get; init; } = ProjectorMargins.Normal;
        // While projecting, only the text shows unless these are turned on.
        public bool ShowAnnotationsWhenProjecting { get; init; }
        public bool MarkJumpWhenProjecting { get; init; } = true;

        public UserSetting() { }

        public UserSetting(int id, int lastBookId, int lastChapterNumber, double fontScale, ThemePreference theme,
            int lastTranslationId = 1, bool showWordsOfJesus = true)
        {
            if (lastChapterNumber < 0) throw new ArgumentOutOfRangeException(nameof(lastChapterNumber));
            if (fontScale <= 0) throw new ArgumentOutOfRangeException(nameof(fontScale));
            if (lastTranslationId < 1) throw new ArgumentOutOfRangeException(nameof(lastTranslationId));

            Id = id;
            LastBookId = lastBookId;
            LastChapterNumber = lastChapterNumber;
            FontScale = fontScale;
            Theme = theme;
            LastTranslationId = lastTranslationId;
            ShowWordsOfJesus = showWordsOfJesus;
        }
    }
}
