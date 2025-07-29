using System.Configuration;
using System.Data;
using System.Windows;
using TaskBarPlus;

namespace Task_Bar_Plus
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            // Load settings if needed
            int refreshRate = Settings.Default.RefreshRate;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            // Save settings
            Settings.Default.Save();
        }
    }
}
