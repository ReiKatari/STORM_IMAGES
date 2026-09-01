using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using StormImages.ViewModels;

namespace StormImages.Views
{
    public partial class GenerationPage : UserControl
    {
        public GenerationPage()
        {
            InitializeComponent();
        }

        private void DropBorder_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0 && DataContext is GenerationViewModel vm)
                {
                    vm.LoadImage(files[0]);
                }
            }
        }

        private void DropBorder_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
                e.Handled = true;
            }
        }

        private void DropBorder_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is GenerationViewModel vm)
            {
                vm.SelectSourceImage();
            }
        }
    }
}