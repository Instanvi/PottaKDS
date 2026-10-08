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

            var ownerWindow = owner ?? Application.Current.MainWindow;

            Point ownerScreenPos;
            try
            {
                ownerScreenPos = ownerWindow.PointToScreen(new Point(0, 0));
            }
            catch (InvalidOperationException)
            {

                ownerScreenPos = new Point(ownerWindow.Left, ownerWindow.Top);
            }

            var hostWindow = new Window
            {
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush(Color.FromArgb(150, 0, 0, 0)),
                AllowsTransparency = true,
                ShowInTaskbar = false,
                Topmost = true,
                Owner = ownerWindow,
                WindowStartupLocation = WindowStartupLocation.Manual,
                Width = ownerWindow.ActualWidth,
                Height = ownerWindow.ActualHeight,
                Left = ownerScreenPos.X,
                Top = ownerScreenPos.Y
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
                "#065F46", "#D1FAE5", "#065F46"),

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
                "#374151", "#F8FAFC", "#111827"),
        };

        private void Configure(string message, string title, MessageBoxType type, MessageBoxButtons buttons)
        {
            MessageTextBlock.Text = message;
            TitleTextBlock.Text = title;

            var (circle, symbol, colorHex, headerHex, titleHex) = IconMap[type];
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex));

            // Solid filled disc
            IconCircle.Data = Geometry.Parse(circle);
            IconCircle.Fill = brush;
            IconCircle.Stroke = Brushes.Transparent;
            IconCircle.StrokeThickness = 0;

            // White symbol on top
            IconSymbol.Data = Geometry.Parse(symbol);
            IconSymbol.Stroke = Brushes.White;
            IconSymbol.Fill = Brushes.Transparent;
            IconSymbol.StrokeThickness = 2;
            IconSymbol.StrokeLineJoin = PenLineJoin.Round;
            IconSymbol.StrokeStartLineCap = PenLineCap.Round;
            IconSymbol.StrokeEndLineCap = PenLineCap.Round;

            // Header colors
            HeaderBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(headerHex));
            TitleTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(titleHex));

            // Buttons
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
                    ConfirmButton.SetValue(Grid.ColumnProperty, 2);
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