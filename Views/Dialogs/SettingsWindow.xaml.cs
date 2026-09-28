using PottaKDS.ViewModels;
using System;
using System.Windows;

namespace PottaKDS.Views.Dialogs
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow(SettingsViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            viewModel.RequestClose += (_, _) =>
            {
                DialogResult = true;
                Close();
            };
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
