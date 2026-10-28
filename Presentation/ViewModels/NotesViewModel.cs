using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TWCF_BibleApp.Application.DTOs;
using TWCF_BibleApp.Application.Services;
using TWCF_BibleApp.Domain;

namespace TWCF_BibleApp.Presentation.ViewModels
{
    /// <summary>
    /// Shared by the Notes and Highlights pages: book names, Bible order and the verse text
    /// (in the version the reader is showing) for each saved passage.
    /// </summary>
    public abstract class AnnotationListViewModel : ObservableObject
    {
        protected readonly BibleService BibleService;
        protected readonly UserService UserService;
        private Dictionary<int, BookDto>? _books;

        protected AnnotationListViewModel(BibleService bibleService, UserService userService)
        {
            BibleService = bibleService ?? throw new ArgumentNullException(nameof(bibleService));
            UserService = userService ?? throw new ArgumentNullException(nameof(userService));
        }

        private bool _isLoading;
        public bool IsLoading { get => _isLoading; protected set => SetProperty(ref _isLoading, value); }

        protected async Task<(IReadOnlyDictionary<int, BookDto> Books, IReadOnlyDictionary<(int, int, int, int), string> Texts)>
            LoadPassagesAsync(IEnumerable<(int BookId, int Chapter, int Start, int End)> passages)
        {
            _books ??= (await BibleService.GetBooksAsync()).ToDictionary(b => b.Id);
            var translationId = (await UserService.GetUserSettingAsync())?.LastTranslationId ?? 1;
            var texts = await BibleService.GetPassageTextsAsync(passages, translationId);
            return (_books, texts);
        }

        protected static string Reference(BookDto? book, int chapter, int start, int end) => book == null ? $"{chapter}:{start}"
            : new BibleReference(book, chapter, new[] { new VerseRange(start, end) }, true).Display;

        protected static string BookTitle(BookDto? book) => book == null ? "" : ReferenceParser.DisplayBookName(book.Name);

        // The reader marks the verses and scrolls to them.
        protected static Task OpenInReaderAsync((int BookId, int Chapter, int Start, int End) p) =>
            Shell.Current.GoToAsync($"//ReaderPage?bookId={p.BookId}&chapter={p.Chapter}&verse={p.Start}&to={p.End}");
    }

    public sealed class NotesViewModel : AnnotationListViewModel
    {
        private List<NoteListItem> _all = new();

        public NotesViewModel(BibleService bibleService, UserService userService) : base(bibleService, userService)
        {
            OpenNoteCommand = new AsyncRelayCommand<NoteListItem>(n => n == null ? Task.CompletedTask
                : OpenInReaderAsync((n.Note.BookId, n.Note.ChapterNumber, n.Note.StartVerseNumber, n.Note.EndVerseNumber ?? n.Note.StartVerseNumber)));
            EditCommand = new RelayCommand<NoteListItem>(StartEditing);
            CancelEditCommand = new RelayCommand<NoteListItem>(n => { if (n != null) n.IsEditing = false; });
            SaveEditCommand = new AsyncRelayCommand<NoteListItem>(SaveEditAsync);
            DeleteCommand = new AsyncRelayCommand<NoteListItem>(DeleteAsync);
            SortByBibleCommand = new RelayCommand(() => SortByDate = false);
            SortByDateCommand = new RelayCommand(() => SortByDate = true);
        }

        public IAsyncRelayCommand<NoteListItem> OpenNoteCommand { get; }
        public IRelayCommand<NoteListItem> EditCommand { get; }
        public IRelayCommand<NoteListItem> CancelEditCommand { get; }
        public IAsyncRelayCommand<NoteListItem> SaveEditCommand { get; }
        public IAsyncRelayCommand<NoteListItem> DeleteCommand { get; }
        public IRelayCommand SortByBibleCommand { get; }
        public IRelayCommand SortByDateCommand { get; }

        public ObservableCollection<NoteGroup> Groups { get; } = new();

