using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PottaKDS.Components
{
    public partial class CustomMessageBox : UserControl
    {
        public enum MessageBoxType
        {
            Confirmation,
            Warning,
            Error,
            Info,
            Success
        }

        public enum MessageBoxButtons
        {
            YesNo,
            OKCancel,
            OK
        }

        public bool? DialogResult { get; private set; }

        public CustomMessageBox()
        {
            InitializeComponent();
        }

        public static bool? Show(
            string message,
            string title = "Confirmation",
            MessageBoxType type = MessageBoxType.Confirmation,
            MessageBoxButtons buttons = MessageBoxButtons.YesNo,
            Window? owner = null)
        {
            var messageBox = new CustomMessageBox();
            messageBox.Configure(message, title, type, buttons);

            var ownerWindow = owner ?? Application.Current?.MainWindow;

            Point ownerScreenPos = new Point(100, 100);
            double ownerWidth = 800;
            double ownerHeight = 600;

            if (ownerWindow != null)
            {
                try
                {
                    ownerScreenPos = ownerWindow.PointToScreen(new Point(0, 0));
                    ownerWidth = ownerWindow.ActualWidth;
                    ownerHeight = ownerWindow.ActualHeight;
                }
                catch
                {
                    ownerScreenPos = new Point(ownerWindow.Left, ownerWindow.Top);
                }
            }

            var hostWindow = new Window
            {
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush(Color.FromArgb(140, 0, 0, 0)),
                AllowsTransparency = true,
                ShowInTaskbar = false,
                Topmost = true,
                Owner = ownerWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Width = ownerWidth > 0 ? ownerWidth : 800,
                Height = ownerHeight > 0 ? ownerHeight : 600
            };

            var rootGrid = new Grid { Background = Brushes.Transparent };
            messageBox.HorizontalAlignment = HorizontalAlignment.Center;
            messageBox.VerticalAlignment = VerticalAlignment.Center;
            rootGrid.Children.Add(messageBox);
            hostWindow.Content = rootGrid;

            hostWindow.ShowDialog();
            return messageBox.DialogResult;
        }

        private static readonly Dictionary<MessageBoxType, (string Circle, string Symbol, string Color, string HeaderHex, string TitleHex)> IconMap = new()
        {
            [MessageBoxType.Info] = (
                "M12 2C6.477 2 2 6.477 2 12s4.477 10 10 10 10-4.477 10-10S17.523 2 12 2z",
                "M12 8v4 M12 16h.01",
                "#1E40AF", "#DBEAFE", "#1E40AF"),

            [MessageBoxType.Success] = (
                "M12 2C6.477 2 2 6.477 2 12s4.477 10 10 10 10-4.477 10-10S17.523 2 12 2z",
                "M7.5 12.5l3 3 6-6",
                "#15803D", "#DCFCE7", "#15803D"),

            [MessageBoxType.Warning] = (
                "M10.29 3.86L1.82 18a2 2 0 001.71 3h16.94a2 2 0 001.71-3L13.71 3.86a2 2 0 00-3.42 0z",
                "M12 9v4 M12 17h.01",
                "#D97706", "#FEF3C7", "#92400E"),

            [MessageBoxType.Error] = (
                "M12 2C6.477 2 2 6.477 2 12s4.477 10 10 10 10-4.477 10-10S17.523 2 12 2z",
                "M15 9l-6 6 M9 9l6 6",
                "#DC2626", "#FEE2E2", "#991B1B"),

            [MessageBoxType.Confirmation] = (
                "M12 2C6.477 2 2 6.477 2 12s4.477 10 10 10 10-4.477 10-10S17.523 2 12 2z",
                "M9.09 9a3 3 0 015.83 1c0 2-3 3-3 3 M12 16h.01",
                "#1B5E20", "#F0FDF4", "#1B5E20"),
        };

        private void Configure(string message, string title, MessageBoxType type, MessageBoxButtons buttons)
        {
            MessageTextBlock.Text = message;
            TitleTextBlock.Text = title;

            var (circle, symbol, colorHex, headerHex, titleHex) = IconMap[type];
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex));

            IconCircle.Data = Geometry.Parse(circle);
            IconCircle.Fill = brush;

            IconSymbol.Data = Geometry.Parse(symbol);
            IconSymbol.Stroke = Brushes.White;

            HeaderBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(headerHex));
            TitleTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(titleHex));

            switch (buttons)
            {
                case MessageBoxButtons.YesNo:
                    CancelButton.Content = "No";
                    ConfirmButton.Content = "Yes";
                    CancelButton.Visibility = Visibility.Visible;
                    ConfirmButton.Visibility = Visibility.Visible;
                    break;

                case MessageBoxButtons.OKCancel:
                    CancelButton.Content = "Cancel";
                    ConfirmButton.Content = "OK";
                    CancelButton.Visibility = Visibility.Visible;
                    ConfirmButton.Visibility = Visibility.Visible;
                    break;

                case MessageBoxButtons.OK:
                    CancelButton.Visibility = Visibility.Collapsed;
                    ConfirmButton.Content = "OK";
                    ConfirmButton.Visibility = Visibility.Visible;
                    Grid.SetColumn(ConfirmButton, 2);
                    break;
            }
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            CloseDialog();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            CloseDialog();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = null;
            CloseDialog();
        }

        private void CloseDialog()
        {
            var window = Window.GetWindow(this);
            window?.Close();
        }
    }
}
