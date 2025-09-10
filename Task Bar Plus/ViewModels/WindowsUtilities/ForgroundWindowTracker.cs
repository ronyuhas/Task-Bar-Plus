using System;
using System.Net;
using System.Runtime.InteropServices;
using System.Windows;


namespace TaskBarPlus.ViewModels.WindowsUtilities
{
    public class ForegroundWindowTracker : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern IntPtr SetWinEventHook(
            uint eventMin, uint eventMax, IntPtr hmodWinEventProc,
            WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern void UnhookWinEvent(IntPtr hWinEventHook);

        private delegate void WinEventDelegate(
            IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        private const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
        private const uint WINEVENT_OUTOFCONTEXT = 0;

        private IntPtr _winEventHook;
        private IntPtr _lastForegroundWindow = IntPtr.Zero;
        private readonly IntPtr _appWindowHandle;
        private WinEventDelegate? _procDelegate;

        public IntPtr LastForegroundWindow => _lastForegroundWindow;

        public ForegroundWindowTracker()
        {
            _appWindowHandle = new System.Windows.Interop.WindowInteropHelper(Application.Current.MainWindow).Handle;
            StartForegroundWindowTracking();
        }

        private void StartForegroundWindowTracking()
        {
            _procDelegate = new WinEventDelegate(WinEventProc);
            _winEventHook = SetWinEventHook(
                EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero,
                _procDelegate, 0, 0, WINEVENT_OUTOFCONTEXT);
        }

        private void WinEventProc(
            IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            // Ignore if hwnd is your own window
            if (hwnd != IntPtr.Zero && hwnd != _appWindowHandle)
            {
                _lastForegroundWindow = hwnd;
            }
        }

        private void StopForegroundWindowTracking()
        {
            if (_winEventHook != IntPtr.Zero)
                UnhookWinEvent(_winEventHook);
        }

        public void Dispose()
        {
            StopForegroundWindowTracking();
            GC.SuppressFinalize(this);
        }
    }
}