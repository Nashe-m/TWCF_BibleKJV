using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using TWCF_BibleApp.Presentation.ViewModels;

namespace TWCF_BibleApp.Presentation.Views
{
    public partial class SearchPage : ContentPage
    {
        private readonly SearchViewModel _vm;

        // Parameterless constructor for Shell/XAML
        public SearchPage() : this(MauiProgram.Services.GetService<SearchViewModel>()!)
        {
        }

        // DI constructor
        public SearchPage(SearchViewModel vm)
        {
            InitializeComponent();
            BindingContext = _vm = vm ?? throw new ArgumentNullException(nameof(vm));
            // New results start at the top (the filters live in the list's header).
            _vm.ScrollToTopRequested += (_, _) =>
            {
                if (_vm.SearchResults.Count > 0)
                    ResultsList.ScrollTo(0, position: ScrollToPosition.Start, animate: false);
            };
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _vm.EnsureLoadedAsync();
            if (!_vm.HasSearched) QueryEntry.Focus();
        }

        private void WordForms_Tapped(object? sender, TappedEventArgs e) => _vm.WordForms = !_vm.WordForms;

        private void Result_PointerEntered(object? sender, PointerEventArgs e)
        {
            if (sender is Border border) border.SetDynamicResource(BackgroundColorProperty, "ChipColor");
        }

        private void Result_PointerExited(object? sender, PointerEventArgs e)
        {
            if (sender is Border border) border.BackgroundColor = Colors.Transparent;
        }
    }
}
