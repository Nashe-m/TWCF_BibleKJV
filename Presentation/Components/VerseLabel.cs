using Microsoft.Maui.Controls;
using TWCF_BibleApp.Presentation.ViewModels;

namespace TWCF_BibleApp.Presentation.Components
{
    /// <summary>
    /// Renders a verse as "number  text", colouring the words of Jesus when red letter is on.
    /// Span colours and fonts use DynamicResource so theme changes apply without a rebuild.
    /// </summary>
    public class VerseLabel : Label
    {
        public static readonly BindableProperty VerseProperty = BindableProperty.Create(
            nameof(Verse), typeof(VerseItem), typeof(VerseLabel), propertyChanged: Rebuild);

        public static readonly BindableProperty ShowWordsOfJesusProperty = BindableProperty.Create(
            nameof(ShowWordsOfJesus), typeof(bool), typeof(VerseLabel), true, propertyChanged: Rebuild);

        public static readonly BindableProperty NumberFontSizeProperty = BindableProperty.Create(
            nameof(NumberFontSize), typeof(double), typeof(VerseLabel), 14.0, propertyChanged: Rebuild);

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

        public double NumberFontSize
        {
            get => (double)GetValue(NumberFontSizeProperty);
            set => SetValue(NumberFontSizeProperty, value);
        }

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

            var number = new Span { Text = Verse.Number.ToString(), FontAttributes = FontAttributes.Bold, FontSize = NumberFontSize };
            number.SetDynamicResource(Span.FontFamilyProperty, "UiFont");
            number.SetDynamicResource(Span.TextColorProperty, "AccentColor");
            formatted.Spans.Add(number);
            formatted.Spans.Add(new Span { Text = "  " });

            foreach (var segment in Verse.Segments)
            {
                // Font size is inherited from the label; family and colour are not.
                var span = new Span { Text = segment.Text };
                span.SetDynamicResource(Span.FontFamilyProperty, "ScriptureFont");
                span.SetDynamicResource(Span.TextColorProperty,
                    segment.IsWordsOfJesus && ShowWordsOfJesus ? "WordsOfJesusColor" : "PageTextColor");
                formatted.Spans.Add(span);
            }

            FormattedText = formatted;
        }
    }
}
