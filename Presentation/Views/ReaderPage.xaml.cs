using Microsoft.Maui.Controls;
using TWCF_BibleApp.Presentation.ViewModels;
using TWCF_BibleApp.Application.DTOs;
using System;
using System.Linq;

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
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _vm.InitializeAsync();
        }

        private void BookEntry_Focused(object sender, FocusEventArgs e)
        {
            _vm.ShowBookDropdown();
        }

        private void ChapterEntry_Focused(object sender, FocusEventArgs e)
        {
            _vm.ShowChapterDropdown();
        }

        private async void BookEntry_Completed(object sender, EventArgs e)
        {
            await _vm.ConfirmBookSelectionAsync();
        }

        private async void ChapterEntry_Completed(object sender, EventArgs e)
        {
            await _vm.ConfirmChapterSelectionAsync();
        }

        private void BookCollection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is BookDto book)
            {
                _vm.SelectBookFromDropdown(book);
            }
        }

        private void ChapterCollection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is int chapter)
            {
                _vm.SelectChapterFromDropdown(chapter);
            }
        }
    }
}