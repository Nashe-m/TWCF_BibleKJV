using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using TWCF_BibleApp.Application.DTOs;
using TWCF_BibleApp.Application.Services;
using TWCF_BibleApp.Domain;
using TWCF_BibleApp.Presentation.Views;

namespace TWCF_BibleApp.Presentation.ViewModels
{
    public class ReaderViewModel : System.ComponentModel.INotifyPropertyChanged
    {
        private readonly BibleService _bibleService;
        private readonly UserService _userService;

        private ObservableCollection<BookDto> _allBooks = new();
        private ObservableCollection<int> _allChapters = new();

        public ObservableCollection<BookDto> FilteredBooks { get; } = new();
        public ObservableCollection<int> FilteredChapters { get; } = new();
        public ObservableCollection<VerseDto> Verses { get; } = new();
        public ObservableCollection<TranslationDto> Translations { get; } = new();

        private TranslationDto? _selectedTranslation;
        public TranslationDto? SelectedTranslation
        {
            get => _selectedTranslation;
            set
            {
                if (value != null && _selectedTranslation?.Id != value.Id)
                {
                    _selectedTranslation = value;
                    OnPropertyChanged(nameof(SelectedTranslation));
                    _ = LoadChapterAsync();
                }
            }
        }

        private BookDto? _selectedBook;
        public BookDto? SelectedBook
        {
            get => _selectedBook;
            set
            {
                if (_selectedBook != value)
                {
                    _selectedBook = value;
                    OnPropertyChanged(nameof(SelectedBook));
                    if (_selectedBook != null)
                        _ = LoadChaptersAsync(_selectedBook.Id);
                }
            }
        }

        private int _selectedChapter;
        public int SelectedChapter
        {
            get => _selectedChapter;
            set
            {
                if (_selectedChapter != value)
                {
                    _selectedChapter = value;
                    OnPropertyChanged(nameof(SelectedChapter));

                    // Update search query to sync UI
                    ChapterSearchQuery = _selectedChapter.ToString();

                    _ = LoadChapterAsync();
                }
            }
        }

        private string _bookSearchQuery = string.Empty;
        public string BookSearchQuery
        {
            get => _bookSearchQuery;
            set
            {
                if (_bookSearchQuery != value)
                {
                    _bookSearchQuery = value;
                    OnPropertyChanged(nameof(BookSearchQuery));
                    FilterBooks();
                    IsBookDropdownVisible = FilteredBooks.Count > 0;
                }
            }
        }

        private string _chapterSearchQuery = string.Empty;
        public string ChapterSearchQuery
        {
            get => _chapterSearchQuery;
            set
            {
                if (_chapterSearchQuery != value)
                {
                    _chapterSearchQuery = value;
                    OnPropertyChanged(nameof(ChapterSearchQuery));
                    FilterChapters();
                    IsChapterDropdownVisible = FilteredChapters.Count > 0;
                }
            }
        }

        private bool _isBookDropdownVisible;
        public bool IsBookDropdownVisible
        {
            get => _isBookDropdownVisible;
            set { _isBookDropdownVisible = value; OnPropertyChanged(nameof(IsBookDropdownVisible)); }
        }

        private bool _isChapterDropdownVisible;
        public bool IsChapterDropdownVisible
        {
            get => _isChapterDropdownVisible;
            set { _isChapterDropdownVisible = value; OnPropertyChanged(nameof(IsChapterDropdownVisible)); }
        }

        private double _fontScale = 1.0;
        public double FontScale
        {
            get => _fontScale;
            set
            {
                if (Math.Abs(_fontScale - value) > 0.001)
                {
                    _fontScale = value;
                    OnPropertyChanged(nameof(FontScale));
                    _ = SaveSettingsAsync();
                }
            }
        }

        private bool _isDarkMode;
        public bool IsDarkMode
        {
            get => _isDarkMode;
            set
            {
                if (_isDarkMode != value)
                {
                    _isDarkMode = value;
                    OnPropertyChanged(nameof(IsDarkMode));
                    ApplyTheme(_isDarkMode);
                    _ = SaveSettingsAsync();
                }
            }
        }

