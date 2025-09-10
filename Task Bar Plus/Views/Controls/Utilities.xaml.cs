using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TaskBarPlus.Views.Controls
{
    /// <summary>
    /// Interaction logic for Utilities.xaml
    /// </summary>
    public partial class Utilities : UserControl
    {
        public Utilities()
        {
            InitializeComponent();
            DataContext = new ViewModels.UtilitiesViewModel();
        }

        private void UserButton_Click(object sender, RoutedEventArgs e)
        {
            UtilityPopup.IsOpen = !UtilityPopup.IsOpen;
        }

        private void RefreshRateComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Settings.Default.Save();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.Save();
            Application.Current.Shutdown();
        }
    }
}
