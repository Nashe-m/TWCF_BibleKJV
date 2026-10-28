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

namespace TWCF_BibleApp.Presentation.ViewModels
{
    public class ReaderViewModel : System.ComponentModel.INotifyPropertyChanged, IQueryAttributable
    {
        // Reading font size in points = BaseFontSize * FontScale (FontScale is what UserSetting stores).
        private const double BaseFontSize = 14.0;
        private const double MinFontSize = 16.0;
        private const double MaxFontSize = 144.0;
        private const double DefaultFontSize = 28.0;
        private const int SuggestionCount = 8;

        // Page widths (device-independent units) below which the reader rearranges itself.
        private const double CompactWidth = 1100; // book list becomes a drawer; display controls fold into "Aa"
        private const double NarrowWidth = 720;   // phone: search moves to its own row

        private readonly BibleService _bibleService;
        private readonly UserService _userService;

        // BibleDataContext and UserDataContext are shared, so never run two queries at once.
        private readonly SemaphoreSlim _loadLock = new(1, 1);
        private CancellationTokenSource? _saveDebounce;
        private CancellationTokenSource? _suggestDebounce;
        private CancellationTokenSource? _statusTimer;
        private List<Note> _chapterNotes = new();
        private bool _initialized;
        private (int BookId, int Chapter, IReadOnlyList<VerseRange> Verses)? _pendingNavigation;
        private int? _pendingTranslationId;

        private readonly List<BookNavItem> _books = new();
        private BookNavItem? _currentBook;