        public ICommand IncreaseFontCommand { get; }
        public ICommand DecreaseFontCommand { get; }
        public ICommand GoToSearchCommand => new Command(async () => await Shell.Current.GoToAsync(nameof(SearchPage)));
        public IAsyncRelayCommand LoadChapterCommand { get; }
        public IAsyncRelayCommand NextChapterCommand { get; }
        public IAsyncRelayCommand PreviousChapterCommand { get; }

        public ReaderViewModel(BibleService bibleService, UserService userService)
        {
            _bibleService = bibleService ?? throw new ArgumentNullException(nameof(bibleService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));

            IncreaseFontCommand = new RelayCommand(() => FontScale = Math.Min(4.0, FontScale + 0.1));
            DecreaseFontCommand = new RelayCommand(() => FontScale = Math.Max(0.4, FontScale - 0.1));
            LoadChapterCommand = new AsyncRelayCommand(LoadChapterAsync);
            NextChapterCommand = new AsyncRelayCommand(GoToNextChapterAsync);
            PreviousChapterCommand = new AsyncRelayCommand(GoToPreviousChapterAsync);
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));

        public async Task InitializeAsync()
        {
            var books = await _bibleService.GetBooksAsync();
            _allBooks.Clear();
            foreach (var b in books) _allBooks.Add(b);

            FilterBooks();

            var setting = await _userService.GetUserSettingAsync();

            var translations = await _bibleService.GetTranslationsAsync();
            Translations.Clear();
            foreach (var t in translations) Translations.Add(t);

            // Set the backing field so the chapter is loaded once, below, not per property change.
            _selectedTranslation = Translations.FirstOrDefault(t => t.Id == setting?.LastTranslationId) ?? Translations.FirstOrDefault();
            OnPropertyChanged(nameof(SelectedTranslation));

            if (setting != null)
            {
                FontScale = setting.FontScale;
                SelectedBook = _allBooks.FirstOrDefault(x => x.Id == setting.LastBookId) ?? _allBooks.FirstOrDefault();
                SelectedChapter = setting.LastChapterNumber > 0 ? setting.LastChapterNumber : 1;
                IsDarkMode = setting.Theme == Domain.Enums.ThemePreference.Dark;
            }
            else
            {
                SelectedBook = _allBooks.FirstOrDefault();
                SelectedChapter = 1;
            }

            ApplyTheme(IsDarkMode);

            if (SelectedBook != null)
            {
                await LoadChaptersAsync(SelectedBook.Id);
                await LoadChapterAsync();
            }
        }

        private void FilterBooks()
        {
            FilteredBooks.Clear();
            foreach (var book in _allBooks.Where(b => b.Name.Contains(BookSearchQuery ?? string.Empty, StringComparison.OrdinalIgnoreCase)))
                FilteredBooks.Add(book);
        }

        private void FilterChapters()
        {
            FilteredChapters.Clear();
            foreach (var c in _allChapters.Where(c => c.ToString().Contains(ChapterSearchQuery ?? string.Empty)))
                FilteredChapters.Add(c);
        }

        public async Task LoadChaptersAsync(int bookId)
        {
            _allChapters.Clear();
            var chapters = await _bibleService.GetChaptersForBookAsync(bookId);
            foreach (var c in chapters) _allChapters.Add(c.Number);

            FilterChapters();
        }

        public async Task LoadChapterAsync()
        {
            if (SelectedBook == null || SelectedTranslation == null) return;
            var verses = await _bibleService.GetChapterVersesAsync(SelectedBook.Id, SelectedChapter, SelectedTranslation.Id);
            Verses.Clear();
            foreach (var v in verses) Verses.Add(v);

            await SaveSettingsAsync();
        }

        private async Task SaveSettingsAsync()
        {
            try
            {
                var setting = new UserSetting(0, SelectedBook?.Id ?? 0, SelectedChapter, FontScale,
                    IsDarkMode ? Domain.Enums.ThemePreference.Dark : Domain.Enums.ThemePreference.Light,
                    SelectedTranslation?.Id ?? 1);
                await _userService.SaveUserSettingAsync(setting);
            }
            catch { }
        }

