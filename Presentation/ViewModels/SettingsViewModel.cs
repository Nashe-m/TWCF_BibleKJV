using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using TWCF_BibleApp.Application.DTOs;
using TWCF_BibleApp.Application.Services;

namespace TWCF_BibleApp.Presentation.ViewModels
{
    /// <summary>Settings page. For now only the About section; display settings arrive in Phase 6.</summary>
    public class SettingsViewModel
    {
        private readonly BibleService _bibleService;
        private bool _loaded;

        public SettingsViewModel(BibleService bibleService)
        {
            _bibleService = bibleService ?? throw new ArgumentNullException(nameof(bibleService));
        }

        public ObservableCollection<TranslationDto> Translations { get; } = new();

        public string AppVersion => $"Version {AppInfo.Current.VersionString}";

        public async Task LoadAsync()
        {
            if (_loaded) return;
            _loaded = true;
            foreach (var t in await _bibleService.GetTranslationsAsync())
                Translations.Add(t);
        }
    }
}
