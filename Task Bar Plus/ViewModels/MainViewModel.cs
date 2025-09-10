using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using TaskBarPlus.Models;
using TaskBarPlus.ViewModels.BaseClasses;
using TaskBarPlus.ViewModels.WindowsUtilities;

namespace TaskBarPlus.ViewModels
{
    class MainViewModel : BaseViewModel
    {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")]
        private static extern IntPtr GetTopWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        private const uint GW_HWNDNEXT = 2;


        [DllImport("user32.dll")]
        private static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        [StructLayout(LayoutKind.Sequential)]
        private struct WINDOWPLACEMENT
        {
            public int length;
            public int flags;
            public int showCmd;
            public Point ptMinPosition;
            public Point ptMaxPosition;
            public Rectangle rcNormalPosition;
        }

        private const int SW_SHOWMINIMIZED = 2;
        private const int SW_SHOWNORMAL = 1;
        private const int SW_SHOWMAXIMIZED = 3;


        private const int SW_MINIMIZE = 6;
        private const int SW_RESTORE = 9;
        public ObservableCollection<ApplicationItem> AppItems { get; set; }
        public ICommand BringToFrontCommand { get; }

        private DispatcherTimer _refreshTimer;

        public int IconSize => Settings.Default.IconSize;
        public int FontSize => Settings.Default.FontSize;
        public int RefreshRate => Settings.Default.RefreshRate;

        private readonly ForegroundWindowTracker _tracker;
        public MainViewModel(ForegroundWindowTracker tracker)
        {
            _tracker = tracker;
            AppItems = new ObservableCollection<ApplicationItem>();
            BringToFrontCommand = new RelayCommand(BringAppToFront);
            UpdateGrouping();
            Settings.Default.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(Settings.Default.IconSize))
                {
                    OnPropertyChanged(nameof(IconSize));
                }
                if (e.PropertyName == nameof(Settings.Default.FontSize))
                {
                    OnPropertyChanged(nameof(FontSize));
                }
                if (e.PropertyName == nameof(Settings.Default.IsGroupingEnabled))
                {
                    UpdateGrouping();
                    Settings.Default.Save();
                }
                if (e.PropertyName == nameof(Settings.Default.RefreshRate))
                {
                    UpdateRefreshRate(Settings.Default.RefreshRate);
                    OnPropertyChanged(nameof(RefreshRate));
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
        }

        private void RefreshApplications()
        {
            var newItems = GetRunningApplications(); // Extracted from LoadRunningApplications
            var currenthwnds = AppItems.Select(a => a.MainWindowHandle).ToHashSet();

            // Add new apps
            foreach (var item in newItems.Where(n => !currenthwnds.Contains(n.MainWindowHandle)))
                AppItems.Add(item);

            // Update existing apps if window title has changed
            foreach (var existingItem in AppItems)
            {
                var updatedItem = newItems.FirstOrDefault(n => n.MainWindowHandle == existingItem.MainWindowHandle);
                if (updatedItem != null && updatedItem.Title != existingItem.Title)
                {
                    existingItem.Title = updatedItem.Title;
                }
            }

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

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        private void BringAppToFront(object parameter)
        {
            if (parameter is ApplicationItem item && item.MainWindowHandle != IntPtr.Zero)
            {
                IntPtr? foregroundNullable = _tracker?.LastForegroundWindow;
                IntPtr foreground = foregroundNullable ?? IntPtr.Zero;
                if (foreground == item.MainWindowHandle)
                {
                    // If app is already focused, minimize it
                    ShowWindow(item.MainWindowHandle, SW_MINIMIZE);
                }
                else
                {
                    // Always maximize/restore and bring to front
                    WINDOWPLACEMENT placement = new WINDOWPLACEMENT();
                    placement.length = Marshal.SizeOf(placement);
                    if (GetWindowPlacement(item.MainWindowHandle, ref placement))
                    {
                        ShowWindow(item.MainWindowHandle, SW_RESTORE);

                        SetForegroundWindow(item.MainWindowHandle);
                    }
                }
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