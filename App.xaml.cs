using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using TWCF_BibleApp.Presentation.Views;

namespace TWCF_BibleApp
{
    public partial class App : Microsoft.Maui.Controls.Application
    {
        /*private readonly ReaderPage _readerPage;
        public App(ReaderPage readerPage)
        {
            InitializeComponent();
            _readerPage = readerPage;
        }

        protected override Microsoft.Maui.Controls.Window CreateWindow(IActivationState? activationState)
        {
            //return new Microsoft.Maui.Controls.Window(new AppShell());
            return new Window(_readerPage);
        }
    }*/
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell()) { Title = "TWCF Bible" };
        }
    }

}