using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using StormImages.Controls;
using StormImages.Models;
using StormImages.Services;
using StormImages.Themes;

namespace StormImages
{
    public partial class App : Application
    {
        private static Mutex? _mutex;
        private const string MutexName = @"Global\STORM_IMAGES_SingleInstanceMutex";

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_RESTORE = 9;

        protected override void OnStartup(StartupEventArgs e)
        {
            _mutex = new Mutex(true, MutexName, out bool createdNew);

            if (!createdNew)
            {
                // Another instance is already running
                BringExistingInstanceToFront();
                StormMessageBox.Show("Приложение STORM IMAGES уже запущено на этом компьютере.", "STORM IMAGES", MessageBoxButton.OK, MessageBoxImage.Information);
                Shutdown();
                return;
            }

            base.OnStartup(e);

            // Apply saved theme and language
            var settings = SettingsService.Instance.Settings;
            ThemeManager.Instance.ApplyTheme(settings.Theme);
            LocalizationManager.Instance.SetLanguage(settings.Language);

            if (settings.AutoStartBackend)
            {
                BackendService.Instance.StartLocalServer();
            }
        }

        private static void BringExistingInstanceToFront()
        {
            try
            {
                var current = Process.GetCurrentProcess();
                foreach (var process in Process.GetProcessesByName(current.ProcessName))
                {
                    if (process.Id != current.Id && process.MainWindowHandle != IntPtr.Zero)
                    {
                        ShowWindow(process.MainWindowHandle, SW_RESTORE);
                        SetForegroundWindow(process.MainWindowHandle);
                        break;
                    }
                }
            }
            catch { }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _mutex?.ReleaseMutex();
            _mutex?.Dispose();
            base.OnExit(e);
        }
    }
}