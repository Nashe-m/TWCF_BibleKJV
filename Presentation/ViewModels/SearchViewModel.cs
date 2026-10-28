using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using TWCF_BibleApp.Application.DTOs;
using TWCF_BibleApp.Application.Services;

namespace TWCF_BibleApp.Presentation.ViewModels
{
    public class SearchViewModel : INotifyPropertyChanged, IQueryAttributable
    {
        private const int PageSize = 50;

        private readonly BibleService _bibleService;
        private readonly UserService _userService;
        private int? _translationId;
        private string _lastQuery = string.Empty;
        private int _totalCount;

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public SearchViewModel(BibleService bibleService, UserService userService)
        {
            _bibleService = bibleService ?? throw new ArgumentNullException(nameof(bibleService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            PerformSearchCommand = new AsyncRelayCommand(PerformSearchAsync);
            LoadMoreCommand = new AsyncRelayCommand(LoadMoreAsync);
            OpenResultCommand = new AsyncRelayCommand<SearchResultDto>(OpenResultAsync);
        }

        public IAsyncRelayCommand PerformSearchCommand { get; }
        public IAsyncRelayCommand LoadMoreCommand { get; }
        public IAsyncRelayCommand<SearchResultDto> OpenResultCommand { get; }

        public ObservableCollection<SearchResultDto> SearchResults { get; } = new();

        private string _searchQuery = string.Empty;
        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                if (_searchQuery == value) return;
                _searchQuery = value;
                OnPropertyChanged();
            }
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (_isBusy == value) return;
                _isBusy = value;
                OnPropertyChanged();
            }
        }

        public bool HasMore => SearchResults.Count < _totalCount;

        public string Summary => _lastQuery.Length == 0 ? ""
            : _totalCount == 0 ? $"No verses contain “{_lastQuery}”"
            : $"{_totalCount:N0} {(_totalCount == 1 ? "verse" : "verses")} containing “{_lastQuery}”";

        // The reader opens this page with ?q=<text>&t=<translation id>.
        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("t", out var t) && int.TryParse(t?.ToString(), out var translationId))
                _translationId = translationId;

            if (query.TryGetValue("q", out var q) && q?.ToString() is { Length: > 0 } text)
            {
                SearchQuery = Uri.UnescapeDataString(text);
                PerformSearchCommand.Execute(null);
            }
        }

        private async Task PerformSearchAsync()
        {
            var text = SearchQuery.Trim();
            if (text.Length == 0) return;

            // Search the version the reader is showing.
            _translationId ??= (await _userService.GetUserSettingAsync())?.LastTranslationId ?? 1;

            IsBusy = true;
            try
            {
                var results = await _bibleService.SearchVersesAsync(text, _translationId.Value, SearchScope.All, 0, PageSize);
                _lastQuery = text;
                _totalCount = results.TotalCount;
                SearchResults.Clear();
                foreach (var r in results.Items) SearchResults.Add(r);
            }
            finally
            {
                IsBusy = false;
                OnPropertyChanged(nameof(Summary));
                OnPropertyChanged(nameof(HasMore));
            }
        }

        private async Task LoadMoreAsync()
        {
            if (!HasMore || IsBusy || _translationId == null) return;

            IsBusy = true;
            try
            {
                var results = await _bibleService.SearchVersesAsync(_lastQuery, _translationId.Value, SearchScope.All, SearchResults.Count, PageSize);
                foreach (var r in results.Items) SearchResults.Add(r);
            }
            finally
            {
                IsBusy = false;
                OnPropertyChanged(nameof(HasMore));
            }
        }

        private async Task OpenResultAsync(SearchResultDto? result)
        {
            if (result == null) return;
            await Shell.Current.GoToAsync(
                $"//ReaderPage?bookId={result.BookId}&chapter={result.Chapter}&verse={result.Verse}");
        }
    }
}
