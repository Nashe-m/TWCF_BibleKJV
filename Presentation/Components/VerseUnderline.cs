using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace TWCF_BibleApp.Presentation.Components
{
    /// <summary>
    /// A faint dotted underline beneath each line of a selected <see cref="VerseLabel"/>, in the
    /// accent colour whatever the text colour. Sits over the label in the same layout cell and
    /// lets taps through.
    /// </summary>
    public sealed class VerseUnderline : GraphicsView, IDrawable
    {
        private const float DotRadius = 1.1f;
        private const float DotGap = 4.5f;      // centre to centre
        private const float Opacity = 0.6f;

        public static readonly BindableProperty LabelProperty = BindableProperty.Create(
            nameof(Label), typeof(VerseLabel), typeof(VerseUnderline),
            propertyChanged: (b, o, n) => ((VerseUnderline)b).Attach(o as VerseLabel, n as VerseLabel));

        public VerseLabel? Label
        {
            get => (VerseLabel?)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public VerseUnderline()
        {
            Drawable = this;
            InputTransparent = true;
            IsVisible = false;
            BackgroundColor = Colors.Transparent;
        }

        private void Attach(VerseLabel? old, VerseLabel? label)
        {
            if (old != null)
            {
                old.UnderlineChanged -= OnUnderlineChanged;
                old.SizeChanged -= OnUnderlineChanged;
            }
            if (label != null)
            {
                label.UnderlineChanged += OnUnderlineChanged;
                label.SizeChanged += OnUnderlineChanged;
            }
            OnUnderlineChanged(this, EventArgs.Empty);
        }

        private void OnUnderlineChanged(object? sender, EventArgs e)
        {
            IsVisible = VerseLabel.SupportsDrawnUnderline && Label?.IsUnderlined == true;
            if (!IsVisible) return;
            Invalidate();
            // The platform re-lays out the text a moment after a change; draw again once it has.
            Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(60), Invalidate);
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            if (Label is not { IsUnderlined: true } label) return;

            var accent = Microsoft.Maui.Controls.Application.Current?.Resources.TryGetValue("AccentColor", out var value) == true
                && value is Color c ? c : Colors.Gray;
            canvas.FillColor = accent.WithAlpha(Opacity);

            // A little below the baseline, clear of the letters but above descenders' worst.
            var drop = (float)Math.Max(3, label.FontSize * 0.14);
            foreach (var line in label.GetUnderlineSegments())
            {
                var y = (float)line.Baseline + drop;
                for (var x = (float)line.Left + DotRadius; x <= line.Right; x += DotGap)
                    canvas.FillCircle(x, y, DotRadius);
            }
        }
    }
}
