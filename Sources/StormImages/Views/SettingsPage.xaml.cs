using System.Windows;
using System.Windows.Controls;
using StormImages.Models;
using StormImages.Themes;
using StormImages.ViewModels;

namespace StormImages.Views
{
    public partial class SettingsPage : UserControl
    {
        public SettingsPage()
        {
            InitializeComponent();
        }

        private void ThemeMidnight_Checked(object sender, RoutedEventArgs e)
        {
            if (DataContext is SettingsViewModel vm)
            {
                vm.SelectedTheme = ThemeType.StormMidnight;
            }
        }

        private void ThemeNight_Checked(object sender, RoutedEventArgs e)
        {
            if (DataContext is SettingsViewModel vm)
            {
                vm.SelectedTheme = ThemeType.StormNight;
            }
        }

        private void ThemeDay_Checked(object sender, RoutedEventArgs e)
        {
            if (DataContext is SettingsViewModel vm)
            {
                vm.SelectedTheme = ThemeType.StormDay;
            }
        }
    }
}