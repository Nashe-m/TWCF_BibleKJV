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

        public UserSetting() { }

        public UserSetting(int id, int lastBookId, int lastChapterNumber, double fontScale, ThemePreference theme)
        {
            if (lastChapterNumber < 0) throw new ArgumentOutOfRangeException(nameof(lastChapterNumber));
            if (fontScale <= 0) throw new ArgumentOutOfRangeException(nameof(fontScale));

            Id = id;
            LastBookId = lastBookId;
            LastChapterNumber = lastChapterNumber;
            FontScale = fontScale;
            Theme = theme;
        }
    }
}
