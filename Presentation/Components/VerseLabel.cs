using System;
using System.Collections.Generic;
using Microsoft.Maui.Controls;
using TWCF_BibleApp.Presentation.ViewModels;

namespace TWCF_BibleApp.Presentation.Components
{
    /// <summary>One wrapped line of verse text to underline, in the label's coordinates.</summary>
    public readonly record struct UnderlineSegment(double Left, double Right, double Baseline);

    /// <summary>
    /// Renders a verse as "number  text", colouring the words of Jesus when red letter is on.
    /// Span colours use DynamicResource so theme changes apply without a rebuild.
    /// </summary>
    public class VerseLabel : Label
    {
        public static readonly BindableProperty VerseProperty = BindableProperty.Create(
            nameof(Verse), typeof(VerseItem), typeof(VerseLabel), propertyChanged: Rebuild);

        public static readonly BindableProperty ShowWordsOfJesusProperty = BindableProperty.Create(
            nameof(ShowWordsOfJesus), typeof(bool), typeof(VerseLabel), true, propertyChanged: Rebuild);

        public static readonly BindableProperty ShowNumberProperty = BindableProperty.Create(
            nameof(ShowNumber), typeof(bool), typeof(VerseLabel), true, propertyChanged: Rebuild);

        public static readonly BindableProperty NumberFontSizeProperty = BindableProperty.Create(
            nameof(NumberFontSize), typeof(double), typeof(VerseLabel), 14.0, propertyChanged: Rebuild);

        // Set directly on the spans (not via DynamicResource) so a change rebuilds the text and the
        // label is re-measured for the new font's width.
        public static readonly BindableProperty TextFontFamilyProperty = BindableProperty.Create(
            nameof(TextFontFamily), typeof(string), typeof(VerseLabel), "Georgia", propertyChanged: Rebuild);

        public string TextFontFamily
        {
            get => (string)GetValue(TextFontFamilyProperty);
            set => SetValue(TextFontFamilyProperty, value);
        }

        // A selected verse is underlined (the text only, not the verse number). Where the platform
        // can report line positions, VerseUnderline draws faint dots; elsewhere a plain underline.
        public static readonly BindableProperty IsUnderlinedProperty = BindableProperty.Create(
            nameof(IsUnderlined), typeof(bool), typeof(VerseLabel), false,
            propertyChanged: (b, _, _) =>
            {
                var label = (VerseLabel)b;
                if (SupportsDrawnUnderline) label.UnderlineChanged?.Invoke(label, EventArgs.Empty);
                else label.Rebuild();
            });

        public bool IsUnderlined
        {
            get => (bool)GetValue(IsUnderlinedProperty);
            set => SetValue(IsUnderlinedProperty, value);
        }

        /// <summary>Raised when the underline should be redrawn: it was turned on or off, or the text changed.</summary>
        public event EventHandler? UnderlineChanged;

#if ANDROID || WINDOWS
        public static bool SupportsDrawnUnderline => true;
#else
        public static bool SupportsDrawnUnderline => false;
#endif

        public VerseItem? Verse
        {
            get => (VerseItem?)GetValue(VerseProperty);
            set => SetValue(VerseProperty, value);
        }

        public bool ShowWordsOfJesus
        {
            get => (bool)GetValue(ShowWordsOfJesusProperty);
            set => SetValue(ShowWordsOfJesusProperty, value);
        }

        public bool ShowNumber
        {
            get => (bool)GetValue(ShowNumberProperty);
            set => SetValue(ShowNumberProperty, value);
        }

        public double NumberFontSize
        {
            get => (double)GetValue(NumberFontSizeProperty);
            set => SetValue(NumberFontSizeProperty, value);
        }

        // Where the verse text starts, past the number and its gap: as a character offset into
        // the whole text, and as an index into the spans.
        private int _textStartOffset;
        private int _textStartSpan;

        private static void Rebuild(BindableObject bindable, object oldValue, object newValue) =>
            ((VerseLabel)bindable).Rebuild();

