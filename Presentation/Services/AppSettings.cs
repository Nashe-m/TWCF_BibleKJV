using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using TWCF_BibleApp.Application.Services;
using TWCF_BibleApp.Domain;
using TWCF_BibleApp.Domain.Enums;
using TWCF_BibleApp.Presentation.Themes;

namespace TWCF_BibleApp.Presentation.Services
{
    /// <summary>
    /// The user's display settings and reading position, shared by the reader and the Settings
    /// page so a change on one shows on the other at once. Saved (debounced) to the UserSettings row.
    /// </summary>
    public sealed class AppSettings : ObservableObject
    {
        // Reading font size in points = BaseFontSize * FontScale (FontScale is what UserSetting stores).
        public const double BaseFontSize = 14.0;
        public const double MinFontSize = 16.0;
        public const double MaxFontSize = 144.0;
        public const double DefaultFontSize = 28.0;

        public const double CompactLineSpacing = 1.25;
        public const double NormalLineSpacing = 1.45;
        public const double RelaxedLineSpacing = 1.7;

        private readonly UserService _userService;
        private CancellationTokenSource? _saveDebounce;
        private Task? _loading;
        private bool _loaded;
        private bool _isProjecting;

        public AppSettings(UserService userService)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        }

        // ---- Display ------------------------------------------------------------------------

        private double _fontSize = DefaultFontSize;
        public double FontSize
        {
            get => _fontSize;
            set => Set(ref _fontSize, Math.Round(Math.Clamp(value, MinFontSize, MaxFontSize)));
        }

        private ThemePreference _theme = ThemePreference.System;
        public ThemePreference Theme
        {
            get => _theme;
            set
            {
                if (Set(ref _theme, value)) ApplyTheme();
            }
        }

        private bool _showWordsOfJesus = true;
        public bool ShowWordsOfJesus { get => _showWordsOfJesus; set => Set(ref _showWordsOfJesus, value); }

        private double _lineSpacing = NormalLineSpacing;
        public double LineSpacing { get => _lineSpacing; set => Set(ref _lineSpacing, Math.Clamp(value, 1.0, 2.5)); }

        private bool _showVerseNumbers = true;
        public bool ShowVerseNumbers { get => _showVerseNumbers; set => Set(ref _showVerseNumbers, value); }

        private ReadingFont _readingFont = ReadingFont.Serif;
        public ReadingFont ReadingFont
        {
            get => _readingFont;
            set
            {
                if (Set(ref _readingFont, value)) ApplyReadingFont();
            }
        }

        /// <summary>The font family for scripture text; also published as the ScriptureFont resource.</summary>
        public string ScriptureFontFamily => ReadingFonts.FamilyFor(ReadingFont);

        private void ApplyReadingFont()
        {
            if (Microsoft.Maui.Controls.Application.Current is { } app)
                app.Resources["ScriptureFont"] = ScriptureFontFamily;
            OnPropertyChanged(nameof(ScriptureFontFamily));
        }

        // ---- Projector ----------------------------------------------------------------------

        private ProjectorTheme _projectorTheme = ProjectorTheme.SameAsApp;
        public ProjectorTheme ProjectorTheme
        {
            get => _projectorTheme;
            set
            {
                if (Set(ref _projectorTheme, value)) ApplyTheme();
            }
        }

        private ProjectorMargins _projectorMargins = ProjectorMargins.Normal;
        public ProjectorMargins ProjectorMargins { get => _projectorMargins; set => Set(ref _projectorMargins, value); }

        private bool _showAnnotationsWhenProjecting;
        public bool ShowAnnotationsWhenProjecting { get => _showAnnotationsWhenProjecting; set => Set(ref _showAnnotationsWhenProjecting, value); }

        private bool _markJumpWhenProjecting = true;
        public bool MarkJumpWhenProjecting { get => _markJumpWhenProjecting; set => Set(ref _markJumpWhenProjecting, value); }

        // ---- Version and position -----------------------------------------------------------

        private int _translationId = 1;
        public int TranslationId { get => _translationId; set => Set(ref _translationId, Math.Max(1, value)); }

        public int LastBookId { get; private set; }
        public int LastChapter { get; private set; }

