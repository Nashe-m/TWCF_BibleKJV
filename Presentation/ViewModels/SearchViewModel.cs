using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using TWCF_BibleApp.Application.DTOs;
using TWCF_BibleApp.Application.Interfaces;
using TWCF_BibleApp.Application.Services;
using CommunityToolkit.Mvvm.Input;

namespace TWCF_BibleApp.Presentation.ViewModels
{
    public class SearchViewModel : INotifyPropertyChanged, IQueryAttributable
    {
        private readonly IBibleRepository _repository;
        private readonly UserService _userService;

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

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

        // The reader's search box opens this page with ?q=<text>.
        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("q", out var q) && q?.ToString() is { Length: > 0 } text)
            {
                SearchQuery = Uri.UnescapeDataString(text);
                PerformSearchCommand.Execute(null);
            }
        }

        public ObservableCollection<SearchResultDto> SearchResults { get; } = new();

        private SearchResultDto? _selectedResult;
        public SearchResultDto? SelectedResult
        {
            get => _selectedResult;
            set
            {
                if (_selectedResult == value) return;
                _selectedResult = value;
                OnPropertyChanged();
                if (value != null) NavigateToVerse(value);
            }
        }

        public ICommand PerformSearchCommand { get; }

        public SearchViewModel(IBibleRepository repository, UserService userService)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            PerformSearchCommand = new AsyncRelayCommand(PerformSearch);
        }

        private async Task PerformSearch()
        {
            if (string.IsNullOrWhiteSpace(SearchQuery)) return;

            // Search the version the reader is currently showing.
            var translationId = (await _userService.GetUserSettingAsync())?.LastTranslationId ?? 1;
            var results = await _repository.SearchVersesAsync(SearchQuery, translationId);

            SearchResults.Clear();
            foreach (var verse in results)
            {
                SearchResults.Add(new SearchResultDto
                {
                    BookId = verse.BookId,
                    Chapter = verse.ChapterNumber,
                    Verse = verse.VerseNumber,
                    Reference = $"{verse.Book.Name} {verse.ChapterNumber}:{verse.VerseNumber}",
                    Text = verse.Text
                });
            }
        }

        private async void NavigateToVerse(SearchResultDto result)
        {
            if (result == null) return;
            await Shell.Current.GoToAsync(
                $"//ReaderPage?bookId={result.BookId}&chapter={result.Chapter}&verse={result.Verse}");
        }
    }
}