using TWCF_BibleApp.Presentation.Views;

namespace TWCF_BibleApp
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(SearchPage), typeof(SearchPage));
        }
    }
}
