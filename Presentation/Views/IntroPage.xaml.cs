using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using TWCF_BibleApp.Presentation.Services;

namespace TWCF_BibleApp.Presentation.Views
{
    /// <summary>
    /// A short "the Bible opens" animation (about 1.5 s) played on launch. Settings load while it
    /// runs, so the shell opens straight into the user's theme instead of flashing light first.
    /// </summary>
    public partial class IntroPage : ContentPage
    {
        private const double LineOpacity = 0.35; // matches the splash artwork

        private readonly AppSettings _settings;
        private readonly Func<Page> _createNext;
        private readonly CancellationTokenSource _skip = new();
        private bool _started;
        private bool _finished;

        public IntroPage(AppSettings settings, Func<Page> createNext)
        {
            InitializeComponent();
            _settings = settings;
            _createNext = createNext;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (_started) return;
            _started = true;

            var loadSettings = LoadSettingsSafelyAsync();
            try
            {
                await PlayAsync(_skip.Token);
            }
            catch (OperationCanceledException)
            {
                // Skipped: fall through and open the app.
            }

            await loadSettings;
            await FinishAsync();
        }

        private async Task PlayAsync(CancellationToken ct)
        {
            await Task.Delay(120, ct);

            // The pages unfold from the spine.
            await Task.WhenAll(
                LeftPage.ScaleXToAsync(1, 420, Easing.CubicOut),
                RightPage.ScaleXToAsync(1, 420, Easing.CubicOut));
            ct.ThrowIfCancellationRequested();

            // Lines of text write in, left page then right, while the ribbon drops into place
            // and the name rises underneath.
            var lines = new[] { Line1, Line2, Line3, Line4, Line5 };
            var steps = new Task[lines.Length + 2];
            for (var i = 0; i < lines.Length; i++)
                steps[i] = FadeInAfterAsync(lines[i], 60 * i, ct);
            steps[lines.Length] = Task.WhenAll(
                Ribbon.FadeToAsync(1, 220),
                Ribbon.TranslateToAsync(0, 0, 380, Easing.SpringOut));
            steps[lines.Length + 1] = Task.WhenAll(
                AppName.FadeToAsync(1, 360, Easing.CubicOut),
                AppName.TranslateToAsync(0, 0, 360, Easing.CubicOut));
            await Task.WhenAll(steps);

            await Task.Delay(380, ct);
        }

        private static async Task FadeInAfterAsync(VisualElement element, int delayMs, CancellationToken ct)
        {
            await Task.Delay(delayMs, ct);
            await element.FadeToAsync(LineOpacity, 180);
        }

        private async Task LoadSettingsSafelyAsync()
        {
            try
            {
                await _settings.LoadAsync();
            }
            catch (Exception ex)
            {
                // The reader loads settings again and shows its own error state.
                System.Diagnostics.Debug.WriteLine($"Intro: loading settings failed: {ex}");
            }
        }

        /// <summary>Fades the artwork out and the background into the app's page colour, then swaps in the shell.</summary>
        private async Task FinishAsync()
        {
            if (_finished) return;
            _finished = true;

            var from = BackgroundColor;
            var to = Microsoft.Maui.Controls.Application.Current?.Resources.TryGetValue("PageBackgroundColor", out var c) == true
                     && c is Color pageColor ? pageColor : from;

            var fade = new Animation(t => BackgroundColor = Lerp(from, to, t), 0, 1, Easing.CubicInOut);
            var done = new TaskCompletionSource();
            fade.Commit(this, "IntroFade", length: 260, finished: (_, _) => done.TrySetResult());

            await Task.WhenAll(
                done.Task,
                Stage.FadeToAsync(0, 200, Easing.CubicIn),
                Book.ScaleToAsync(Book.Scale * 1.08, 260, Easing.CubicIn));

            if (Window != null)
                Window.Page = _createNext();
        }

        private static Color Lerp(Color a, Color b, double t) => new(
            (float)(a.Red + (b.Red - a.Red) * t),
            (float)(a.Green + (b.Green - a.Green) * t),
            (float)(a.Blue + (b.Blue - a.Blue) * t));

        private void OnSkipTapped(object? sender, TappedEventArgs e) => _skip.Cancel();
    }
}
