using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using TWCF_BibleApp.Presentation.ViewModels;

namespace TWCF_BibleApp.Presentation.Views
{
    public partial class SearchPage : ContentPage
    {
        // Parameterless constructor for Shell/XAML
        public SearchPage() : this(MauiProgram.Services.GetService<SearchViewModel>()!)
        {
        }

        // DI constructor
        public SearchPage(SearchViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm ?? throw new ArgumentNullException(nameof(vm));
        }

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
