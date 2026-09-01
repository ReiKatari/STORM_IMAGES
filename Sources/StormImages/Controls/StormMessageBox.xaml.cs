using System;
using System.Windows;
using System.Windows.Media;
using StormImages.Services;
using StormImages.Themes;

namespace StormImages.Controls
{
    public partial class StormMessageBox : Window
    {
        public MessageBoxResult Result { get; private set; } = MessageBoxResult.None;

        public StormMessageBox(string message, string? title = null, MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.Information)
        {
            InitializeComponent();

            var loc = LocalizationManager.Instance;
            TxtTitle.Text = string.IsNullOrWhiteSpace(title) ? loc["AppTitle"] : title;
            TxtMessage.Text = message;

            BtnOk.Content = loc["BtnOk"];
            BtnCancel.Content = loc["BtnCancel"];
            BtnYes.Content = loc["BtnYes"];
            BtnNo.Content = loc["BtnNo"];

            // Configure Button Visibility
            switch (buttons)
            {
                case MessageBoxButton.OK:
                    BtnOk.Visibility = Visibility.Visible;
                    BtnCancel.Visibility = Visibility.Collapsed;
                    BtnYes.Visibility = Visibility.Collapsed;
                    BtnNo.Visibility = Visibility.Collapsed;
                    break;
                case MessageBoxButton.OKCancel:
                    BtnOk.Visibility = Visibility.Visible;
                    BtnCancel.Visibility = Visibility.Visible;
                    BtnYes.Visibility = Visibility.Collapsed;
                    BtnNo.Visibility = Visibility.Collapsed;
                    break;
                case MessageBoxButton.YesNo:
                    BtnOk.Visibility = Visibility.Collapsed;
                    BtnCancel.Visibility = Visibility.Collapsed;
                    BtnYes.Visibility = Visibility.Visible;
                    BtnNo.Visibility = Visibility.Visible;
                    break;
                case MessageBoxButton.YesNoCancel:
                    BtnOk.Visibility = Visibility.Collapsed;
                    BtnCancel.Visibility = Visibility.Visible;
                    BtnYes.Visibility = Visibility.Visible;
                    BtnNo.Visibility = Visibility.Visible;
                    break;
            }

            // Set Icon Geometry
            try
            {
                if (image == MessageBoxImage.Error)
                {
                    IconPath.Data = (Geometry)Application.Current.FindResource("GeoTrash");
                    IconPath.Fill = Brushes.Crimson;
                }
                else if (image == MessageBoxImage.Warning)
                {
                    IconPath.Data = (Geometry)Application.Current.FindResource("GeoSparkles");
                    IconPath.Fill = (Brush)Application.Current.FindResource("AccentPrimaryBrush");
                }
                else if (image == MessageBoxImage.Question)
                {
                    IconPath.Data = (Geometry)Application.Current.FindResource("GeoMagic");
                }
            }
            catch { }
        }

        public static MessageBoxResult Show(string message, string? title = null, MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.Information)
        {
            MessageBoxResult res = MessageBoxResult.None;
            if (Application.Current?.Dispatcher != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var dlg = new StormMessageBox(message, title, buttons, image);
                    if (Application.Current.MainWindow != null && Application.Current.MainWindow.IsLoaded)
                    {
                        dlg.Owner = Application.Current.MainWindow;
                    }
                    dlg.ShowDialog();
                    res = dlg.Result;
                });
            }
            return res;
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.OK;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.Cancel;
            Close();
        }

        private void BtnYes_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.Yes;
            Close();
        }

        private void BtnNo_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.No;
            Close();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.Cancel;
            Close();
        }
    }
}