        /// <summary>Remembers where the reader is; saved with the other settings.</summary>
        public void SetPosition(int bookId, int chapter)
        {
            if (bookId == LastBookId && chapter == LastChapter) return;
            LastBookId = bookId;
            LastChapter = chapter;
            QueueSave();
        }

        // ---- Theme --------------------------------------------------------------------------

        /// <summary>Whether dark colours are showing now (the projector theme while projecting).</summary>
        public bool IsDarkActive { get; private set; }

        /// <summary>The reader calls this on entering and leaving projector mode.</summary>
        public void SetProjecting(bool projecting)
        {
            if (_isProjecting == projecting) return;
            _isProjecting = projecting;
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            var app = Microsoft.Maui.Controls.Application.Current;
            var followSystem = false;
            bool dark;
            if (_isProjecting && ProjectorTheme != ProjectorTheme.SameAsApp)
                dark = ProjectorTheme == ProjectorTheme.Dark;
            else if (Theme == ThemePreference.System)
            {
                followSystem = true;
                dark = app?.PlatformAppTheme == AppTheme.Dark;
            }
            else
                dark = Theme == ThemePreference.Dark;

            ThemeManager.Apply(dark, followSystem);
            if (IsDarkActive == dark) return;
            IsDarkActive = dark;
            OnPropertyChanged(nameof(IsDarkActive));
        }

        // ---- Loading and saving -------------------------------------------------------------

        public Task LoadAsync() => _loading ??= LoadCoreAsync();

        private async Task LoadCoreAsync()
        {
            UserSetting? s = null;
            try
            {
                s = await _userService.GetUserSettingAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Loading settings failed: {ex}");
            }

            if (s != null)
            {
                // Fields, not setters: loading must not trigger a save.
                _fontSize = Math.Clamp(Math.Round(BaseFontSize * s.FontScale), MinFontSize, MaxFontSize);
                _theme = s.Theme;
                _showWordsOfJesus = s.ShowWordsOfJesus;
                _lineSpacing = s.LineSpacing;
                _showVerseNumbers = s.ShowVerseNumbers;
                _readingFont = s.ReadingFont;
                _projectorTheme = s.ProjectorTheme;
                _projectorMargins = s.ProjectorMargins;
                _showAnnotationsWhenProjecting = s.ShowAnnotationsWhenProjecting;
                _markJumpWhenProjecting = s.MarkJumpWhenProjecting;
                _translationId = s.LastTranslationId;
                LastBookId = s.LastBookId;
                LastChapter = s.LastChapterNumber;
                OnPropertyChanged(string.Empty);
            }

            _loaded = true;
            ApplyTheme();
            ApplyReadingFont();

            // "System" follows Windows / the phone when its theme changes.
            if (Microsoft.Maui.Controls.Application.Current is { } app)
                app.RequestedThemeChanged += (_, _) =>
                {
                    if (Theme == ThemePreference.System) ApplyTheme();
                };
        }

        private bool Set<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string? name = null)
        {
            if (!SetProperty(ref field, value, name)) return false;
            QueueSave();
            return true;
        }

        // Settings change in quick bursts (font size, chapter turns); write once things settle.
        private void QueueSave()
        {
            if (!_loaded) return;

            _saveDebounce?.Cancel();
            var cts = _saveDebounce = new CancellationTokenSource();
            _ = Task.Delay(600, cts.Token).ContinueWith(async _ =>
            {
                try
                {
                    await _userService.SaveUserSettingAsync(new UserSetting(0, LastBookId, LastChapter, FontSize / BaseFontSize, Theme,
                        TranslationId, ShowWordsOfJesus)
                    {
                        LineSpacing = LineSpacing,
                        ShowVerseNumbers = ShowVerseNumbers,
                        ReadingFont = ReadingFont,
                        ProjectorTheme = ProjectorTheme,
                        ProjectorMargins = ProjectorMargins,
                        ShowAnnotationsWhenProjecting = ShowAnnotationsWhenProjecting,
                        MarkJumpWhenProjecting = MarkJumpWhenProjecting,
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Saving settings failed: {ex}");
                }
            }, cts.Token, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
        }
    }
}
