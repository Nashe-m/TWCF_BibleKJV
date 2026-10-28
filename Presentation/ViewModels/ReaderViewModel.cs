using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TWCF_BibleApp.Application.DTOs;
using TWCF_BibleApp.Application.Services;
using TWCF_BibleApp.Domain;
using TWCF_BibleApp.Presentation.Themes;
using TWCF_BibleApp.Presentation.Views;

namespace TWCF_BibleApp.Presentation.ViewModels
{
    public class ReaderViewModel : System.ComponentModel.INotifyPropertyChanged, IQueryAttributable
    {
        // Reading font size in points = BaseFontSize * FontScale (FontScale is what UserSetting stores).
        private const double BaseFontSize = 14.0;
        private const double MinFontSize = 16.0;
        private const double MaxFontSize = 144.0;
        private const double DefaultFontSize = 28.0;

        private readonly BibleService _bibleService;
        private readonly UserService _userService;

        // BibleDataContext and UserDataContext are shared, so never run two queries at once.
        private readonly SemaphoreSlim _loadLock = new(1, 1);
        private CancellationTokenSource? _saveDebounce;
        private bool _initialized;
        private (int BookId, int Chapter, int? Verse)? _pendingNavigation;

        private readonly List<BookNavItem> _books = new();
        private BookNavItem? _currentBook;

        public ObservableCollection<BookNavItem> OldTestamentBooks { get; } = new();
        public ObservableCollection<BookNavItem> NewTestamentBooks { get; } = new();
        public ObservableCollection<VerseItem> Verses { get; } = new();
        public ObservableCollection<TranslationDto> Translations { get; } = new();

        /// <summary>Raised after a chapter loads: the verse index to scroll to, or -1 for the top.</summary>
        public event EventHandler<int>? ScrollRequested;
        public event EventHandler<bool>? ProjectorModeChanged;

        public ReaderViewModel(BibleService bibleService, UserService userService)
        {
            _bibleService = bibleService ?? throw new ArgumentNullException(nameof(bibleService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));

            PreviousChapterCommand = new AsyncRelayCommand(GoToPreviousChapterAsync);
            NextChapterCommand = new AsyncRelayCommand(GoToNextChapterAsync);
            SelectBookCommand = new AsyncRelayCommand<BookNavItem>(SelectBookAsync);
            SelectChapterCommand = new AsyncRelayCommand<ChapterNavItem>(c => c == null ? Task.CompletedTask : NavigateToAsync(CurrentBookId, c.Number));
            SubmitSearchCommand = new AsyncRelayCommand(SubmitSearchAsync);
            IncreaseFontCommand = new RelayCommand(() => SetFontSize(ReadingFontSize < 40 ? ReadingFontSize + 2 : ReadingFontSize * 1.1));
            DecreaseFontCommand = new RelayCommand(() => SetFontSize(ReadingFontSize <= 40 ? ReadingFontSize - 2 : ReadingFontSize / 1.1));
            ResetFontCommand = new RelayCommand(() => SetFontSize(DefaultFontSize));
            ToggleSidebarCommand = new RelayCommand(() => IsSidebarOpen = !IsSidebarOpen);
            ToggleProjectorCommand = new RelayCommand(() => IsProjectorMode = !IsProjectorMode);
            ExitProjectorCommand = new RelayCommand(() => IsProjectorMode = false);
            ToggleThemeCommand = new RelayCommand(() => IsDarkMode = !IsDarkMode);
            ToggleWordsOfJesusCommand = new RelayCommand(() => ShowWordsOfJesus = !ShowWordsOfJesus);
        }

        public IAsyncRelayCommand PreviousChapterCommand { get; }
        public IAsyncRelayCommand NextChapterCommand { get; }
        public IAsyncRelayCommand<BookNavItem> SelectBookCommand { get; }
        public IAsyncRelayCommand<ChapterNavItem> SelectChapterCommand { get; }
        public IAsyncRelayCommand SubmitSearchCommand { get; }
        public IRelayCommand IncreaseFontCommand { get; }
        public IRelayCommand DecreaseFontCommand { get; }
        public IRelayCommand ResetFontCommand { get; }
        public IRelayCommand ToggleSidebarCommand { get; }
        public IRelayCommand ToggleProjectorCommand { get; }
        public IRelayCommand ExitProjectorCommand { get; }
        public IRelayCommand ToggleThemeCommand { get; }
        public IRelayCommand ToggleWordsOfJesusCommand { get; }

