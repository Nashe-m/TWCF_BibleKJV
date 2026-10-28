using Microsoft.Maui.Controls;
using TWCF_BibleApp.Application.DTOs;
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
            _vm.ProjectorModeChanged += (_, on) =>
            {
                KeepCurrentVerseInView();
                SetFullScreen(on);
                (Shell.Current as AppShell)?.SetNavigationVisible(!on);
            };
            HandlerChanged += (_, _) => AttachKeyboardShortcuts();
            SearchBox.SizeChanged += (_, _) => AlignSuggestionsPanel();

            // Start typing straight away when the note editor opens.
            _vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(ReaderViewModel.IsNoteEditorOpen) && _vm.IsNoteEditorOpen)
                    Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(100), () => NoteEditorText.Focus());
            };
            SizeChanged += (_, _) =>
            {
                _vm.UpdateLayout(Width);
                ApplyLayout();
            };
        }

        // ---- Responsive layout ---------------------------------------------------------------

        private bool? _appliedCompact;
        private bool? _appliedNarrow;

        // Grid positions can't be bound, so the parts that move between layouts are placed here.
        private void ApplyLayout()
        {
            if (_appliedCompact != _vm.IsCompact)
            {
                _appliedCompact = _vm.IsCompact;
                // Wide: docked beside the text. Compact: a drawer over the text (above the scrim).
                Grid.SetColumn(Sidebar, _vm.IsCompact ? 1 : 0);
                Sidebar.HorizontalOptions = _vm.IsCompact ? LayoutOptions.Start : LayoutOptions.Fill;
            }

            if (_appliedNarrow != _vm.IsNarrow)
            {
                var narrow = _vm.IsNarrow;
                _appliedNarrow = narrow;
                // Phone: the search box gets its own full-width row; the version shows as "KJV".
                Grid.SetRow(SearchBox, narrow ? 1 : 0);
                Grid.SetColumn(SearchBox, narrow ? 0 : 3);
                Grid.SetColumnSpan(SearchBox, narrow ? 5 : 1);
                SearchBox.Margin = narrow ? new Thickness(0, 10, 0, 0) : new Thickness(0);
                VersionPicker.ItemDisplayBinding = new Binding(narrow ? nameof(TranslationDto.Code) : nameof(TranslationDto.DisplayName));

                // Note editor: bottom sheet on a phone, side panel otherwise.
                NoteEditor.HorizontalOptions = narrow ? LayoutOptions.Fill : LayoutOptions.End;
                NoteEditor.VerticalOptions = narrow ? LayoutOptions.End : LayoutOptions.Fill;
                NoteEditor.WidthRequest = narrow ? -1 : 420;
                NoteEditor.HeightRequest = narrow ? Math.Max(320, Height * 0.6) : -1;
                NoteEditor.Margin = narrow ? new Thickness(8, 0, 8, 8) : new Thickness(16);
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _vm.OnAppearingAsync();
        }

        // The verse to keep in view while the text is still being laid out after a jump.
        private int _scrollTarget = -1;
        private DateTime _keepScrollTargetUntil;

        private void OnScrollRequested(object? sender, int verseIndex)
        {
            _scrollTarget = verseIndex;
            // Entering projector mode re-wraps the text at a new width a little later, which moves
            // the verse; VersesLayout.SizeChanged scrolls again until then.
            _keepScrollTargetUntil = DateTime.UtcNow.AddSeconds(1.5);

            // Wait a moment so the new verses are laid out before scrolling.
            Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(150), () =>
            {
                ScrollToTarget(animate: true);
                _ = ScrollSidebarToCurrentBookAsync();
            });
        }

        // Switching projector mode changes the text width; keep the verse being read on screen.
        private void KeepCurrentVerseInView()
        {
            var centre = ReadingScroll.ScrollY + ReadingScroll.Height / 2 - VersesLayout.Y;
            for (var i = 0; i < VersesLayout.Children.Count; i++)
            {
                if (VersesLayout.Children[i] is VisualElement v && centre >= v.Y && centre <= v.Y + v.Height + VersesLayout.Spacing)
                {
                    _scrollTarget = i;
                    _keepScrollTargetUntil = DateTime.UtcNow.AddSeconds(1.5);
                    return;
                }
            }
        }

        private void VersesLayout_SizeChanged(object? sender, EventArgs e)
        {
            if (DateTime.UtcNow < _keepScrollTargetUntil)
                Dispatcher.Dispatch(() => ScrollToTarget(animate: false));
        }

        // Offsets are computed by hand because ScrollToAsync(element) ignores the offset of the
        // item's parent layout. Not awaited: on Windows ScrollToAsync never completes when the
        // position doesn't change.
        private void ScrollToTarget(bool animate)
        {
            if (_scrollTarget >= 0 && _scrollTarget < VersesLayout.Children.Count
                && VersesLayout.Children[_scrollTarget] is VisualElement verse)
            {
                var y = VersesLayout.Y + verse.Y - (ReadingScroll.Height - verse.Height) / 2;
                _ = ReadingScroll.ScrollToAsync(0, Math.Max(0, y), animate);
            }
            else
            {
                _ = ReadingScroll.ScrollToAsync(0, 0, false);
            }
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

        // ---- Search dropdown ---------------------------------------------------------------

        // The dropdown lives in the body row so it can overlay the reading column; line it up
        // with the search box in the top bar (whose grid starts at the page's left edge).
        private void AlignSuggestionsPanel()
        {
            SuggestionsPanel.Margin = new Thickness(SearchBox.X, 6, 0, 0);
            SuggestionsPanel.WidthRequest = Math.Min(Math.Max(SearchBox.Width, 420), Math.Max(0, Width - SearchBox.X - 16));
        }

        private void SearchEntry_Focused(object? sender, FocusEventArgs e) => _vm.ReopenSuggestions();

        // Close after a moment so a click on a suggestion still lands.
        private void SearchEntry_Unfocused(object? sender, FocusEventArgs e) =>
            Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(250), () =>
            {
                if (!SearchEntry.IsFocused) _vm.CloseSuggestions();
            });

        private void Suggestion_PointerEntered(object? sender, PointerEventArgs e)
        {
            if (sender is Border border) border.SetDynamicResource(BackgroundColorProperty, "ChipColor");
        }

        private void Suggestion_PointerExited(object? sender, PointerEventArgs e)
        {
            if (sender is Border border) border.BackgroundColor = Colors.Transparent;
        }

        // Esc closes one thing at a time: dropdown, note editor, selection, drawer or menu, then projector mode.
        private void OnEscape()
        {
            if (_vm.IsSuggestionsOpen) _vm.CloseSuggestions();
            else if (_vm.IsNoteEditorOpen) _vm.CloseNoteEditor();
            else if (_vm.HasSelection) _vm.ClearSelection();
            else if (!_vm.CloseOverlays()) _vm.ExitProjectorCommand.Execute(null);
        }

        // ---- Verse selection --------------------------------------------------------------

        private void Verse_Tapped(object? sender, TappedEventArgs e)
        {
            if ((sender as BindableObject)?.BindingContext is VerseItem verse)
                _vm.ToggleVerseSelection(verse, IsShiftDown());
        }

        private static bool IsShiftDown()
        {
#if WINDOWS
            return Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift)
                .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
#else
            return false;
#endif
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
            AddShortcut(element, Windows.System.VirtualKey.R, ctrl, () => _vm.ToggleWordsOfJesusCommand.Execute(null));
            AddShortcut(element, Windows.System.VirtualKey.K, ctrl, FocusSearch);
            AddShortcut(element, Windows.System.VirtualKey.F5, none, () => _vm.ToggleProjectorCommand.Execute(null));
            AddShortcut(element, Windows.System.VirtualKey.F11, none, () => _vm.ToggleProjectorCommand.Execute(null));
            AddShortcut(element, Windows.System.VirtualKey.Escape, none, OnEscape);
            AddShortcut(element, Windows.System.VirtualKey.C, ctrl, () => _vm.CopySelectionCommand.Execute(null), skipWhenTyping: true);

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
