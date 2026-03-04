using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using TWCF_BibleApp.Application.DTOs;
using TWCF_BibleApp.Application.Interfaces;
using CommunityToolkit.Mvvm.Input;

namespace TWCF_BibleApp.Presentation.ViewModels
{
    public class SearchViewModel : INotifyPropertyChanged
    {
        private readonly IBibleRepository _repository;

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public string SearchQuery { get; set; } = string.Empty;

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

        public SearchViewModel(IBibleRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            PerformSearchCommand = new AsyncRelayCommand(PerformSearch);
        }

        private async Task PerformSearch()
        {
            if (string.IsNullOrWhiteSpace(SearchQuery)) return;

            var results = await _repository.SearchVersesAsync(SearchQuery);

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