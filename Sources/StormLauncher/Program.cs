using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace StormLauncher
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string targetExe = Path.Combine(appDir, "StormImages.exe");

            // 1. Try launching elevated via Task Scheduler (Instant, 0 UAC prompts)
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "schtasks.exe",
                    Arguments = "/run /tn \"STORM_IMAGES\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                using var p = Process.Start(psi);
                p?.WaitForExit(2000);

                Thread.Sleep(300);
                if (Process.GetProcessesByName("StormImages").Length > 0)
                {
                    return;
                }
            }
            catch { }

            // 2. Direct fallback launch
            try
            {
                if (File.Exists(targetExe))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = targetExe,
                        WorkingDirectory = appDir,
                        UseShellExecute = true
                    });
                }
            }
            catch { }
        }
    }
}