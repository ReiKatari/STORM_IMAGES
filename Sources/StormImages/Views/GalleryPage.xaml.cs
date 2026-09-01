using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using StormImages.Models;
using StormImages.ViewModels;

namespace StormImages.Views
{
    public partial class GalleryPage : UserControl
    {
        public GalleryPage()
        {
            InitializeComponent();
        }

        private void GalleryItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement elem && elem.DataContext is GenerationHistoryItem item)
            {
                if (DataContext is GalleryViewModel vm)
                {
                    vm.SelectedItem = item;
                }
            }
        }
    }
}