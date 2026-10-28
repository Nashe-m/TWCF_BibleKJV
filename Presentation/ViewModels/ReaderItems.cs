using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using TWCF_BibleApp.Application.DTOs;
using TWCF_BibleApp.Application.Services;

namespace TWCF_BibleApp.Presentation.ViewModels
{
    /// <summary>A book in the sidebar; the selected one expands to show its chapters.</summary>
    public sealed partial class BookNavItem : ObservableObject
    {
        public BookNavItem(BookDto book)
        {
            Book = book;
            DisplayName = ReferenceParser.DisplayBookName(book.Name);
        }

        public BookDto Book { get; }
        public string DisplayName { get; }
        public ObservableCollection<ChapterNavItem> Chapters { get; } = new();

        [ObservableProperty]
        private bool _isSelected;
    }

    public sealed partial class ChapterNavItem : ObservableObject
    {
        public ChapterNavItem(int number) => Number = number;

        public int Number { get; }

        [ObservableProperty]
        private bool _isCurrent;
    }

    /// <summary>A verse in the reading column.</summary>
    public sealed partial class VerseItem : ObservableObject
    {
        public VerseItem(VerseDto verse) => Verse = verse;

        public VerseDto Verse { get; }
        public int Number => Verse.VerseNumber;
        public string Text => Verse.Text;

        // The verse the user jumped to (e.g. from "Jn 3:16" or a search result).
        [ObservableProperty]
        private bool _isTarget;
    }
}