        // ---- Current location ------------------------------------------------------------

        private int CurrentBookId => _currentBook?.Book.Id ?? 0;
        public BookNavItem? CurrentBook => _currentBook;

        private int _currentChapter;
        public int CurrentChapter
        {
            get => _currentChapter;
            private set => SetField(ref _currentChapter, value);
        }

        public string LocationLabel => _currentBook == null ? "" : $"{_currentBook.DisplayName}  ›  {CurrentChapter}";
        public string ChapterTitle => _currentBook == null ? "" : $"{ChapterWord(_currentBook)} {CurrentChapter}";
        public string TestamentLabel => _currentBook?.Book.Testament == "NT" ? "NEW TESTAMENT" : "OLD TESTAMENT";

        private string _previousLabel = "";
        public string PreviousLabel { get => _previousLabel; private set => SetField(ref _previousLabel, value); }

        private string _nextLabel = "";
        public string NextLabel { get => _nextLabel; private set => SetField(ref _nextLabel, value); }

        private TranslationDto? _selectedTranslation;
        public TranslationDto? SelectedTranslation
        {
            get => _selectedTranslation;
            set
            {
                if (value == null || _selectedTranslation?.Id == value.Id) return;
                _selectedTranslation = value;
                OnPropertyChanged(nameof(SelectedTranslation));
                OnPropertyChanged(nameof(HasWordsOfJesus));
                if (_initialized)
                    _ = NavigateToAsync(CurrentBookId, CurrentChapter);
            }
        }

        // ---- Display settings ------------------------------------------------------------

        private double _readingFontSize = DefaultFontSize;
        public double ReadingFontSize
        {
            get => _readingFontSize;
            private set
            {
                if (!SetField(ref _readingFontSize, value)) return;
                OnPropertyChanged(nameof(VerseNumberFontSize));
                OnPropertyChanged(nameof(ChapterTitleFontSize));
                OnPropertyChanged(nameof(VerseSpacing));
                OnPropertyChanged(nameof(FontSizeLabel));
                OnPropertyChanged(nameof(ReadingColumnWidth));
            }
        }

        public double VerseNumberFontSize => Math.Max(11, ReadingFontSize * 0.55);
        public double ChapterTitleFontSize => Math.Clamp(ReadingFontSize * 1.6, 40, 140);
        public double VerseSpacing => ReadingFontSize * 0.5;
        public string FontSizeLabel => $"{ReadingFontSize:0} pt";

        // Keep lines a readable length on a desktop; use the whole screen when projecting.
        public double ReadingColumnWidth => IsProjectorMode ? double.PositiveInfinity : Math.Max(760, ReadingFontSize * 30);

        private bool _isDarkMode;
        public bool IsDarkMode
        {
            get => _isDarkMode;
            set
            {
                if (!SetField(ref _isDarkMode, value)) return;
                ThemeManager.Apply(value);
                OnPropertyChanged(nameof(ThemeGlyph));
                QueueSaveSettings();
            }
        }

        // Red letter: words of Jesus in colour. Only some versions (KJV, WEB) have the markup.
        private bool _showWordsOfJesus = true;
        public bool ShowWordsOfJesus
        {
            get => _showWordsOfJesus;
            set
            {
                if (SetField(ref _showWordsOfJesus, value))
                    QueueSaveSettings();
            }
        }

        public bool HasWordsOfJesus => SelectedTranslation?.HasWordsOfJesus == true;

        // Segoe Fluent Icons: moon to switch to dark, sun to switch to light.
        public string ThemeGlyph => IsDarkMode ? "" : "";

        private bool _isSidebarOpen = true;
        public bool IsSidebarOpen
        {
            get => _isSidebarOpen;
            set
            {
                if (SetField(ref _isSidebarOpen, value))
                    OnPropertyChanged(nameof(IsSidebarVisible));
            }
        }

        private bool _isProjectorMode;
        public bool IsProjectorMode
        {
            get => _isProjectorMode;
            set
            {
                if (!SetField(ref _isProjectorMode, value)) return;
                OnPropertyChanged(nameof(IsChromeVisible));
                OnPropertyChanged(nameof(IsSidebarVisible));
                OnPropertyChanged(nameof(ReadingColumnWidth));
                OnPropertyChanged(nameof(ReadingPadding));
                ProjectorModeChanged?.Invoke(this, value);
            }
        }

