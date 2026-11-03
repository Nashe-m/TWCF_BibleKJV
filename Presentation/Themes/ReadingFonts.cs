using Microsoft.Maui.Devices;
using TWCF_BibleApp.Domain.Enums;

namespace TWCF_BibleApp.Presentation.Themes
{
    /// <summary>
    /// The font family behind each <see cref="ReadingFont"/>. The serif faces are system fonts, so
    /// they differ by platform (Android has one serif, so Book and Serif look alike there); the sans
    /// serif is bundled (Resources/Fonts) and the same everywhere.
    /// </summary>
    public static class ReadingFonts
    {
        public static string FamilyFor(ReadingFont font)
        {
            var android = DeviceInfo.Platform == DevicePlatform.Android;
            var windows = DeviceInfo.Platform == DevicePlatform.WinUI;
            return font switch
            {
                ReadingFont.Book => android ? "serif" : windows ? "Palatino Linotype" : "Palatino",
                ReadingFont.SansSerif => "OpenSansRegular",
                _ => android ? "serif" : "Georgia",
            };
        }
    }
}
