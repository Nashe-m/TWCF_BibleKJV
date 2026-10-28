using Microsoft.Maui.Controls;
using TWCF_BibleApp.Presentation.ViewModels;

namespace TWCF_BibleApp.Presentation.Views
{
    public partial class NotesPage : ContentPage
    {
        private readonly NotesViewModel _vm;

        public NotesPage(NotesViewModel vm)
        {
            InitializeComponent();
            BindingContext = _vm = vm;
        }

        // Put the cursor in the note as soon as Edit is pressed.
        private void NoteEditor_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IsVisible) && sender is Editor { IsVisible: true } editor)
                Dispatcher.DispatchDelayed(System.TimeSpan.FromMilliseconds(50), () =>
                {
                    editor.Focus();
                    editor.CursorPosition = editor.Text?.Length ?? 0;
                });
        }

        // Reload every time: notes and highlights are added in the reader.
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _vm.LoadAsync();
        }
    }
}
