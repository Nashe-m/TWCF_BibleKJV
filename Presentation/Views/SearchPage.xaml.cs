using Microsoft.Maui.Controls;
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
            _vm = vm ?? throw new ArgumentNullException(nameof(vm));
            InitializeComponent();
            BindingContext = _vm;
        }

        private void OnSearchCompleted(object sender, EventArgs e)
        {
            _vm.PerformSearchCommand.Execute(null);
        }
    }
}