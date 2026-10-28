using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
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
            UndoRemoveCommand = new AsyncRelayCommand(UndoRemoveAsync);
        }

        public IAsyncRelayCommand UndoRemoveCommand { get; }

        // The last removed highlight, while its Undo bar is showing.
        private HighlightListItem? _removed;
        private CancellationTokenSource? _undoTimer;

        private string _undoMessage = "";
        public string UndoMessage
        {
            get => _undoMessage;
            private set
            {
                if (SetProperty(ref _undoMessage, value)) OnPropertyChanged(nameof(CanUndo));
            }
        }

        public bool CanUndo => UndoMessage.Length > 0;

        private async void OfferUndo(HighlightListItem item)
        {
            _undoTimer?.Cancel();
            var cts = _undoTimer = new CancellationTokenSource();
            _removed = item;
            UndoMessage = $"Removed the highlight on {item.Reference}";
            try
            {
                await Task.Delay(8000, cts.Token);
                ClearUndo();
            }
            catch (TaskCanceledException)
            {
            }
        }

        public void ClearUndo()
        {
            _undoTimer?.Cancel();
            _removed = null;
            UndoMessage = "";
        }

        private async Task UndoRemoveAsync()
        {
            if (_removed is not { } item) return;
            ClearUndo();
            try
            {
                // Same verses, colour and date; it gets a new Id, so reload the list.
                await UserService.AddHighlightAsync(item.Highlight);
                await LoadAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Undoing highlight removal failed: {ex}");
                await Shell.Current.DisplayAlertAsync("Couldn't undo", "The highlight could not be put back. Please try again.", "OK");
            }
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

        // No confirmation: Undo is offered instead.
        private async Task RemoveAsync(HighlightListItem? item)
        {
            if (item == null) return;
            try
            {
                await UserService.DeleteHighlightAsync(item.Highlight.Id);
                _all.Remove(item);
                Rebuild();
                OfferUndo(item);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Removing highlight failed: {ex}");
                await Shell.Current.DisplayAlertAsync("Couldn't remove", "The highlight could not be removed. Please try again.", "OK");
            }
        }
    }
}