        private void Rebuild()
        {
            if (Verse == null)
            {
                FormattedText = null;
                return;
            }

            var formatted = new FormattedString();
            _textStartOffset = 0;
            _textStartSpan = 0;

            if (ShowNumber)
            {
                var numberText = Verse.Number.ToString();
                var number = new Span { Text = numberText, FontAttributes = FontAttributes.Bold, FontSize = NumberFontSize };
                number.SetDynamicResource(Span.FontFamilyProperty, "UiFont");
                number.SetDynamicResource(Span.TextColorProperty, "AccentColor");
                formatted.Spans.Add(number);
                formatted.Spans.Add(new Span { Text = "  " });
                _textStartOffset = numberText.Length + 2;
                _textStartSpan = 2;
            }

            var solid = IsUnderlined && !SupportsDrawnUnderline;
            foreach (var segment in Verse.Segments)
            {
                // Font size is inherited from the label; family and colour are not.
                var span = new Span
                {
                    Text = segment.Text,
                    FontFamily = TextFontFamily,
                    TextDecorations = solid ? TextDecorations.Underline : TextDecorations.None,
                };
                span.SetDynamicResource(Span.TextColorProperty,
                    segment.IsWordsOfJesus && ShowWordsOfJesus ? "WordsOfJesusColor" : "PageTextColor");
                formatted.Spans.Add(span);
            }

            FormattedText = formatted;
            UnderlineChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// The baseline and extent of each line of verse text as laid out now (empty if the
        /// platform can't say, or the text isn't laid out yet). Trailing spaces are left out.
        /// </summary>
        public IReadOnlyList<UnderlineSegment> GetUnderlineSegments()
        {
            var segments = new List<UnderlineSegment>();
#if ANDROID
            if (Handler?.PlatformView is Android.Widget.TextView tv && tv.Layout is { } layout && tv.Text is { } text)
            {
                var start = Math.Min(_textStartOffset, text.Length);
                if (start >= text.Length) return segments;
                // The layout works in pixels; MAUI in device-independent units.
                var density = tv.Resources?.DisplayMetrics?.Density ?? 1f;
                double padLeft = tv.TotalPaddingLeft, padTop = tv.TotalPaddingTop;
                var firstLine = layout.GetLineForOffset(start);
                for (var line = firstLine; line < layout.LineCount; line++)
                {
                    var lineLeft = layout.GetLineLeft(line);
                    var left = line == firstLine ? layout.GetPrimaryHorizontal(start) : lineLeft;
                    var right = lineLeft + layout.GetLineMax(line);   // excludes trailing whitespace
                    if (right - left < 1) continue;
                    segments.Add(new UnderlineSegment((padLeft + left) / density, (padLeft + right) / density,
                        (padTop + layout.GetLineBaseline(line)) / density));
                }
            }
#elif WINDOWS
            if (Handler?.PlatformView is Microsoft.UI.Xaml.Controls.TextBlock tb)
            {
                var runs = new List<Microsoft.UI.Xaml.Documents.Run>();
                CollectRuns(tb.Inlines, runs);
                if (runs.Count <= _textStartSpan) return segments;

                // Lines are found by walking the characters; a new top means a new line. The block
                // reports only the first line's baseline, so later ones keep the same distance below their top.
                var firstTop = tb.ContentStart.GetCharacterRect(Microsoft.UI.Xaml.Documents.LogicalDirection.Forward).Y;
                double? top = null;
                double left = 0, right = 0, height = 0;
                var hasInk = false;

                void Flush()
                {
                    if (top is double t && hasInk && right - left >= 1)
                        segments.Add(new UnderlineSegment(left, right, tb.BaselineOffset + (t - firstTop)));
                }

                for (var r = _textStartSpan; r < runs.Count; r++)
                {
                    var run = runs[r];
                    var runText = run.Text ?? "";
                    for (var i = 0; i < runText.Length; i++)
                    {
                        var pointer = run.ContentStart.GetPositionAtOffset(i, Microsoft.UI.Xaml.Documents.LogicalDirection.Forward);
                        if (pointer == null) continue;
                        var rect = pointer.GetCharacterRect(Microsoft.UI.Xaml.Documents.LogicalDirection.Forward);
                        if (top is not double current || Math.Abs(rect.Y - current) > Math.Max(1, height / 2))
                        {
                            Flush();
                            top = rect.Y;
                            height = rect.Height;
                            hasInk = false;
                        }
                        if (char.IsWhiteSpace(runText[i])) continue;
                        if (!hasInk) { left = rect.X; right = rect.X + rect.Width; hasInk = true; }
                        else { left = Math.Min(left, rect.X); right = Math.Max(right, rect.X + rect.Width); }
                    }
                }
                Flush();
            }
#endif
            return segments;
        }

#if WINDOWS
        private static void CollectRuns(Microsoft.UI.Xaml.Documents.InlineCollection inlines, List<Microsoft.UI.Xaml.Documents.Run> runs)
        {
            foreach (var inline in inlines)
            {
                if (inline is Microsoft.UI.Xaml.Documents.Run run) runs.Add(run);
                else if (inline is Microsoft.UI.Xaml.Documents.Span span) CollectRuns(span.Inlines, runs);
            }
        }
#endif
    }
}
