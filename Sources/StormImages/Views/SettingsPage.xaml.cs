using System.Windows;
using System.Windows.Controls;
using StormImages.Models;
using StormImages.Services;
using StormImages.ViewModels;

namespace StormImages.Views
{
    public partial class SettingsPage : UserControl
    {
        public SettingsPage()
        {
            InitializeComponent();
        }

        private void LangRu_Checked(object sender, RoutedEventArgs e) => SetLang("ru");
        private void LangEn_Checked(object sender, RoutedEventArgs e) => SetLang("en");
        private void LangDe_Checked(object sender, RoutedEventArgs e) => SetLang("de");
        private void LangFr_Checked(object sender, RoutedEventArgs e) => SetLang("fr");
        private void LangZh_Checked(object sender, RoutedEventArgs e) => SetLang("zh");
        private void LangJa_Checked(object sender, RoutedEventArgs e) => SetLang("ja");

        private void SetLang(string lang)
        {
            if (DataContext is SettingsViewModel vm)
            {
                vm.SelectedLanguage = lang;
            }
        }

        private void ThemeMidnight_Checked(object sender, RoutedEventArgs e) => SetTheme(ThemeType.StormMidnight);
        private void ThemeDark_Checked(object sender, RoutedEventArgs e) => SetTheme(ThemeType.StormDark);
        private void ThemeNight_Checked(object sender, RoutedEventArgs e) => SetTheme(ThemeType.StormNight);
        private void ThemeDay_Checked(object sender, RoutedEventArgs e) => SetTheme(ThemeType.StormDay);
        private void ThemeMatrix_Checked(object sender, RoutedEventArgs e) => SetTheme(ThemeType.StormMatrix);
        private void ThemeCyberpunk_Checked(object sender, RoutedEventArgs e) => SetTheme(ThemeType.StormCyberpunk);
        private void ThemeFantasy_Checked(object sender, RoutedEventArgs e) => SetTheme(ThemeType.StormFantasy);
        private void ThemeWarhammer_Checked(object sender, RoutedEventArgs e) => SetTheme(ThemeType.StormWarhammer);

        private void SetTheme(ThemeType theme)
        {
            if (DataContext is SettingsViewModel vm)
            {
                vm.SelectedTheme = theme;
            }
        }
    }
}