using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using TWCF_BibleApp.Domain;

namespace TWCF_BibleApp.Presentation.ViewModels
{
    /// <summary>A note on the Notes page, with the verses it belongs to.</summary>
    public sealed partial class NoteListItem : ObservableObject
    {
        public NoteListItem(Note note, string bookName, string reference, string verseText, int bookOrder)
        {
            Note = note;
            BookName = bookName;
            Reference = reference;
            VerseText = verseText;
            BookOrder = bookOrder;
        }

        public Note Note { get; private set; }
        public string BookName { get; }
        public string Reference { get; }
        public string VerseText { get; }
        public int BookOrder { get; }

        public string Text => Note.Text;
        public DateTime LastChangedUtc => Note.ModifiedUtc ?? Note.CreatedUtc;
        public string DateLabel => LastChangedUtc.ToLocalTime().ToString("d MMM yyyy");

        // Inline editing on the Notes page.
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotEditing))]
        private bool _isEditing;

        public bool IsNotEditing => !IsEditing;

        [ObservableProperty]
        private string _editText = "";

        public void Update(Note note)
        {
            Note = note;
            OnPropertyChanged(nameof(Text));
            OnPropertyChanged(nameof(DateLabel));
        }
    }

    /// <summary>A highlight on the Highlights page.</summary>
    public sealed class HighlightListItem
    {
        public HighlightListItem(Highlight highlight, string bookName, string reference, string verseText, int bookOrder)
        {
            Highlight = highlight;
            BookName = bookName;
            Reference = reference;
            VerseText = verseText;
            BookOrder = bookOrder;
            Swatch = HighlightSwatch.Find(highlight.ColorHex);
        }

        public Highlight Highlight { get; }
        public string BookName { get; }
        public string Reference { get; }
        public string VerseText { get; }
        public int BookOrder { get; }
        public HighlightSwatch Swatch { get; }
        public Color Color => Color.FromArgb(Highlight.ColorHex);
    }

    /// <summary>A heading and its items in a grouped list ("John", "October 2026").</summary>
    public class ListGroup<T> : List<T>
    {
        public ListGroup(string title, IEnumerable<T> items) : base(items) => Title = title;

        public string Title { get; }
    }

    // Non-generic so XAML can name them in x:DataType.
    public sealed class NoteGroup : ListGroup<NoteListItem>
    {
        public NoteGroup(string title, IEnumerable<NoteListItem> items) : base(title, items) { }
    }

    public sealed class HighlightGroup : ListGroup<HighlightListItem>
    {
        public HighlightGroup(string title, IEnumerable<HighlightListItem> items) : base(title, items) { }
    }

    /// <summary>A colour chip that filters the Highlights page; Swatch is null for "All".</summary>
    public sealed partial class ColorFilter : ObservableObject
    {
        public ColorFilter(HighlightSwatch? swatch) => Swatch = swatch;

        public HighlightSwatch? Swatch { get; }
        public string Name => Swatch?.Name ?? "All";
        public bool HasColor => Swatch != null;
        public Color Color => Swatch?.Color ?? Colors.Transparent;

        [ObservableProperty]
        private bool _isSelected;
    }
}
