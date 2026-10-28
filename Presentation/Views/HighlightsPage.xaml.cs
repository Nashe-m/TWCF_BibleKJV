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

        // Undo only applies while you're looking at the list.
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _vm.ClearUndo();
        }

        // Reload every time: notes and highlights are added in the reader.
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _vm.LoadAsync();
        }
    }
}
