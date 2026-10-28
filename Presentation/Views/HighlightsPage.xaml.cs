using Microsoft.Maui.Controls;
using TWCF_BibleApp.Presentation.ViewModels;

namespace TWCF_BibleApp.Presentation.Views
{
    public partial class HighlightsPage : ContentPage
    {
        private readonly HighlightsViewModel _vm;

        public HighlightsPage(HighlightsViewModel vm)
        {
            InitializeComponent();
            BindingContext = _vm = vm;
        }

        // Reload every time: notes and highlights are added in the reader.
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _vm.LoadAsync();
        }
    }
}
