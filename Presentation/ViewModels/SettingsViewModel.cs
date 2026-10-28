using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TWCF_BibleApp.Application.DTOs;
using TWCF_BibleApp.Application.Services;
using TWCF_BibleApp.Domain.Enums;
using TWCF_BibleApp.Presentation.Services;

namespace TWCF_BibleApp.Presentation.ViewModels
{
    /// <summary>
    /// The Settings page. Switches bind straight to <see cref="Settings"/>; the segmented choices
    /// (theme, spacing, projector) go through the commands and Is* properties here.
    /// </summary>
    public class SettingsViewModel : ObservableObject
    {
        private readonly BibleService _bibleService;
        private bool _loaded;

        public SettingsViewModel(BibleService bibleService, AppSettings settings)
        {
            _bibleService = bibleService ?? throw new ArgumentNullException(nameof(bibleService));
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            // Every Is*/label below derives from the settings; refresh them all on any change.
            Settings.PropertyChanged += (_, _) => OnPropertyChanged(string.Empty);

            SetThemeCommand = new RelayCommand<string>(v => Settings.Theme = Enum.Parse<ThemePreference>(v!));
            SetLineSpacingCommand = new RelayCommand<string>(v => Settings.LineSpacing = v switch
            {
                "Compact" => AppSettings.CompactLineSpacing,
                "Relaxed" => AppSettings.RelaxedLineSpacing,
                _ => AppSettings.NormalLineSpacing,
            });
            SetProjectorThemeCommand = new RelayCommand<string>(v => Settings.ProjectorTheme = Enum.Parse<ProjectorTheme>(v!));
            SetProjectorMarginsCommand = new RelayCommand<string>(v => Settings.ProjectorMargins = Enum.Parse<ProjectorMargins>(v!));
            IncreaseFontCommand = new RelayCommand(() => Settings.FontSize = Settings.FontSize < 40 ? Settings.FontSize + 2 : Settings.FontSize * 1.1);
            DecreaseFontCommand = new RelayCommand(() => Settings.FontSize = Settings.FontSize <= 40 ? Settings.FontSize - 2 : Settings.FontSize / 1.1);
            ResetFontCommand = new RelayCommand(() => Settings.FontSize = AppSettings.DefaultFontSize);
        }

        public AppSettings Settings { get; }

        public IRelayCommand<string> SetThemeCommand { get; }
        public IRelayCommand<string> SetLineSpacingCommand { get; }
        public IRelayCommand<string> SetProjectorThemeCommand { get; }
        public IRelayCommand<string> SetProjectorMarginsCommand { get; }
        public IRelayCommand IncreaseFontCommand { get; }
        public IRelayCommand DecreaseFontCommand { get; }
        public IRelayCommand ResetFontCommand { get; }

        public ObservableCollection<TranslationDto> Translations { get; } = new();

        public TranslationDto? SelectedTranslation
        {
            get => Translations.FirstOrDefault(t => t.Id == Settings.TranslationId);
            set
            {
                if (value != null) Settings.TranslationId = value.Id;
            }
        }

        public bool IsThemeLight => Settings.Theme == ThemePreference.Light;
        public bool IsThemeDark => Settings.Theme == ThemePreference.Dark;
        public bool IsThemeSystem => Settings.Theme == ThemePreference.System;

        public bool IsSpacingCompact => Settings.LineSpacing < (AppSettings.CompactLineSpacing + AppSettings.NormalLineSpacing) / 2;
        public bool IsSpacingRelaxed => Settings.LineSpacing > (AppSettings.NormalLineSpacing + AppSettings.RelaxedLineSpacing) / 2;
        public bool IsSpacingNormal => !IsSpacingCompact && !IsSpacingRelaxed;

        public bool IsProjectorSameAsApp => Settings.ProjectorTheme == ProjectorTheme.SameAsApp;
        public bool IsProjectorLight => Settings.ProjectorTheme == ProjectorTheme.Light;
        public bool IsProjectorDark => Settings.ProjectorTheme == ProjectorTheme.Dark;

        public bool IsMarginsNarrow => Settings.ProjectorMargins == ProjectorMargins.Narrow;
        public bool IsMarginsNormal => Settings.ProjectorMargins == ProjectorMargins.Normal;
        public bool IsMarginsWide => Settings.ProjectorMargins == ProjectorMargins.Wide;

        public string FontSizeLabel => $"{Settings.FontSize:0} pt";
        // The preview shows the real size, up to a point, so the page stays usable.
        public double PreviewFontSize => Math.Min(Settings.FontSize, 56);

        public string AppVersion => $"Version {AppInfo.Current.VersionString}";

        public async Task LoadAsync()
        {
            if (_loaded) return;
            _loaded = true;
            await Settings.LoadAsync();
            foreach (var t in await _bibleService.GetTranslationsAsync())
                Translations.Add(t);
            OnPropertyChanged(string.Empty);
        }
    }
}
