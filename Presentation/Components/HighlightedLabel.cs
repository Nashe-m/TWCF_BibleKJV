using Microsoft.Maui.Controls;
using System.Text.RegularExpressions;

namespace TWCF_BibleApp.Presentation.Components
{
    /// <summary>
    /// Renders search result text, showing the matched words (wrapped in &lt;m&gt;...&lt;/m&gt; by
    /// BibleRepository.SearchVersesAsync) in bold accent colour.
    /// </summary>
    public class HighlightedLabel : Label
    {
        public static readonly BindableProperty MarkedTextProperty = BindableProperty.Create(
            nameof(MarkedText), typeof(string), typeof(HighlightedLabel), propertyChanged: (b, _, _) => ((HighlightedLabel)b).Rebuild());

        public string? MarkedText
        {
            get => (string?)GetValue(MarkedTextProperty);
            set => SetValue(MarkedTextProperty, value);
        }

        private void Rebuild()
        {
            if (string.IsNullOrEmpty(MarkedText))
            {
                FormattedText = null;
                return;
            }

            var formatted = new FormattedString();
            var inMatch = false;
            foreach (var part in Regex.Split(MarkedText, "(</?m>)"))
            {
                if (part == "<m>") inMatch = true;
                else if (part == "</m>") inMatch = false;
                else if (part.Length > 0)
                {
                    var span = new Span { Text = part, FontAttributes = inMatch ? FontAttributes.Bold : FontAttributes.None };
                    span.SetDynamicResource(Span.TextColorProperty, inMatch ? "AccentColor" : "PageTextColor");
                    formatted.Spans.Add(span);
                }
            }

            FormattedText = formatted;
        }
    }
}
