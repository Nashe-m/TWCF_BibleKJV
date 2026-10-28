using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
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

    /// <summary>A run of verse text; IsWordsOfJesus marks red-letter text.</summary>
    public sealed record VerseSegment(string Text, bool IsWordsOfJesus);

    /// <summary>A verse in the reading column.</summary>
    public sealed partial class VerseItem : ObservableObject
    {
        public VerseItem(VerseDto verse)
        {
            Verse = verse;
            Segments = Split(verse);
        }

        public VerseDto Verse { get; }
        public int Number => Verse.VerseNumber;
        public string Text => Verse.Text;
        public IReadOnlyList<VerseSegment> Segments { get; }

        // MarkedText wraps the words of Jesus in <wj>...</wj> (see Tools/import_usfm.py).
        private static IReadOnlyList<VerseSegment> Split(VerseDto verse)
        {
            if (string.IsNullOrEmpty(verse.MarkedText))
                return new[] { new VerseSegment(verse.Text, false) };

            var segments = new List<VerseSegment>();
            var parts = Regex.Split(verse.MarkedText, "(</?wj>)");
            var inWords = false;
            foreach (var part in parts)
            {
                if (part == "<wj>") inWords = true;
                else if (part == "</wj>") inWords = false;
                else if (part.Length > 0) segments.Add(new VerseSegment(part, inWords));
            }
            return segments;
        }

        // The verse the user jumped to (e.g. from "Jn 3:16" or a search result).
        [ObservableProperty]
        private bool _isTarget;
    }
}
