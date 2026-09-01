using System;
using System.Windows;
using StormImages.Models;
using StormImages.Services;

namespace StormImages.Themes
{
    public class ThemeManager
    {
        private static ThemeManager? _instance;
        public static ThemeManager Instance => _instance ??= new ThemeManager();

        public event EventHandler<ThemeType>? ThemeChanged;

        public ThemeType CurrentTheme { get; private set; } = ThemeType.StormMidnight;

        private ThemeManager()
        {
            CurrentTheme = SettingsService.Instance.Settings.Theme;
        }

        public void ApplyTheme(ThemeType theme, Window? window = null)
        {
            CurrentTheme = theme;
            SettingsService.Instance.Settings.Theme = theme;
            SettingsService.Instance.Save();

            try
            {
                string themePath = theme switch
                {
                    ThemeType.StormMidnight => "Themes/StormMidnightTheme.xaml",
                    ThemeType.StormDark => "Themes/StormDarkTheme.xaml",
                    ThemeType.StormNight => "Themes/StormNightTheme.xaml",
                    ThemeType.StormDay => "Themes/StormDayTheme.xaml",
                    ThemeType.StormMatrix => "Themes/StormMatrixTheme.xaml",
                    ThemeType.StormCyberpunk => "Themes/StormCyberpunkTheme.xaml",
                    ThemeType.StormFantasy => "Themes/StormFantasyTheme.xaml",
                    ThemeType.StormWarhammer => "Themes/StormWarhammerTheme.xaml",
                    _ => "Themes/StormMidnightTheme.xaml"
                };

                var themeDict = new ResourceDictionary
                {
                    Source = new Uri(themePath, UriKind.RelativeOrAbsolute)
                };

                var iconsDict = new ResourceDictionary
                {
                    Source = new Uri("Themes/StormIcons.xaml", UriKind.RelativeOrAbsolute)
                };

                if (Application.Current != null)
                {
                    var merged = Application.Current.Resources.MergedDictionaries;
                    merged.Clear();
                    merged.Add(themeDict);
                    merged.Add(iconsDict);
                }

                if (window != null)
                {
                    var helper = new System.Windows.Interop.WindowInteropHelper(window);
                    bool isDark = theme != ThemeType.StormDay;
                    NativeMethods.SetWindowImmersiveDarkMode(helper.Handle, isDark);
                    NativeMethods.SetWindowCornerPreference(helper.Handle, 2);
                }
            }
            catch { }

            ThemeChanged?.Invoke(this, theme);
        }
    }
}