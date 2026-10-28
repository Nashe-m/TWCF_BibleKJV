using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TWCF_BibleApp.Application.DTOs;
using TWCF_BibleApp.Application.Services;
using TWCF_BibleApp.Domain.Enums;

namespace TWCF_BibleApp.Presentation.ViewModels
{
    /// <summary>A book in the "matches per book" row; tapping it limits the results to that book.</summary>
    public sealed partial class BookMatchItem : ObservableObject
    {
        public BookMatchItem(BookDto book, int count)
        {
            Book = book;
            Count = count;
            Label = $"{ReferenceParser.DisplayBookName(book.Name)}  {count:N0}";
        }

        public BookDto Book { get; }
        public int Count { get; }
        public string Label { get; }

        [ObservableProperty]
        private bool _isSelected;
    }

    public class SearchViewModel : ObservableObject, IQueryAttributable
    {
        private const int PageSize = 50;
        private const int RecentCount = 8;
        private const string RecentKey = "RecentSearches";

        private readonly BibleService _bibleService;
        private readonly UserService _userService;
        private Dictionary<int, BookDto> _books = new();
        private List<BookMatchItem> _allBookMatches = new();
        private SearchRequest? _lastRequest;
        private int _totalCount;
        private bool _loaded;
        private bool _suspendSearch;

