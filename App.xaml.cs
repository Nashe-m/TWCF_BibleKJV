using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using TWCF_BibleApp.Presentation.Services;
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
        private readonly AppSettings _settings;

        public App(AppSettings settings)
        {
            InitializeComponent();
            _settings = settings;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // Play the short opening animation, then hand the window to the shell.
            var intro = new IntroPage(_settings, () => new AppShell());
            return new Window(intro) { Title = "TWCF Bible" };
        }
    }

}