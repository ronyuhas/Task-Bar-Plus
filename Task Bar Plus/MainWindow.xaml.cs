using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using TaskBarPlus.Models;
using TaskBarPlus.ViewModels;

namespace TaskBarPlus
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
            //AppListView.Items.GroupDescriptions.Add(new PropertyGroupDescription("ExecutablePath"));
        }
        private bool isResizing = false;
        private Point lastMousePosition;

        private void ResizeBorder_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                isResizing = true;
                lastMousePosition = e.GetPosition(this);
                Mouse.Capture((UIElement)sender);
            }
        }

        private void ResizeBorder_MouseMove(object sender, MouseEventArgs e)
        {
            if (isResizing)
            {
                Point currentPosition = e.GetPosition(this);
                double delta = currentPosition.X - lastMousePosition.X;
                double newWidth = this.Width + delta;

                if (newWidth >= this.MinWidth)
                {
                    this.Width = newWidth;
                    lastMousePosition = currentPosition;
                }
            }
        }

        private void ResizeBorder_MouseUp(object sender, MouseButtonEventArgs e)
        {
            isResizing = false;
            Mouse.Capture(null);
        }

        private void Window_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                e.Handled = true;
                int delta = e.Delta > 0 ? 1 : -1; // Adjust step size as needed
                int newSize = Math.Max(12, Math.Min(120, Settings.Default.IconSize + delta)); // Minimum size
                Settings.Default.IconSize = newSize;
                int newFontSize = Math.Max(10, Math.Min(118, Settings.Default.FontSize + delta)); // Minimum size
                Settings.Default.FontSize = newFontSize;
                Settings.Default.Save();
            }
        }

        private void AppListViewItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListViewItem item && item.DataContext is ApplicationItem appItem)
            {
                var viewModel = DataContext as MainViewModel;
                viewModel?.BringToFrontCommand.Execute(appItem);
            }
        }


        private void ListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (Settings.Default.RequireDoubleClick)
            {
                if (DataContext is MainViewModel vm && AppListView.SelectedItem is ApplicationItem item)
                {
                    if (vm.BringToFrontCommand.CanExecute(item))
                        vm.BringToFrontCommand.Execute(item);
                }
            }

        }

        private void ListView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (DataContext is MainViewModel vm && AppListView.SelectedItem is ApplicationItem item)
                {
                    if (vm.BringToFrontCommand.CanExecute(item))
                        vm.BringToFrontCommand.Execute(item);
                }
            }
        }

        private void AppListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!Settings.Default.RequireDoubleClick)
            {
                if (DataContext is MainViewModel vm && AppListView.SelectedItem is ApplicationItem item)
                {
                    if (vm.BringToFrontCommand.CanExecute(item))
                        vm.BringToFrontCommand.Execute(item);
                }
            }
        }
    }
}