        public SearchViewModel(BibleService bibleService, UserService userService)
        {
            _bibleService = bibleService ?? throw new ArgumentNullException(nameof(bibleService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            PerformSearchCommand = new AsyncRelayCommand(PerformSearchAsync);
            LoadMoreCommand = new AsyncRelayCommand(LoadMoreAsync);
            OpenResultCommand = new AsyncRelayCommand<SearchResultDto>(OpenResultAsync);
            SetTestamentCommand = new RelayCommand<string>(t => Testament = t switch { "Old" => Domain.Enums.Testament.Old, "New" => Domain.Enums.Testament.New, _ => null });
            SetOrderCommand = new RelayCommand<string>(o => Order = o == "Best" ? SearchOrder.BestMatch : SearchOrder.Bible);
            SelectBookCommand = new RelayCommand<BookMatchItem>(b => SelectedBook = b == null || b.Book.Id == SelectedBook?.Book.Id ? null : b);
            UseRecentCommand = new AsyncRelayCommand<string>(async q => { SearchQuery = q ?? ""; await PerformSearchAsync(); });
            ClearRecentCommand = new RelayCommand(ClearRecent);
            LoadRecent();
        }

        public IAsyncRelayCommand PerformSearchCommand { get; }
        public IAsyncRelayCommand LoadMoreCommand { get; }
        public IAsyncRelayCommand<SearchResultDto> OpenResultCommand { get; }
        public IRelayCommand<string> SetTestamentCommand { get; }
        public IRelayCommand<string> SetOrderCommand { get; }
        public IRelayCommand<BookMatchItem> SelectBookCommand { get; }
        public IAsyncRelayCommand<string> UseRecentCommand { get; }
        public IRelayCommand ClearRecentCommand { get; }

        public ObservableCollection<SearchResultDto> SearchResults { get; } = new();
        public ObservableCollection<TranslationDto> Translations { get; } = new();
        public ObservableCollection<BookMatchItem> BookMatches { get; } = new();
        public ObservableCollection<string> RecentSearches { get; } = new();

        // ---- Query and filters (changing a filter re-runs the last search) ------------------

        private string _searchQuery = string.Empty;
        public string SearchQuery { get => _searchQuery; set => SetProperty(ref _searchQuery, value ?? ""); }

        private TranslationDto? _selectedTranslation;
        public TranslationDto? SelectedTranslation
        {
            get => _selectedTranslation;
            set
            {
                if (value == null || _selectedTranslation?.Id == value.Id) return;
                _selectedTranslation = value;
                OnPropertyChanged();
                RefreshSearch(resetBook: false);
            }
        }

        private Testament? _testament;
        public Testament? Testament
        {
            get => _testament;
            set
            {
                if (!SetProperty(ref _testament, value)) return;
                OnPropertyChanged(nameof(IsAllTestaments));
                OnPropertyChanged(nameof(IsOldTestament));
                OnPropertyChanged(nameof(IsNewTestament));
                // A chosen book from the other testament no longer applies.
                if (value != null && SelectedBook?.Book.Testament != value) _selectedBook = null;
                RefreshSearch(resetBook: false);
            }
        }

        public bool IsAllTestaments => Testament == null;
        public bool IsOldTestament => Testament == Domain.Enums.Testament.Old;
        public bool IsNewTestament => Testament == Domain.Enums.Testament.New;

        private BookMatchItem? _selectedBook;
        public BookMatchItem? SelectedBook
        {
            get => _selectedBook;
            set
            {
                if (!SetProperty(ref _selectedBook, value)) return;
                RefreshSearch(resetBook: false);
            }
        }

        private bool _wordForms;
        public bool WordForms
        {
            get => _wordForms;
            set
            {
                if (SetProperty(ref _wordForms, value)) RefreshSearch(resetBook: true);
            }
        }

        private SearchOrder _order = SearchOrder.Bible;
        public SearchOrder Order
        {
            get => _order;
            set
            {
                if (!SetProperty(ref _order, value)) return;
                OnPropertyChanged(nameof(IsBibleOrder));
                OnPropertyChanged(nameof(IsBestMatch));
                RefreshSearch(resetBook: false);
            }
        }

        public bool IsBibleOrder => Order == SearchOrder.Bible;
        public bool IsBestMatch => Order == SearchOrder.BestMatch;

        // ---- Results ------------------------------------------------------------------------

        private bool _isBusy;
        public bool IsBusy { get => _isBusy; private set => SetProperty(ref _isBusy, value); }

        public bool HasSearched => _lastRequest != null;
        public bool HasMore => SearchResults.Count < _totalCount;
        public bool HasRecent => RecentSearches.Count > 0 && !HasSearched;

        public string Summary
        {
            get
            {
                if (_lastRequest == null) return "";
                var where = SelectedBook != null ? $" in {ReferenceParser.DisplayBookName(SelectedBook.Book.Name)}"
                    : Testament == Domain.Enums.Testament.Old ? " in the Old Testament"
                    : Testament == Domain.Enums.Testament.New ? " in the New Testament" : "";
                return _totalCount == 0
                    ? $"No verses contain \u201c{_lastRequest.Query}\u201d{where}"
                    : $"{_totalCount:N0} {(_totalCount == 1 ? "verse" : "verses")} containing \u201c{_lastRequest.Query}\u201d{where}";
            }
        }

        public bool HasBookMatches => BookMatches.Count > 0;

        // ---- Loading ------------------------------------------------------------------------

        // The reader opens this page with ?q=<text>&t=<translation id>.
        public async void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            await EnsureLoadedAsync();

            _suspendSearch = true;
            if (query.TryGetValue("t", out var t) && int.TryParse(t?.ToString(), out var translationId))
                SelectedTranslation = Translations.FirstOrDefault(x => x.Id == translationId) ?? SelectedTranslation;
            _suspendSearch = false;

            if (query.TryGetValue("q", out var q) && q?.ToString() is { Length: > 0 } text)
            {
                SearchQuery = Uri.UnescapeDataString(text);
                // A new search from the reader starts without the last search's book.
                _selectedBook = null;
                await PerformSearchAsync();
            }
            query.Clear();
        }

        public async Task EnsureLoadedAsync()
        {
            if (_loaded) return;
            _loaded = true;

            _books = (await _bibleService.GetBooksAsync()).ToDictionary(b => b.Id);
            foreach (var tr in await _bibleService.GetTranslationsAsync()) Translations.Add(tr);

            // Start in the version the reader is showing.
            var current = (await _userService.GetUserSettingAsync())?.LastTranslationId ?? 1;
            _selectedTranslation = Translations.FirstOrDefault(x => x.Id == current) ?? Translations.FirstOrDefault();
            OnPropertyChanged(nameof(SelectedTranslation));
        }

        private SearchRequest BuildRequest(string query) => new(query, SelectedTranslation?.Id ?? 1)
        {
            Scope = new SearchScope(SelectedBook == null ? Testament : null, SelectedBook?.Book.Id),
            Take = PageSize,
            WordForms = WordForms,
            Order = Order,
        };

        private void RefreshSearch(bool resetBook)
        {
            if (resetBook) _selectedBook = null;
            if (_suspendSearch || _lastRequest == null) return;
            _ = RunSearchAsync(_lastRequest.Query, recountBooks: resetBook || _lastRequest.TranslationId != SelectedTranslation?.Id);
        }

        private async Task PerformSearchAsync()
        {
            var text = SearchQuery.Trim();
            if (text.Length == 0) return;
            await EnsureLoadedAsync();
            AddRecent(text);
            _selectedBook = null;
            await RunSearchAsync(text, recountBooks: true);
        }

        private async Task RunSearchAsync(string text, bool recountBooks)
        {
            var request = BuildRequest(text);
            IsBusy = true;
            try
            {
                var results = await _bibleService.SearchVersesAsync(request);
                if (recountBooks)
                {
                    var counts = await _bibleService.CountMatchesByBookAsync(request);
                    _allBookMatches = counts.Where(c => _books.ContainsKey(c.BookId))
                        .Select(c => new BookMatchItem(_books[c.BookId], c.Count)).ToList();
                }

                _lastRequest = request;
                _totalCount = results.TotalCount;
                SearchResults.Clear();
                foreach (var r in results.Items) SearchResults.Add(r);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Search failed: {ex}");
            }
            finally
            {
                IsBusy = false;
                RebuildBookMatches();
                OnPropertyChanged(nameof(SelectedBook));
                OnPropertyChanged(nameof(Summary));
                OnPropertyChanged(nameof(HasMore));
                OnPropertyChanged(nameof(HasSearched));
                OnPropertyChanged(nameof(HasRecent));
                ScrollToTopRequested?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>Raised when a new set of results replaces the old one.</summary>
        public event EventHandler? ScrollToTopRequested;

        private async Task LoadMoreAsync()
        {
            if (!HasMore || IsBusy || _lastRequest == null) return;

            IsBusy = true;
            try
            {
                var results = await _bibleService.SearchVersesAsync(_lastRequest with { Skip = SearchResults.Count });
                foreach (var r in results.Items) SearchResults.Add(r);
            }
            finally
            {
                IsBusy = false;
                OnPropertyChanged(nameof(HasMore));
            }
        }

        // Matches per book, limited to the chosen testament.
        private void RebuildBookMatches()
        {
            var shown = _allBookMatches.Where(b => Testament == null || b.Book.Testament == Testament).ToList();

            BookMatches.Clear();
            foreach (var b in shown)
            {
                b.IsSelected = b.Book.Id == SelectedBook?.Book.Id;
                BookMatches.Add(b);
            }
            if (SelectedBook != null) _selectedBook = shown.FirstOrDefault(b => b.Book.Id == SelectedBook.Book.Id) ?? SelectedBook;

            OnPropertyChanged(nameof(HasBookMatches));
        }

        // Opens the verse in the reader, in the version that was searched.
        private async Task OpenResultAsync(SearchResultDto? result)
        {
            if (result == null) return;
            await Shell.Current.GoToAsync(
                $"//ReaderPage?bookId={result.BookId}&chapter={result.Chapter}&verse={result.Verse}&t={_lastRequest?.TranslationId ?? SelectedTranslation?.Id ?? 1}");
        }

        // ---- Recent searches (kept on this device) --------------------------------------------

        private void LoadRecent()
        {
            RecentSearches.Clear();
            foreach (var q in Preferences.Default.Get(RecentKey, "").Split('\n', StringSplitOptions.RemoveEmptyEntries))
                RecentSearches.Add(q);
        }

        private void AddRecent(string query)
        {
            var list = new[] { query }.Concat(RecentSearches.Where(q => !string.Equals(q, query, StringComparison.CurrentCultureIgnoreCase)))
                .Take(RecentCount).ToList();
            Preferences.Default.Set(RecentKey, string.Join("\n", list));
            LoadRecent();
            OnPropertyChanged(nameof(HasRecent));
        }

        private void ClearRecent()
        {
            Preferences.Default.Remove(RecentKey);
            RecentSearches.Clear();
            OnPropertyChanged(nameof(HasRecent));
        }
    }
}
