using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TaskBarPlus.Models;

namespace TaskBarPlus.ViewModels.WindowsUtilities
{
    class AppData
    {
        //[DllImport("user32.dll")]
        //public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        //public static List<ApplicationItem> GetOpenApplications()
        //{
        //    List<ApplicationItem> applications = new List<ApplicationItem>();
        //    EnumWindows((hWnd, lParam) =>
        //    {
        //        if (IsWindowVisible(hWnd))
        //        {
        //            StringBuilder windowText = new StringBuilder(256);
        //            GetWindowText(hWnd, windowText, windowText.Capacity);
        //            if (windowText.Length > 0)
        //            {
        //                uint processId;
        //                GetWindowThreadProcessId(hWnd, out processId);
        //                // Here you would typically retrieve the icon for the application using the processId
        //                // For simplicity, we will use a placeholder icon
        //                applications.Add(new ApplicationItem { Title = windowText.ToString(), Icon = new BitmapImage(new Uri("pack://application:,,,/Resources/placeholder.png")) });
        //            }
        //        }
        //        return true;
        //    }, IntPtr.Zero);
        //    return applications;
        //}

    }
}