        public ObservableCollection<BookNavItem> OldTestamentBooks { get; } = new();
        public ObservableCollection<BookNavItem> NewTestamentBooks { get; } = new();
        public ObservableCollection<VerseItem> Verses { get; } = new();
        public ObservableCollection<TranslationDto> Translations { get; } = new();
        public ObservableCollection<SearchSuggestion> Suggestions { get; } = new();

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
            SelectChapterCommand = new AsyncRelayCommand<ChapterNavItem>(SelectChapterAsync);
            SubmitSearchCommand = new AsyncRelayCommand(SubmitSearchAsync);
            IncreaseFontCommand = new RelayCommand(() => SetFontSize(ReadingFontSize < 40 ? ReadingFontSize + 2 : ReadingFontSize * 1.1));
            DecreaseFontCommand = new RelayCommand(() => SetFontSize(ReadingFontSize <= 40 ? ReadingFontSize - 2 : ReadingFontSize / 1.1));
            ResetFontCommand = new RelayCommand(() => SetFontSize(DefaultFontSize));
            ToggleSidebarCommand = new RelayCommand(() => IsSidebarOpen = !IsSidebarOpen);
            ToggleProjectorCommand = new RelayCommand(() => IsProjectorMode = !IsProjectorMode);
            ExitProjectorCommand = new RelayCommand(() => IsProjectorMode = false);
            ToggleThemeCommand = new RelayCommand(() => IsDarkMode = !IsDarkMode);
            ToggleWordsOfJesusCommand = new RelayCommand(() => ShowWordsOfJesus = !ShowWordsOfJesus);
            OpenSuggestionCommand = new AsyncRelayCommand<SearchSuggestion>(OpenSuggestionAsync);
            SeeAllResultsCommand = new AsyncRelayCommand(() => OpenSearchPageAsync(SearchText.Trim()));
            ToggleDisplayMenuCommand = new RelayCommand(() => IsDisplayMenuOpen = !IsDisplayMenuOpen);
            HighlightSelectionCommand = new AsyncRelayCommand<HighlightSwatch>(s => SetSelectionHighlightAsync(s?.Hex));
            RemoveHighlightCommand = new AsyncRelayCommand(() => SetSelectionHighlightAsync(null));
            ClearSelectionCommand = new RelayCommand(ClearSelection);
            CopySelectionCommand = new AsyncRelayCommand(CopySelectionAsync);
            ShareSelectionCommand = new AsyncRelayCommand(ShareSelectionAsync);
            PresentSelectionCommand = new RelayCommand(PresentSelection);
            OpenNoteEditorCommand = new RelayCommand(OpenNoteEditorForSelection);
            OpenNoteCommand = new RelayCommand<VerseItem>(OpenNoteEditorForVerse);
            SaveNoteCommand = new AsyncRelayCommand(SaveNoteAsync);
            DeleteNoteCommand = new AsyncRelayCommand(DeleteNoteAsync);
            CloseNoteEditorCommand = new RelayCommand(CloseNoteEditor);
            CloseOverlaysCommand = new RelayCommand(() => CloseOverlays());
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
        public IAsyncRelayCommand<SearchSuggestion> OpenSuggestionCommand { get; }
        public IAsyncRelayCommand SeeAllResultsCommand { get; }
        public IRelayCommand ToggleDisplayMenuCommand { get; }
        public IAsyncRelayCommand<HighlightSwatch> HighlightSelectionCommand { get; }
        public IAsyncRelayCommand RemoveHighlightCommand { get; }
        public IRelayCommand ClearSelectionCommand { get; }
        public IAsyncRelayCommand CopySelectionCommand { get; }
        public IAsyncRelayCommand ShareSelectionCommand { get; }
        public IRelayCommand PresentSelectionCommand { get; }
        public IRelayCommand OpenNoteEditorCommand { get; }
        public IRelayCommand<VerseItem> OpenNoteCommand { get; }
        public IAsyncRelayCommand SaveNoteCommand { get; }
        public IAsyncRelayCommand DeleteNoteCommand { get; }
        public IRelayCommand CloseNoteEditorCommand { get; }
        public IRelayCommand CloseOverlaysCommand { get; }

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
        public string TestamentLabel => _currentBook?.Book.Testament == Domain.Enums.Testament.New ? "NEW TESTAMENT" : "OLD TESTAMENT";

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
                OnPropertyChanged(nameof(NoteMarkerFontSize));
                OnPropertyChanged(nameof(NoteMarkerSize));
                OnPropertyChanged(nameof(ChapterTitleFontSize));
                OnPropertyChanged(nameof(VerseSpacing));
                OnPropertyChanged(nameof(FontSizeLabel));
                OnPropertyChanged(nameof(ReadingColumnWidth));
            }
        }

        public double VerseNumberFontSize => Math.Max(11, ReadingFontSize * 0.55);
        // The note icon beside a verse grows with the text so it stays easy to hit.
        public double NoteMarkerFontSize => Math.Max(16, ReadingFontSize * 0.5);
        public double NoteMarkerSize => Math.Max(40, NoteMarkerFontSize * 2);
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
                ApplyAnnotations();
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
                if (!SetField(ref _isSidebarOpen, value)) return;
                OnPropertyChanged(nameof(IsSidebarVisible));
                OnPropertyChanged(nameof(IsDrawerOpen));
            }
        }

        private bool _isProjectorMode;
        public bool IsProjectorMode
        {
            get => _isProjectorMode;
            set
            {
                if (!SetField(ref _isProjectorMode, value)) return;
                if (value)
                {
                    CloseSuggestions();
                    IsDisplayMenuOpen = false;
                    CloseNoteEditor();
                    ClearSelection();
                }
                ApplyAnnotations();
                OnPropertyChanged(nameof(IsActionBarVisible));
                OnPropertyChanged(nameof(IsChromeVisible));
                OnPropertyChanged(nameof(IsSidebarVisible));
                OnPropertyChanged(nameof(IsDrawerOpen));
                OnPropertyChanged(nameof(ReadingColumnWidth));
                OnPropertyChanged(nameof(ReadingPadding));
                ProjectorModeChanged?.Invoke(this, value);
            }
        }

        public bool IsChromeVisible => !IsProjectorMode;
        public bool IsSidebarVisible => IsSidebarOpen && !IsProjectorMode;
        public Thickness ReadingPadding =>
            IsProjectorMode ? new Thickness(72, 48, 72, 96)
            : IsNarrow ? new Thickness(20, 24, 20, 48)
            : new Thickness(48, 40, 48, 64);

        // ---- Layout (ReaderPage reports its width) -------------------------------------------

        private bool _layoutKnown;

        private bool _isCompact;
        public bool IsCompact { get => _isCompact; private set => SetField(ref _isCompact, value); }

        private bool _isNarrow;
        public bool IsNarrow { get => _isNarrow; private set => SetField(ref _isNarrow, value); }

        // On a compact screen the book list slides over the text instead of sitting beside it.
        public bool IsDrawerOpen => IsCompact && IsSidebarVisible;
        public bool IsInlineDisplayControlsVisible => !IsCompact;
        // Chapter arrows and Present; on a phone the arrows give way and Present moves into "Aa".
        public bool IsFullToolbar => !IsNarrow;
        public double VersionPickerWidth => IsNarrow ? 96 : IsCompact ? 170 : 240;

        private bool _isDisplayMenuOpen;
        public bool IsDisplayMenuOpen { get => _isDisplayMenuOpen; set => SetField(ref _isDisplayMenuOpen, value); }

        public void UpdateLayout(double width)
        {
            if (width <= 0) return;

            IsNarrow = width < NarrowWidth;
            var compact = width < CompactWidth;
            if (compact != IsCompact || !_layoutKnown)
            {
                _layoutKnown = true;
                IsCompact = compact;
                // Docked on a wide screen; a drawer starts closed so the text gets the room.
                IsSidebarOpen = !compact;
                if (!compact) IsDisplayMenuOpen = false;
            }

            OnPropertyChanged(nameof(IsDrawerOpen));
            OnPropertyChanged(nameof(IsInlineDisplayControlsVisible));
            OnPropertyChanged(nameof(IsFullToolbar));
            OnPropertyChanged(nameof(VersionPickerWidth));
            OnPropertyChanged(nameof(ReadingPadding));
        }

        /// <summary>Closes the drawer and the display menu; returns false if neither was open.</summary>
        public bool CloseOverlays()
        {
            var wasOpen = IsDrawerOpen || IsDisplayMenuOpen;
            if (IsDrawerOpen) IsSidebarOpen = false;
            IsDisplayMenuOpen = false;
            return wasOpen;
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetField(ref _searchText, value ?? string.Empty))
                    QueueSuggestions();
            }
        }

        // ---- Search box dropdown -----------------------------------------------------------

        private bool _isSuggestionsOpen;
        public bool IsSuggestionsOpen { get => _isSuggestionsOpen; private set => SetField(ref _isSuggestionsOpen, value); }

        private int _suggestionTotal;
        public bool HasNoSuggestions => Suggestions.Count == 0;
        public bool HasMoreResults => _suggestionTotal > 0;
        // No count here: the dropdown also matches the word being typed as a prefix ("one" finds
        // "ones"), so its total can differ from the search page's whole-word results.
        public string SeeAllLabel => $"See all verses containing \u201c{SearchText.Trim()}\u201d  \u2192";

        // ---- Loading -----------------------------------------------------------------------

        public async Task OnAppearingAsync()
        {
            if (!_initialized)
                await InitializeAsync();

            // A search result opens in the version that was searched.
            if (_pendingTranslationId is int translationId && Translations.FirstOrDefault(t => t.Id == translationId) is { } translation
                && translation.Id != SelectedTranslation?.Id)
            {
                _selectedTranslation = translation;   // not the setter: it would load the old chapter first
                OnPropertyChanged(nameof(SelectedTranslation));
                OnPropertyChanged(nameof(HasWordsOfJesus));
                _pendingNavigation ??= (CurrentBookId, CurrentChapter, Array.Empty<VerseRange>());
            }
            _pendingTranslationId = null;

            if (_pendingNavigation is { } nav)
            {
                _pendingNavigation = null;
                await NavigateToAsync(nav.BookId, nav.Chapter, nav.Verses);
            }
            else
            {
                // Notes and highlights may have been edited on their own pages meanwhile.
                await RefreshAnnotationsAsync();
            }
        }

        // Shell passes ?bookId=&chapter=&verse=[&to=][&t=] when a search result, note or highlight is opened.
        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            // Applied by OnAppearingAsync, which Shell calls right after this.
            _pendingTranslationId = TryGetInt(query, "t", out var t) ? t : null;
            if (!TryGetInt(query, "bookId", out var bookId) || !TryGetInt(query, "chapter", out var chapter)) return;

            IReadOnlyList<VerseRange> verses = Array.Empty<VerseRange>();
            if (TryGetInt(query, "verse", out var verse))
                verses = new[] { new VerseRange(verse, TryGetInt(query, "to", out var to) && to > verse ? to : verse) };
            _pendingNavigation = (bookId, chapter, verses);
        }

        private async Task RefreshAnnotationsAsync()
        {
            if (!_initialized || _currentBook == null) return;

            await _loadLock.WaitAsync();
            try
            {
                AttachHighlights(await _userService.GetHighlightsAsync(CurrentBookId, CurrentChapter));
                AttachNotes(await _userService.GetNotesAsync(CurrentBookId, CurrentChapter));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Refreshing highlights/notes failed: {ex}");
            }
            finally
            {
                _loadLock.Release();
            }
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
                (book.Testament == Domain.Enums.Testament.New ? NewTestamentBooks : OldTestamentBooks).Add(item);
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
        public Task NavigateToAsync(int bookId, int chapter, int? verse = null) =>
            NavigateToAsync(bookId, chapter, verse is int v ? new[] { new VerseRange(v, v) } : Array.Empty<VerseRange>());

        /// <summary>Shows a chapter, scrolling to the first of the given verses and marking them all.</summary>
        public async Task NavigateToAsync(int bookId, int chapter, IReadOnlyList<VerseRange> verses)
        {
            if (!_initialized || SelectedTranslation == null) return;
            CloseNoteEditor();

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

                var chapterVerses = await _bibleService.GetChapterVersesAsync(bookId, chapter, SelectedTranslation.Id);
                Verses.Clear();
                var targetIndex = -1;
                foreach (var v in chapterVerses)
                {
                    var item = new VerseItem(v) { IsTarget = verses.Any(r => r.Contains(v.VerseNumber)) };
                    if (item.IsTarget && targetIndex < 0) targetIndex = Verses.Count;
                    Verses.Add(item);
                }

                // A failure here (e.g. a damaged user database) should not stop the chapter showing.
                try
                {
                    AttachHighlights(await _userService.GetHighlightsAsync(bookId, chapter));
                    AttachNotes(await _userService.GetNotesAsync(bookId, chapter));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Loading highlights/notes failed: {ex}");
                }
                _selectionAnchor = null;
                OnSelectionChanged();

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

        private async Task SelectChapterAsync(ChapterNavItem? chapter)
        {
            if (chapter == null) return;
            await NavigateToAsync(CurrentBookId, chapter.Number);
            if (IsCompact) IsSidebarOpen = false;
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
        private static string ChapterWord(BookNavItem book) => ReferenceParser.ReferenceBookName(book.Book.Name);

        // ---- Search / jump -----------------------------------------------------------------

        private async Task SubmitSearchAsync()
        {
            var text = SearchText?.Trim();
            if (string.IsNullOrEmpty(text)) return;

            // "John 3", "Ps 23" and "1 Cor" jump straight there; a partial book name such as "son"
            // or "is" is searched for instead (the dropdown still offers the book).
            var reference = ReferenceParser.Parse(text, _books.Select(b => b.Book));
            if (reference is { IsExplicit: true })
            {
                await GoToReferenceAsync(reference);
                return;
            }

            await OpenSearchPageAsync(text);
        }

        private Task GoToReferenceAsync(BibleReference reference)
        {
            SearchText = string.Empty;
            return NavigateToAsync(reference.Book.Id, reference.Chapter, reference.Verses);
        }

        private Task OpenSuggestionAsync(SearchSuggestion? suggestion)
        {
            if (suggestion?.Reference is { } reference)
                return GoToReferenceAsync(reference);
            if (suggestion?.Result is not { } result)
                return Task.CompletedTask;

            SearchText = string.Empty;
            return NavigateToAsync(result.BookId, result.Chapter, result.Verse);
        }

        private async Task OpenSearchPageAsync(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            SearchText = string.Empty;
            // The Search tab (route "search" in AppShell.xaml) keeps its results when you come back.
            await Shell.Current.GoToAsync($"//search?q={Uri.EscapeDataString(text)}&t={SelectedTranslation?.Id ?? 1}");
        }

        public void CloseSuggestions()
        {
            _suggestDebounce?.Cancel();
            IsSuggestionsOpen = false;
        }

        /// <summary>Shows the dropdown again for text still in the box (e.g. when it regains focus).</summary>
        public void ReopenSuggestions() => QueueSuggestions();

        // Search as you type, once typing pauses.
        private void QueueSuggestions()
        {
            _suggestDebounce?.Cancel();
            var text = SearchText.Trim();
            if (!_initialized || IsProjectorMode || text.Length < 2)
            {
                IsSuggestionsOpen = false;
                return;
            }

            var cts = _suggestDebounce = new CancellationTokenSource();
            _ = UpdateSuggestionsAsync(SearchText, cts.Token);
        }

        private async Task UpdateSuggestionsAsync(string text, CancellationToken token)
        {
            try
            {
                await Task.Delay(200, token);
            }
            catch (TaskCanceledException)
            {
                return;
            }

            var reference = ReferenceParser.Parse(text, _books.Select(b => b.Book));
            // "Jn 3:16" is clearly a reference, so don't also look for verses containing "jn 3 16".
            var isReferenceOnly = reference != null && text.Any(char.IsDigit);

            var results = SearchResultsDto.Empty;
            if (!isReferenceOnly)
            {
                await _loadLock.WaitAsync();
                try
                {
                    if (token.IsCancellationRequested) return;
                    results = await _bibleService.SearchVersesAsync(
                        new SearchRequest(text, SelectedTranslation?.Id ?? 1) { Take = SuggestionCount, AsYouType = true });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Search failed: {ex}");
                }
                finally
                {
                    _loadLock.Release();
                }
            }

            if (token.IsCancellationRequested) return;

            Suggestions.Clear();
            if (reference != null) Suggestions.Add(SearchSuggestion.GoTo(reference));
            foreach (var r in results.Items) Suggestions.Add(SearchSuggestion.Match(r));
            _suggestionTotal = results.TotalCount;
            OnPropertyChanged(nameof(HasNoSuggestions));
            OnPropertyChanged(nameof(HasMoreResults));
            OnPropertyChanged(nameof(SeeAllLabel));
            IsSuggestionsOpen = true;
        }

        // ---- Verse selection and actions -------------------------------------------------

        private VerseItem? _selectionAnchor;

        public IReadOnlyList<HighlightSwatch> HighlightSwatches => HighlightSwatch.All;

        private List<VerseItem> SelectedVerses => Verses.Where(v => v.IsSelected).ToList();
        public bool HasSelection => Verses.Any(v => v.IsSelected);
        public bool IsActionBarVisible => HasSelection && !IsProjectorMode && !IsNoteEditorOpen;
        public string SelectionLabel => HasSelection ? ReferenceFor(SelectedVerses.Select(v => v.Number)) : "";
        public bool SelectionHasHighlight => Verses.Any(v => v.IsSelected && v.HighlightHex != null);
        public string NoteActionLabel => FindNote(SelectedVerses.Select(v => v.Number)) != null ? "Edit note" : "Note";

        /// <summary>Tap toggles a verse; with extend (Shift) it selects everything from the last tapped verse.</summary>
        public void ToggleVerseSelection(VerseItem item, bool extend)
        {
            if (IsProjectorMode || !Verses.Contains(item)) return;

            // The jump marker has done its job, and would hide any highlight on the verse.
            foreach (var v in Verses) v.IsTarget = false;

            if (extend && _selectionAnchor != null && Verses.Contains(_selectionAnchor))
            {
                var a = Verses.IndexOf(_selectionAnchor);
                var b = Verses.IndexOf(item);
                for (var i = Math.Min(a, b); i <= Math.Max(a, b); i++) Verses[i].IsSelected = true;
            }
            else
            {
                item.IsSelected = !item.IsSelected;
                if (item.IsSelected) _selectionAnchor = item;
            }
            OnSelectionChanged();
        }

        public void ClearSelection()
        {
            foreach (var v in Verses) v.IsSelected = false;
            _selectionAnchor = null;
            OnSelectionChanged();
        }

        private void OnSelectionChanged()
        {
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(IsActionBarVisible));
            OnPropertyChanged(nameof(SelectionLabel));
            OnPropertyChanged(nameof(SelectionHasHighlight));
            OnPropertyChanged(nameof(NoteActionLabel));
        }

        // "John 3:16-18, 20"
        private string ReferenceFor(IEnumerable<int> verses) => _currentBook == null ? ""
            : new BibleReference(_currentBook.Book, CurrentChapter, VerseRange.FromVerses(verses), true).Display;

        private async Task SetSelectionHighlightAsync(string? colorHex)
        {
            var verses = SelectedVerses.Select(v => v.Number).ToList();
            if (verses.Count == 0) return;

            await _loadLock.WaitAsync();
            try
            {
                AttachHighlights(await _userService.SetHighlightAsync(CurrentBookId, CurrentChapter, verses, colorHex));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Saving highlight failed: {ex}");
                ShowStatus("Couldn't save the highlight");
            }
            finally
            {
                _loadLock.Release();
            }
            ClearSelection();
        }

        // Text as it is pasted elsewhere: verse numbers only when there is more than one verse.
        private string SelectionText()
        {
            var verses = SelectedVerses;
            var body = string.Join(" ", verses.Select(v => verses.Count > 1 ? $"{v.Number} {v.Text}" : v.Text));
            return $"{body}\n{SelectionLabel} ({SelectedTranslation?.Code})";
        }

        private async Task CopySelectionAsync()
        {
            if (!HasSelection) return;
            var label = SelectionLabel;
            await Clipboard.Default.SetTextAsync(SelectionText());
            ClearSelection();
            ShowStatus($"Copied {label}");
        }

        private async Task ShareSelectionAsync()
        {
            if (!HasSelection) return;
            var request = new ShareTextRequest { Title = SelectionLabel, Text = SelectionText() };
            ClearSelection();
            await Share.Default.RequestAsync(request);
        }

        // Straight to the projector with the first selected verse in view.
        private void PresentSelection()
        {
            var first = Verses.IndexOf(Verses.FirstOrDefault(v => v.IsSelected)!);
            if (first < 0) return;
            IsProjectorMode = true;
            ScrollRequested?.Invoke(this, first);
        }

        // ---- Highlights and notes in the text ----------------------------------------------

        private void AttachHighlights(IEnumerable<Highlight> highlights)
        {
            var list = highlights.ToList();
            foreach (var v in Verses)
                v.HighlightHex = list.LastOrDefault(h => v.Number >= h.StartVerseNumber && v.Number <= h.EndVerseNumber)?.ColorHex;
            ApplyAnnotations();
            OnPropertyChanged(nameof(SelectionHasHighlight));
        }

        // A note shows on the first verse it covers.
        private void AttachNotes(IEnumerable<Note> notes)
        {
            _chapterNotes = notes.ToList();
            foreach (var v in Verses)
                v.Note = _chapterNotes.FirstOrDefault(n => n.StartVerseNumber == v.Number);
            ApplyAnnotations();
            OnPropertyChanged(nameof(NoteActionLabel));
        }

        // Highlights and note markers are hidden while projecting.
        private void ApplyAnnotations()
        {
            var show = !IsProjectorMode;
            var alpha = IsDarkMode ? 0.30f : 0.38f;
            foreach (var v in Verses)
            {
                v.HasHighlight = show && v.HighlightHex != null;
                v.HighlightColor = v.HasHighlight ? Color.FromArgb(v.HighlightHex!).WithAlpha(alpha) : Colors.Transparent;
                v.ShowsNote = show && v.Note != null;
            }
        }

        // The note for exactly these verses, if there is one.
        private Note? FindNote(IEnumerable<int> verses)
        {
            var list = verses.ToList();
            if (list.Count == 0) return null;
            var (start, end) = (list.Min(), list.Max());
            return _chapterNotes.FirstOrDefault(n => n.StartVerseNumber == start && (n.EndVerseNumber ?? n.StartVerseNumber) == end);
        }

        // ---- Note editor -------------------------------------------------------------------

        private Note? _editingNote;
        private (int Start, int End) _noteVerses;

        private bool _isNoteEditorOpen;
        public bool IsNoteEditorOpen
        {
            get => _isNoteEditorOpen;
            private set
            {
                if (SetField(ref _isNoteEditorOpen, value))
                    OnPropertyChanged(nameof(IsActionBarVisible));
            }
        }

        private string _noteText = "";
        public string NoteText { get => _noteText; set => SetField(ref _noteText, value ?? ""); }

        private string _noteReference = "";
        public string NoteReference { get => _noteReference; private set => SetField(ref _noteReference, value); }

        private string _notePreview = "";
        public string NotePreview { get => _notePreview; private set => SetField(ref _notePreview, value); }

        public bool IsEditingExistingNote => _editingNote != null;

        private void OpenNoteEditorForSelection()
        {
            var verses = SelectedVerses.Select(v => v.Number).ToList();
            if (verses.Count == 0) return;
            OpenNoteEditor(verses.Min(), verses.Max(), FindNote(verses));
        }

        private void OpenNoteEditorForVerse(VerseItem? item)
        {
            if (item?.Note is not { } note || IsProjectorMode) return;
            OpenNoteEditor(note.StartVerseNumber, note.EndVerseNumber ?? note.StartVerseNumber, note);
        }

        private void OpenNoteEditor(int start, int end, Note? existing)
        {
            _editingNote = existing;
            _noteVerses = (start, end);
            NoteText = existing?.Text ?? "";
            NoteReference = ReferenceFor(Enumerable.Range(start, end - start + 1));
            NotePreview = string.Join(" ", Verses.Where(v => v.Number >= start && v.Number <= end).Select(v => v.Text));
            OnPropertyChanged(nameof(IsEditingExistingNote));
            IsDisplayMenuOpen = false;
            IsNoteEditorOpen = true;
        }

        public void CloseNoteEditor()
        {
            if (!IsNoteEditorOpen) return;
            IsNoteEditorOpen = false;
            _editingNote = null;
        }

        private async Task SaveNoteAsync()
        {
            var text = NoteText.Trim();
            if (text.Length == 0)
            {
                // Saving an emptied note removes it.
                if (_editingNote != null) await DeleteNoteAsync();
                else CloseNoteEditor();
                return;
            }

            await _loadLock.WaitAsync();
            try
            {
                if (_editingNote != null)
                    await _userService.UpdateNoteAsync(new Note { Id = _editingNote.Id, Text = text, ModifiedUtc = DateTime.UtcNow });
                else
                    await _userService.AddNoteAsync(new Note(0, CurrentBookId, CurrentChapter, _noteVerses.Start,
                        _noteVerses.End == _noteVerses.Start ? null : _noteVerses.End, text));
                AttachNotes(await _userService.GetNotesAsync(CurrentBookId, CurrentChapter));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Saving note failed: {ex}");
                ShowStatus("Couldn't save the note");
                return;
            }
            finally
            {
                _loadLock.Release();
            }

            CloseNoteEditor();
            ClearSelection();
            ShowStatus($"Note saved on {NoteReference}");
        }

        private async Task DeleteNoteAsync()
        {
            if (_editingNote == null) return;

            await _loadLock.WaitAsync();
            try
            {
                await _userService.DeleteNoteAsync(_editingNote.Id);
                AttachNotes(await _userService.GetNotesAsync(CurrentBookId, CurrentChapter));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Deleting note failed: {ex}");
                ShowStatus("Couldn't delete the note");
                return;
            }
            finally
            {
                _loadLock.Release();
            }

            CloseNoteEditor();
            ClearSelection();
            ShowStatus("Note deleted");
        }

        // ---- Status message ("Copied John 3:16") -------------------------------------------

        private string _statusMessage = "";
        public string StatusMessage
        {
            get => _statusMessage;
            private set
            {
                if (SetField(ref _statusMessage, value))
                    OnPropertyChanged(nameof(HasStatus));
            }
        }

        public bool HasStatus => StatusMessage.Length > 0;

        private async void ShowStatus(string message)
        {
            _statusTimer?.Cancel();
            var cts = _statusTimer = new CancellationTokenSource();
            StatusMessage = message;
            try
            {
                await Task.Delay(2500, cts.Token);
                StatusMessage = "";
            }
            catch (TaskCanceledException)
            {
            }
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
