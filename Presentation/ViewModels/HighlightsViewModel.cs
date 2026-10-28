using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using TWCF_BibleApp.Application.Services;

namespace TWCF_BibleApp.Presentation.ViewModels
{
    public sealed class HighlightsViewModel : AnnotationListViewModel
    {
        private List<HighlightListItem> _all = new();

        public HighlightsViewModel(BibleService bibleService, UserService userService) : base(bibleService, userService)
        {
            Filters = new ObservableCollection<ColorFilter>(
                new[] { new ColorFilter(null) { IsSelected = true } }.Concat(HighlightSwatch.All.Select(s => new ColorFilter(s))));

            OpenHighlightCommand = new AsyncRelayCommand<HighlightListItem>(h => h == null ? Task.CompletedTask
                : OpenInReaderAsync((h.Highlight.BookId, h.Highlight.ChapterNumber, h.Highlight.StartVerseNumber, h.Highlight.EndVerseNumber)));
            RemoveCommand = new AsyncRelayCommand<HighlightListItem>(RemoveAsync);
            SelectFilterCommand = new RelayCommand<ColorFilter>(SelectFilter);
        }

        public IAsyncRelayCommand<HighlightListItem> OpenHighlightCommand { get; }
        public IAsyncRelayCommand<HighlightListItem> RemoveCommand { get; }
        public IRelayCommand<ColorFilter> SelectFilterCommand { get; }

        public ObservableCollection<ColorFilter> Filters { get; }
        public ObservableCollection<HighlightGroup> Groups { get; } = new();

        public bool IsEmpty => !IsLoading && _all.Count == 0;
        public bool HasNoMatches => _all.Count > 0 && Groups.Count == 0;
        public bool HasItems => Groups.Count > 0;
        public bool HasAny => _all.Count > 0;
        public string Summary => _all.Count == 0 ? "" : $"{_all.Count} {(_all.Count == 1 ? "highlight" : "highlights")}";

        public async Task LoadAsync()
        {
            IsLoading = true;
            try
            {
                var highlights = (await UserService.GetAllHighlightsAsync()).ToList();
                var passages = highlights.Select(h => (h.BookId, h.ChapterNumber, h.StartVerseNumber, h.EndVerseNumber)).ToList();
                var (books, texts) = await LoadPassagesAsync(passages);
                _all = highlights.Select((h, i) =>
                {
                    books.TryGetValue(h.BookId, out var book);
                    return new HighlightListItem(h, BookTitle(book), Reference(book, h.ChapterNumber, h.StartVerseNumber, h.EndVerseNumber),
                        texts.GetValueOrDefault(passages[i], ""), book?.Order ?? 0);
                }).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Loading highlights failed: {ex}");
            }
            finally
            {
                IsLoading = false;
                Rebuild();
            }
        }

        private void SelectFilter(ColorFilter? filter)
        {
            if (filter == null) return;
            foreach (var f in Filters) f.IsSelected = f == filter;
            Rebuild();
        }

        // Bible order, grouped by book, limited to the chosen colour.
        private void Rebuild()
        {
            var colour = Filters.FirstOrDefault(f => f.IsSelected)?.Swatch;
            var groups = _all
                .Where(h => colour == null || string.Equals(h.Highlight.ColorHex, colour.Hex, StringComparison.OrdinalIgnoreCase))
                .OrderBy(h => h.BookOrder).ThenBy(h => h.Highlight.ChapterNumber).ThenBy(h => h.Highlight.StartVerseNumber)
                .GroupBy(h => h.BookOrder)
                .Select(g => new HighlightGroup(g.First().BookName, g));

            Groups.Clear();
            foreach (var g in groups) Groups.Add(g);

            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(HasNoMatches));
            OnPropertyChanged(nameof(HasItems));
            OnPropertyChanged(nameof(HasAny));
            OnPropertyChanged(nameof(Summary));
        }

        // No confirmation: a highlight is quick to put back, unlike a written note.
        private async Task RemoveAsync(HighlightListItem? item)
        {
            if (item == null) return;
            try
            {
                await UserService.DeleteHighlightAsync(item.Highlight.Id);
                _all.Remove(item);
                Rebuild();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Removing highlight failed: {ex}");
                await Shell.Current.DisplayAlertAsync("Couldn't remove", "The highlight could not be removed. Please try again.", "OK");
            }
        }
    }
}
