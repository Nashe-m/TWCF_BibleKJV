using Microsoft.Maui.Controls;
using TWCF_BibleApp.Presentation.ViewModels;
using System;
using System.Threading.Tasks;

namespace TWCF_BibleApp.Presentation.Views
{
    public partial class ReaderPage : ContentPage
    {
        private readonly ReaderViewModel _vm;

        public ReaderPage(ReaderViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
            _vm = vm;

            _vm.ScrollRequested += OnScrollRequested;
            _vm.ProjectorModeChanged += (_, on) => SetFullScreen(on);
            HandlerChanged += (_, _) => AttachKeyboardShortcuts();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _vm.OnAppearingAsync();
        }

        private void OnScrollRequested(object? sender, int verseIndex)
        {
            // Wait a moment so the new verses are laid out before scrolling. Offsets are computed
            // by hand because ScrollToAsync(element) ignores the offset of the item's parent layout.
            // Not awaited: on Windows ScrollToAsync never completes when the position doesn't change.
            Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(150), () =>
            {
                if (verseIndex >= 0 && verseIndex < VersesLayout.Children.Count
                    && VersesLayout.Children[verseIndex] is VisualElement verse)
                {
                    var y = VersesLayout.Y + verse.Y - (ReadingScroll.Height - verse.Height) / 2;
                    _ = ReadingScroll.ScrollToAsync(0, Math.Max(0, y), true);
                }
                else
                {
                    _ = ReadingScroll.ScrollToAsync(0, 0, false);
                }

                _ = ScrollSidebarToCurrentBookAsync();
            });
        }

        private BookNavItem? _sidebarBook;

        private async Task ScrollSidebarToCurrentBookAsync()
        {
            var book = _vm.CurrentBook;
            if (book == null || book == _sidebarBook || !SidebarScroll.IsVisible) return;

            var isOld = _vm.OldTestamentBooks.Contains(book);
            var list = isOld ? OldTestamentList : NewTestamentList;
            var index = (isOld ? _vm.OldTestamentBooks : _vm.NewTestamentBooks).IndexOf(book);
            if (index < 0 || index >= list.Children.Count || list.Children[index] is not VisualElement item) return;

            // On startup the sidebar may not be measured yet.
            for (var attempt = 0; attempt < 20 && (list.Height <= 0 || SidebarScroll.ContentSize.Height <= 0); attempt++)
                await Task.Delay(100);

            _sidebarBook = book;
            _ = SidebarScroll.ScrollToAsync(0, Math.Max(0, list.Y + item.Y - 24), false);
        }

        private void ProjectorControls_PointerEntered(object? sender, PointerEventArgs e) => ProjectorControls.Opacity = 1;
        private void ProjectorControls_PointerExited(object? sender, PointerEventArgs e) => ProjectorControls.Opacity = 0.35;

        private void SetFullScreen(bool fullScreen)
        {
#if WINDOWS
            if (Window?.Handler?.PlatformView is Microsoft.UI.Xaml.Window window)
            {
                window.AppWindow.SetPresenter(fullScreen
                    ? Microsoft.UI.Windowing.AppWindowPresenterKind.FullScreen
                    : Microsoft.UI.Windowing.AppWindowPresenterKind.Default);
            }
#endif
        }

#if WINDOWS
        private bool _shortcutsAttached;

        private void AttachKeyboardShortcuts()
        {
            if (_shortcutsAttached || Handler?.PlatformView is not Microsoft.UI.Xaml.UIElement element) return;
            _shortcutsAttached = true;

            // Accelerators without a scope work wherever focus is in the window.
            element.KeyboardAcceleratorPlacementMode = Microsoft.UI.Xaml.Input.KeyboardAcceleratorPlacementMode.Hidden;
            const Windows.System.VirtualKeyModifiers ctrl = Windows.System.VirtualKeyModifiers.Control;
            const Windows.System.VirtualKeyModifiers none = Windows.System.VirtualKeyModifiers.None;

            AddShortcut(element, (Windows.System.VirtualKey)187, ctrl, () => _vm.IncreaseFontCommand.Execute(null));  // Ctrl + '='/'+'
            AddShortcut(element, Windows.System.VirtualKey.Add, ctrl, () => _vm.IncreaseFontCommand.Execute(null));
            AddShortcut(element, (Windows.System.VirtualKey)189, ctrl, () => _vm.DecreaseFontCommand.Execute(null));  // Ctrl + '-'
            AddShortcut(element, Windows.System.VirtualKey.Subtract, ctrl, () => _vm.DecreaseFontCommand.Execute(null));
            AddShortcut(element, Windows.System.VirtualKey.Number0, ctrl, () => _vm.ResetFontCommand.Execute(null));
            AddShortcut(element, Windows.System.VirtualKey.B, ctrl, () => _vm.ToggleSidebarCommand.Execute(null));
            AddShortcut(element, Windows.System.VirtualKey.D, ctrl, () => _vm.ToggleThemeCommand.Execute(null));
            AddShortcut(element, Windows.System.VirtualKey.K, ctrl, FocusSearch);
            AddShortcut(element, Windows.System.VirtualKey.F5, none, () => _vm.ToggleProjectorCommand.Execute(null));
            AddShortcut(element, Windows.System.VirtualKey.F11, none, () => _vm.ToggleProjectorCommand.Execute(null));
            AddShortcut(element, Windows.System.VirtualKey.Escape, none, () => _vm.ExitProjectorCommand.Execute(null));

            // Chapter navigation, except while typing in a text box.
            AddShortcut(element, Windows.System.VirtualKey.Left, none, () => _vm.PreviousChapterCommand.Execute(null), skipWhenTyping: true);
            AddShortcut(element, Windows.System.VirtualKey.Right, none, () => _vm.NextChapterCommand.Execute(null), skipWhenTyping: true);

            // Ctrl + mouse wheel zooms the text, like a browser.
            element.AddHandler(Microsoft.UI.Xaml.UIElement.PointerWheelChangedEvent,
                new Microsoft.UI.Xaml.Input.PointerEventHandler((s, e) =>
                {
                    if (!e.KeyModifiers.HasFlag(ctrl)) return;
                    var delta = e.GetCurrentPoint(element).Properties.MouseWheelDelta;
                    (delta > 0 ? _vm.IncreaseFontCommand : _vm.DecreaseFontCommand).Execute(null);
                    e.Handled = true;
                }), handledEventsToo: true);
        }

        private static void AddShortcut(Microsoft.UI.Xaml.UIElement element, Windows.System.VirtualKey key,
            Windows.System.VirtualKeyModifiers modifiers, Action action, bool skipWhenTyping = false)
        {
            var accelerator = new Microsoft.UI.Xaml.Input.KeyboardAccelerator { Key = key, Modifiers = modifiers };
            accelerator.Invoked += (s, e) =>
            {
                if (skipWhenTyping && Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(element.XamlRoot)
                        is Microsoft.UI.Xaml.Controls.TextBox)
                    return;

                e.Handled = true;
                action();
            };
            element.KeyboardAccelerators.Add(accelerator);
        }

        private void FocusSearch()
        {
            if (_vm.IsProjectorMode) return;
            SearchEntry.Focus();
        }
#else
        private void AttachKeyboardShortcuts() { }
#endif
    }
}