        private void ApplyTheme(bool dark)
        {
            try
            {
                Microsoft.Maui.Controls.Application.Current.UserAppTheme = dark ? AppTheme.Dark : AppTheme.Light;

                var resources = Microsoft.Maui.Controls.Application.Current.Resources;
                if (resources.ContainsKey(dark ? "DarkBackground" : "LightBackground"))
                    resources["PageBackgroundColor"] = resources[dark ? "DarkBackground" : "LightBackground"];
                if (resources.ContainsKey(dark ? "DarkText" : "LightText"))
                    resources["PageTextColor"] = resources[dark ? "DarkText" : "LightText"];
            }
            catch { }
        }

        private async Task GoToNextChapterAsync()
        {
            if (SelectedBook == null || _allChapters.Count == 0) return;

            int index = _allChapters.IndexOf(SelectedChapter);
            if (index < _allChapters.Count - 1)
            {
                SelectedChapter = _allChapters[index + 1];
            }
            else
            {
                // Move to next book if exists
                int bookIndex = _allBooks.IndexOf(SelectedBook);
                if (bookIndex < _allBooks.Count - 1)
                {
                    SelectedBook = _allBooks[bookIndex + 1];
                    await LoadChaptersAsync(SelectedBook.Id);
                    SelectedChapter = _allChapters.FirstOrDefault();
                }
            }
        }

        private async Task GoToPreviousChapterAsync()
        {
            if (SelectedBook == null || _allChapters.Count == 0) return;

            int index = _allChapters.IndexOf(SelectedChapter);
            if (index > 0)
            {
                SelectedChapter = _allChapters[index - 1];
            }
            else
            {
                int bookIndex = _allBooks.IndexOf(SelectedBook);
                if (bookIndex > 0)
                {
                    SelectedBook = _allBooks[bookIndex - 1];
                    await LoadChaptersAsync(SelectedBook.Id);
                    SelectedChapter = _allChapters.LastOrDefault();
                }
            }
        }

        // MANUAL SELECTION METHODS
        public void SelectBookFromDropdown(BookDto book)
        {
            if (book == null) return;

            // Always set the SelectedBook to the instance from _allBooks
            var actualBook = _allBooks.FirstOrDefault(b => b.Id == book.Id);
            if (actualBook != null)
            {
                SelectedBook = actualBook;      // triggers LoadChaptersAsync
                BookSearchQuery = actualBook.Name;
                HideBookDropdown();
            }
        }

        public void SelectChapterFromDropdown(int chapter)
        {
            SelectedChapter = chapter;
            ChapterSearchQuery = chapter.ToString();
            HideChapterDropdown();
        }

        // ENTER KEY COMMANDS
        public async Task ConfirmBookSelectionAsync()
        {
            if (!string.IsNullOrWhiteSpace(BookSearchQuery))
            {
                var book = _allBooks.FirstOrDefault(b => b.Name.Equals(BookSearchQuery, StringComparison.OrdinalIgnoreCase));
                if (book != null) SelectedBook = book;
                await LoadChaptersAsync(SelectedBook?.Id ?? 0);
                SelectedChapter = FilteredChapters.FirstOrDefault();
                await LoadChapterAsync();
                HideBookDropdown();
            }
        }

        public async Task ConfirmChapterSelectionAsync()
        {
            if (int.TryParse(ChapterSearchQuery, out int chapter) && FilteredChapters.Contains(chapter))
            {
                SelectedChapter = chapter;
                await LoadChapterAsync();
                HideChapterDropdown();
            }
        }

        // Show all items on focus
        public void ShowBookDropdown() => IsBookDropdownVisible = true;
        public void ShowChapterDropdown() => IsChapterDropdownVisible = true;

        // Hide dropdown after selection
        public void HideBookDropdown() => IsBookDropdownVisible = false;
        public void HideChapterDropdown() => IsChapterDropdownVisible = false;
    }
}