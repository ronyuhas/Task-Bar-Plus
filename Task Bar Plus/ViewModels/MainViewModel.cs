using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using TaskBarPlus.Models;
using TaskBarPlus.ViewModels.BaseClasses;

namespace TaskBarPlus.ViewModels
{
    class MainViewModel : BaseViewModel
    {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_RESTORE = 9;
        public ObservableCollection<ApplicationItem> AppItems { get; set; }
        public ICommand BringToFrontCommand { get; }

        private DispatcherTimer _refreshTimer;


        public MainViewModel()
        {
            AppItems = new ObservableCollection<ApplicationItem>();
            BringToFrontCommand = new RelayCommand(BringAppToFront);
            UpdateGrouping();
            Settings.Default.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(Settings.Default.IsGroupingEnabled))
                {
                    UpdateGrouping();
                    Settings.Default.Save();
                }
            };

            RefreshApplications();

            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(Settings.Default.RefreshRate)
            };
            _refreshTimer.Tick += (s, e) => RefreshApplications();
            _refreshTimer.Start();
            
        }

        public void UpdateRefreshRate(int newRateInSeconds)
        {
            if (_refreshTimer != null)
            {
                _refreshTimer.Stop();
                _refreshTimer.Interval = TimeSpan.FromSeconds(newRateInSeconds);
                _refreshTimer.Start();
            }

            // Optionally persist the new setting
            Settings.Default.RefreshRate = newRateInSeconds;
            Settings.Default.Save();
        }

        private void RefreshApplications()
        {
            var newItems = GetRunningApplications(); // Extracted from LoadRunningApplications
            var currentIds = AppItems.Select(a => a.ProcessId).ToHashSet();
            var currenthwnds = AppItems.Select(a => a.MainWindowHandle).ToHashSet();

            foreach (var item in newItems.Where(n => !currenthwnds.Contains(n.MainWindowHandle)))
                AppItems.Add(item);

            // Remove closed apps
            for (int i = AppItems.Count - 1; i >= 0; i--)
            {
                if (!newItems.Any(n => n.MainWindowHandle == AppItems[i].MainWindowHandle))
                    AppItems.RemoveAt(i);
            }

            OnPropertyChanged("ApplicationsByExecutable");
        }

        private void UpdateGrouping()
        {
            var view = CollectionViewSource.GetDefaultView(AppItems);
            if (view is ListCollectionView listView)
            {
                listView.GroupDescriptions.Clear();
                if (Settings.Default.IsGroupingEnabled)
                {
                    listView.GroupDescriptions.Add(new PropertyGroupDescription("ExecutablePath"));
                }
            }
        }

        private List<ApplicationItem> GetRunningApplications()
        {
            var appList = new List<ApplicationItem>();
            int currentProcessId = Environment.ProcessId;

            EnumWindows((hWnd, lParam) =>
            {
                if (!IsWindowVisible(hWnd)) return true;

                StringBuilder title = new StringBuilder(256);
                GetWindowText(hWnd, title, title.Capacity);
                string windowTitle = title.ToString();

                if (string.IsNullOrWhiteSpace(windowTitle) ||
                       windowTitle.CompareTo("Program Manager") == 0) return true;

                GetWindowThreadProcessId(hWnd, out uint processId);

                if ((int)processId == currentProcessId)
                    return true;

                try
                {
                    Process proc = Process.GetProcessById((int)processId);
                    string? exePath = proc.MainModule?.FileName;

                    if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                    {
                        Icon? icon = Icon.ExtractAssociatedIcon(exePath);
                        if (icon != null)
                        {
                            BitmapImage image = ConvertIconToImageSource(icon);
                            exePath = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(
                                Path.GetFileNameWithoutExtension(exePath).ToLower());

                            appList.Add(new ApplicationItem
                            {
                                Title = windowTitle,
                                Icon = image,
                                ProcessId = (int)processId,
                                MainWindowHandle = hWnd,
                                ExecutablePath = exePath
                            });
                        }
                    }
                }
                catch
                {
                    // Ignore inaccessible processes
                }

                return true;
            }, IntPtr.Zero);

            return appList.OrderBy(item => item.ExecutablePath).ToList();
        }

        private void BringAppToFront(object parameter)
        {
            if (parameter is ApplicationItem item && item.MainWindowHandle != IntPtr.Zero)
            {
                ShowWindow(item.MainWindowHandle, SW_RESTORE);
                SetForegroundWindow(item.MainWindowHandle);
            }
        }


        private BitmapImage ConvertIconToImageSource(Icon icon)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                // Fix: Ensure System.Drawing.Common is referenced in your project
                icon.ToBitmap().Save(ms, ImageFormat.Png); // Use ImageFormat from System.Drawing.Imaging
                ms.Seek(0, SeekOrigin.Begin);
                BitmapImage image = new BitmapImage();
                image.BeginInit();
                image.StreamSource = ms;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.EndInit();
                return image;
            }
        }

        // Win32 API declarations
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        public Dictionary<string, List<ApplicationItem>> ApplicationsByExecutable
        {
            get
            {
                // Group by executable path (Process.MainModule.FileName)
                return AppItems
                    .GroupBy(item =>
                    {
                        try
                        {
                            var proc = Process.GetProcessById(item.ProcessId);
                            return proc.MainModule?.FileName ?? "Unknown";
                        }
                        catch
                        {
                            return "Unknown";
                        }
                    })
                    .ToDictionary(g => g.Key, g => g.ToList());
            }
        }
    }
}