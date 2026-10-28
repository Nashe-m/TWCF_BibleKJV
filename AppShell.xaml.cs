using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;

namespace TWCF_BibleApp
{
    public partial class AppShell : Shell
    {
        // Phones get bottom tabs; everything else gets the navigation rail.
        public static bool UsesTabs => DeviceInfo.Idiom == DeviceIdiom.Phone;

        public AppShell()
        {
            InitializeComponent();
            SetNavigationVisible(true);
        }

        /// <summary>Shows or hides the rail / tab bar. Hidden while projecting so only the text shows.</summary>
        public void SetNavigationVisible(bool visible)
        {
            FlyoutBehavior = visible && !UsesTabs ? FlyoutBehavior.Locked : FlyoutBehavior.Disabled;
            SetTabBarIsVisible(this, visible && UsesTabs);
            if (CurrentPage != null) SetTabBarIsVisible(CurrentPage, visible && UsesTabs);
        }
    }
}
