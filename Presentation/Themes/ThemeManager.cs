using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace TWCF_BibleApp.Presentation.Themes
{
    /// <summary>
    /// Switches the colour tokens in Presentation/Themes/Colors.xaml between light and dark.
    /// </summary>
    public static class ThemeManager
    {
        private static readonly string[] Tokens =
        {
            "PageBackgroundColor", "SurfaceColor", "BorderColor", "PageTextColor", "MutedTextColor",
            "AccentColor", "AccentTextColor", "AccentSoftColor", "ChipColor", "WordsOfJesusColor"
        };

        public static void Apply(bool dark)
        {
            var app = Microsoft.Maui.Controls.Application.Current;
            if (app == null) return;

            app.UserAppTheme = dark ? AppTheme.Dark : AppTheme.Light;

            var prefix = dark ? "Dark" : "Light";
            foreach (var token in Tokens)
            {
                if (app.Resources.TryGetValue(prefix + token, out var value))
                    app.Resources[token] = value;
            }
        }
    }
}
