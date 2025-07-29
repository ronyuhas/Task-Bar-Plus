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
            if (RefreshRateComboBox.SelectedItem is ComboBoxItem selectedItem &&
            int.TryParse(selectedItem.Tag.ToString(), out int seconds))
            {
                // Use 'seconds' as the new refresh rate
                Settings.Default.RefreshRate = seconds;
                Settings.Default.Save();
            }
        }
        private void UtilityPopup_Opened(object sender, EventArgs e)
        {
            int savedRate = Settings.Default.RefreshRate;

            foreach (ComboBoxItem item in RefreshRateComboBox.Items)
            {
                if (int.TryParse(item.Tag?.ToString(), out int tagValue) && tagValue == savedRate)
                {
                    RefreshRateComboBox.SelectedItem = item;
                    break;
                }
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
