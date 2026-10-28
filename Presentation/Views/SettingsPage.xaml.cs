using System;
using Microsoft.Maui.Controls;
using TWCF_BibleApp.Presentation.ViewModels;

namespace TWCF_BibleApp.Presentation.Views
{
    public partial class SettingsPage : ContentPage
    {
        private readonly SettingsViewModel _vm;

        public SettingsPage(SettingsViewModel vm)
        {
            InitializeComponent();
            BindingContext = _vm = vm;
        }

        /// <summary>Set by F1 in the reader: open the page at the keyboard shortcuts.</summary>
        public static bool ScrollToShortcutsOnAppearing { get; set; }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _vm.LoadAsync();

            if (ScrollToShortcutsOnAppearing)
            {
                ScrollToShortcutsOnAppearing = false;
                Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(150),
                    () => _ = PageScroll.ScrollToAsync(ShortcutsCard, ScrollToPosition.Start, true));
            }
        }
    }
}