        private string _searchText = "";
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value ?? "")) Rebuild();
            }
        }

        private bool _sortByDate;
        public bool SortByDate
        {
            get => _sortByDate;
            set
            {
                if (SetProperty(ref _sortByDate, value)) Rebuild();
            }
        }

        public bool IsEmpty => !IsLoading && _all.Count == 0;
        public bool HasNoMatches => _all.Count > 0 && Groups.Count == 0;
        public bool HasItems => Groups.Count > 0;
        public bool HasAny => _all.Count > 0;
        public string Summary => _all.Count == 0 ? "" : $"{_all.Count} {(_all.Count == 1 ? "note" : "notes")}";

        public async Task LoadAsync()
        {
            IsLoading = true;
            try
            {
                var notes = (await UserService.GetAllNotesAsync()).ToList();
                var (books, texts) = await LoadPassagesAsync(notes.Select(Passage));
                _all = notes.Select(n =>
                {
                    var p = Passage(n);
                    books.TryGetValue(n.BookId, out var book);
                    return new NoteListItem(n, BookTitle(book), Reference(book, p.Chapter, p.Start, p.End), texts.GetValueOrDefault(p, ""), book?.Order ?? 0);
                }).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Loading notes failed: {ex}");
            }
            finally
            {
                IsLoading = false;
                Rebuild();
            }
        }

        private static (int BookId, int Chapter, int Start, int End) Passage(Note n) =>
            (n.BookId, n.ChapterNumber, n.StartVerseNumber, n.EndVerseNumber ?? n.StartVerseNumber);

        private void Rebuild()
        {
            var query = SearchText.Trim();
            var matches = _all.Where(n => query.Length == 0
                || n.Text.Contains(query, StringComparison.CurrentCultureIgnoreCase)
                || n.Reference.Contains(query, StringComparison.CurrentCultureIgnoreCase)
                || n.VerseText.Contains(query, StringComparison.CurrentCultureIgnoreCase));

            IEnumerable<NoteGroup> groups = SortByDate
                // Newest first, by month
                ? matches.OrderByDescending(n => n.LastChangedUtc)
                    .GroupBy(n => n.LastChangedUtc.ToLocalTime().ToString("MMMM yyyy"))
                    .Select(g => new NoteGroup(g.Key, g))
                // Bible order, by book
                : matches.OrderBy(n => n.BookOrder).ThenBy(n => n.Note.ChapterNumber).ThenBy(n => n.Note.StartVerseNumber)
                    .GroupBy(n => n.BookOrder)
                    .Select(g => new NoteGroup(g.First().BookName, g));

            Groups.Clear();
            foreach (var g in groups) Groups.Add(g);

            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(HasNoMatches));
            OnPropertyChanged(nameof(HasItems));
            OnPropertyChanged(nameof(HasAny));
            OnPropertyChanged(nameof(Summary));
        }

        private void StartEditing(NoteListItem? item)
        {
            if (item == null) return;
            foreach (var n in _all) n.IsEditing = false;
            item.EditText = item.Text;
            item.IsEditing = true;
        }

        private async Task SaveEditAsync(NoteListItem? item)
        {
            if (item == null) return;
            var text = item.EditText.Trim();
            if (text.Length == 0)
            {
                // Same as the reader: an emptied note is deleted.
                await DeleteAsync(item);
                return;
            }

            var updated = new Note
            {
                Id = item.Note.Id, BookId = item.Note.BookId, ChapterNumber = item.Note.ChapterNumber,
                StartVerseNumber = item.Note.StartVerseNumber, EndVerseNumber = item.Note.EndVerseNumber,
                Text = text, CreatedUtc = item.Note.CreatedUtc, ModifiedUtc = DateTime.UtcNow,
            };
            try
            {
                await UserService.UpdateNoteAsync(updated);
                item.Update(updated);
                item.IsEditing = false;
                if (SortByDate) Rebuild();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Saving note failed: {ex}");
                await Shell.Current.DisplayAlertAsync("Couldn't save", "The note could not be saved. Please try again.", "OK");
            }
        }

        private async Task DeleteAsync(NoteListItem? item)
        {
            if (item == null) return;
            if (!await Shell.Current.DisplayAlertAsync("Delete note?", $"Your note on {item.Reference} will be deleted.", "Delete", "Cancel"))
                return;

            try
            {
                await UserService.DeleteNoteAsync(item.Note.Id);
                _all.Remove(item);
                Rebuild();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Deleting note failed: {ex}");
                await Shell.Current.DisplayAlertAsync("Couldn't delete", "The note could not be deleted. Please try again.", "OK");
            }
        }
    }
}