        public bool IsChromeVisible => !IsProjectorMode;
        public bool IsSidebarVisible => IsSidebarOpen && !IsProjectorMode;
        public Thickness ReadingPadding => IsProjectorMode ? new Thickness(72, 48, 72, 96) : new Thickness(48, 40, 48, 64);

        private string _searchText = string.Empty;
        public string SearchText { get => _searchText; set => SetField(ref _searchText, value); }

        // ---- Loading -----------------------------------------------------------------------

        public async Task OnAppearingAsync()
        {
            if (!_initialized)
                await InitializeAsync();

            if (_pendingNavigation is { } nav)
            {
                _pendingNavigation = null;
                await NavigateToAsync(nav.BookId, nav.Chapter, nav.Verse);
            }
        }

        // Shell passes ?bookId=&chapter=&verse= when a search result is opened.
        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            // Applied by OnAppearingAsync, which Shell calls right after this.
            if (TryGetInt(query, "bookId", out var bookId) && TryGetInt(query, "chapter", out var chapter))
                _pendingNavigation = (bookId, chapter, TryGetInt(query, "verse", out var verse) ? verse : null);
        }

        private async Task InitializeAsync()
        {
            var books = await _bibleService.GetBooksAsync();
            var setting = await _userService.GetUserSettingAsync();
            var translations = await _bibleService.GetTranslationsAsync();

            _books.Clear();
            OldTestamentBooks.Clear();
            NewTestamentBooks.Clear();
            foreach (var book in books.OrderBy(b => b.Order))
            {
                var item = new BookNavItem(book);
                _books.Add(item);
                (book.Testament == "NT" ? NewTestamentBooks : OldTestamentBooks).Add(item);
            }

            Translations.Clear();
            foreach (var t in translations) Translations.Add(t);
            _selectedTranslation = Translations.FirstOrDefault(t => t.Id == setting?.LastTranslationId) ?? Translations.FirstOrDefault();
            OnPropertyChanged(nameof(SelectedTranslation));
            OnPropertyChanged(nameof(HasWordsOfJesus));

            if (setting != null)
            {
                ShowWordsOfJesus = setting.ShowWordsOfJesus;
                ReadingFontSize = Math.Clamp(Math.Round(BaseFontSize * setting.FontScale), MinFontSize, MaxFontSize);
                _isDarkMode = setting.Theme == Domain.Enums.ThemePreference.Dark;
                OnPropertyChanged(nameof(IsDarkMode));
                OnPropertyChanged(nameof(ThemeGlyph));
            }
            ThemeManager.Apply(IsDarkMode);

            _initialized = true;

            var startBook = _books.FirstOrDefault(b => b.Book.Id == setting?.LastBookId) ?? _books.FirstOrDefault();
            if (startBook != null)
                await NavigateToAsync(startBook.Book.Id, Math.Max(1, setting?.LastChapterNumber ?? 1));
        }

        /// <summary>Shows a chapter, optionally scrolling to and marking a verse.</summary>
        public async Task NavigateToAsync(int bookId, int chapter, int? verse = null)
        {
            if (!_initialized || SelectedTranslation == null) return;

            await _loadLock.WaitAsync();
            try
            {
                var book = _books.FirstOrDefault(b => b.Book.Id == bookId);
                if (book == null) return;

                if (book != _currentBook || book.Chapters.Count == 0)
                {
                    var chapters = await _bibleService.GetChaptersForBookAsync(bookId);
                    book.Chapters.Clear();
                    foreach (var c in chapters) book.Chapters.Add(new ChapterNavItem(c.Number));

                    if (_currentBook != null && _currentBook != book)
                    {
                        _currentBook.IsSelected = false;
                        _currentBook.Chapters.Clear();
                    }
                    _currentBook = book;
                    book.IsSelected = true;
                }

                chapter = Math.Clamp(chapter, 1, Math.Max(1, book.Chapters.Count));
                CurrentChapter = chapter;
                foreach (var c in book.Chapters) c.IsCurrent = c.Number == chapter;

                var verses = await _bibleService.GetChapterVersesAsync(bookId, chapter, SelectedTranslation.Id);
                Verses.Clear();
                var targetIndex = -1;
                foreach (var v in verses)
                {
                    var item = new VerseItem(v) { IsTarget = v.VerseNumber == verse };
                    if (item.IsTarget) targetIndex = Verses.Count;
                    Verses.Add(item);
                }

                OnPropertyChanged(nameof(LocationLabel));
                OnPropertyChanged(nameof(ChapterTitle));
                OnPropertyChanged(nameof(TestamentLabel));
                UpdateNeighbourLabels();
                ScrollRequested?.Invoke(this, targetIndex);
            }
            finally
            {
                _loadLock.Release();
            }

            QueueSaveSettings();
        }

        private Task SelectBookAsync(BookNavItem? item)
        {
            if (item == null) return Task.CompletedTask;
            return item == _currentBook ? Task.CompletedTask : NavigateToAsync(item.Book.Id, 1);
        }

        private async Task GoToNextChapterAsync()
        {
            if (_currentBook == null) return;
            if (CurrentChapter < _currentBook.Chapters.Count)
            {
                await NavigateToAsync(CurrentBookId, CurrentChapter + 1);
                return;
            }

            var next = Neighbour(+1);
            if (next != null) await NavigateToAsync(next.Book.Id, 1);
        }

        private async Task GoToPreviousChapterAsync()
        {
            if (_currentBook == null) return;
            if (CurrentChapter > 1)
            {
                await NavigateToAsync(CurrentBookId, CurrentChapter - 1);
                return;
            }

            var previous = Neighbour(-1);
            if (previous != null) await NavigateToAsync(previous.Book.Id, int.MaxValue); // clamped to its last chapter
        }

        private BookNavItem? Neighbour(int offset)
        {
            var index = _currentBook == null ? -1 : _books.IndexOf(_currentBook) + offset;
            return index >= 0 && index < _books.Count ? _books[index] : null;
        }

        private void UpdateNeighbourLabels()
        {
            if (_currentBook == null) return;

            var previous = CurrentChapter > 1 ? $"{ChapterWord(_currentBook)} {CurrentChapter - 1}"
                : Neighbour(-1)?.DisplayName;
            var next = CurrentChapter < _currentBook.Chapters.Count ? $"{ChapterWord(_currentBook)} {CurrentChapter + 1}"
                : Neighbour(+1)?.DisplayName;

            PreviousLabel = previous == null ? "" : $"←  {previous}";
            NextLabel = next == null ? "" : $"{next}  →";
        }

        // "Psalms 23" reads as "Psalm 23".
        private static string ChapterWord(BookNavItem book) => book.Book.Name == "Psalms" ? "Psalm" : book.DisplayName;

        // ---- Search / jump -----------------------------------------------------------------

        private async Task SubmitSearchAsync()
        {
            var text = SearchText?.Trim();
            if (string.IsNullOrEmpty(text)) return;

            var reference = ReferenceParser.Parse(text, _books.Select(b => b.Book));
            if (reference != null)
            {
                SearchText = string.Empty;
                await NavigateToAsync(reference.Book.Id, reference.Chapter, reference.Verse);
                return;
            }

            await Shell.Current.GoToAsync($"{nameof(SearchPage)}?q={Uri.EscapeDataString(text)}");
        }

        // ---- Settings ----------------------------------------------------------------------

        private void SetFontSize(double size)
        {
            ReadingFontSize = Math.Round(Math.Clamp(size, MinFontSize, MaxFontSize));
            QueueSaveSettings();
        }

        // Font size and navigation change quickly; write settings once things settle.
        private void QueueSaveSettings()
        {
            if (!_initialized) return;

            _saveDebounce?.Cancel();
            var cts = _saveDebounce = new CancellationTokenSource();
            _ = Task.Delay(600, cts.Token).ContinueWith(async _ =>
            {
                await _loadLock.WaitAsync();
                try
                {
                    var setting = new UserSetting(0, CurrentBookId, CurrentChapter, ReadingFontSize / BaseFontSize,
                        IsDarkMode ? Domain.Enums.ThemePreference.Dark : Domain.Enums.ThemePreference.Light,
                        SelectedTranslation?.Id ?? 1, ShowWordsOfJesus);
                    await _userService.SaveUserSettingAsync(setting);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Saving settings failed: {ex}");
                }
                finally
                {
                    _loadLock.Release();
                }
            }, cts.Token, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
        }

        // ---- Helpers -----------------------------------------------------------------------

        private static bool TryGetInt(IDictionary<string, object> query, string key, out int value)
        {
            value = 0;
            return query.TryGetValue(key, out var raw) && int.TryParse(raw?.ToString(), out value);
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));

        private bool SetField<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(name);
            return true;
        }
    }
}
