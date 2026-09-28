using Microsoft.Extensions.DependencyInjection;
using PottaKDS.Models;
using PottaKDS.Services;
using PottaKDS.Services.Interfaces;
using PottaKDS.ViewModels;
using PottaKDS.Views.Dialogs;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PottaKDS
{
    public partial class MainWindow : Window
    {
        private readonly KdsDashboardViewModel _viewModel;
        private readonly IServiceProvider _serviceProvider;
        private WindowStyle _previousStyle = WindowStyle.SingleBorderWindow;
        private WindowState _previousState = WindowState.Normal;

        public MainWindow()
        {
            InitializeComponent();

            // Set up DI container
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            _viewModel = _serviceProvider.GetRequiredService<KdsDashboardViewModel>();
            DataContext = _viewModel;

            _viewModel.OpenSettingsRequested += ViewModel_OpenSettingsRequested;
            _viewModel.ToggleFullscreenRequested += (_, _) => ToggleFullscreen();
            _viewModel.OrdersCollectionChanged += (_, _) => ArrangeMasonryLayout();

            this.Loaded += MainWindow_Loaded;
            this.SizeChanged += MainWindow_SizeChanged;
            this.KeyDown += MainWindow_KeyDown;
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<ISettingsService, SettingsService>();
            services.AddSingleton<IKdsApiService, KdsApiService>();
            services.AddSingleton<ILanDiscoveryService, LanDiscoveryService>();
            services.AddSingleton<IAudioService, AudioService>();

            services.AddTransient<SettingsViewModel>();
            services.AddSingleton<KdsDashboardViewModel>();
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateColumnCount();
            await _viewModel.InitializeAsync();

            var settingsService = _serviceProvider.GetRequiredService<ISettingsService>();
            if (settingsService.CurrentSettings.FullscreenOnStartup)
            {
                ToggleFullscreen();
            }

            ArrangeMasonryLayout();
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.WidthChanged)
            {
                UpdateColumnCount();
                ArrangeMasonryLayout();
            }
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F11)
            {
                ToggleFullscreen();
                e.Handled = true;
            }
            else if (e.Key == Key.F5)
            {
                _ = _viewModel.LoadOrdersAsync(isManual: true);
                e.Handled = true;
            }
            else if (e.Key == Key.Escape && WindowStyle == WindowStyle.None)
            {
                ExitFullscreen();
                e.Handled = true;
            }
        }

        private void ToggleFullscreen()
        {
            if (WindowStyle == WindowStyle.None && WindowState == WindowState.Maximized)
            {
                ExitFullscreen();
            }
            else
            {
                EnterFullscreen();
            }
        }

        private void EnterFullscreen()
        {
            _previousStyle = WindowStyle;
            _previousState = WindowState;
            WindowStyle = WindowStyle.None;
            WindowState = WindowState.Maximized;
            _viewModel.IsFullscreen = true;
        }

        private void ExitFullscreen()
        {
            WindowStyle = _previousStyle;
            WindowState = _previousState;
            _viewModel.IsFullscreen = false;
        }

        private void ViewModel_OpenSettingsRequested(object? sender, EventArgs e)
        {
            var settingsVm = _serviceProvider.GetRequiredService<SettingsViewModel>();
            var dialog = new SettingsWindow(settingsVm)
            {
                Owner = this
            };

            if (dialog.ShowDialog() == true)
            {
                _viewModel.RestartPolling();
            }
        }

        #region Masonry Multi-Column Distribution

        private void UpdateColumnCount()
        {
            try
            {
                double containerWidth = this.ActualWidth;
                int columnCount = 4; // Default

                if (containerWidth < 800)
                    columnCount = 2;
                else if (containerWidth < 1200)
                    columnCount = 3;
                else if (containerWidth < 1650)
                    columnCount = 4;
                else
                    columnCount = 5;

                MasonryContainer.ColumnDefinitions.Clear();
                MasonryContainer.Children.Clear();

                for (int i = 0; i < columnCount; i++)
                {
                    MasonryContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                    var column = new StackPanel
                    {
                        Name = $"Column{i + 1}",
                        Margin = new Thickness(4)
                    };
                    Grid.SetColumn(column, i);
                    MasonryContainer.Children.Add(column);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating column count: {ex.Message}");
            }
        }

        private void ArrangeMasonryLayout()
        {
            try
            {
                var columns = MasonryContainer.Children.OfType<StackPanel>().ToArray();
                if (columns.Length == 0) return;

                foreach (var col in columns)
                {
                    col.Children.Clear();
                }

                double[] columnHeights = new double[columns.Length];
                var itemTemplate = HiddenTemplateHolder.ItemTemplate;

                foreach (var order in _viewModel.FilteredOrders)
                {
                    var presenter = new ContentPresenter
                    {
                        Content = order,
                        ContentTemplate = itemTemplate
                    };

                    // Find shortest column
                    int shortestIndex = 0;
                    for (int i = 1; i < columnHeights.Length; i++)
                    {
                        if (columnHeights[i] < columnHeights[shortestIndex])
                        {
                            shortestIndex = i;
                        }
                    }

                    columns[shortestIndex].Children.Add(presenter);

                    // Estimate card height for balanced column distribution
                    double estimatedHeight = 180 + (order.CartItems.Count * 28) + (order.IsRefired ? 80 : 0) + (order.HasNotes ? 30 : 0);
                    columnHeights[shortestIndex] += estimatedHeight;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error arranging masonry layout: {ex.Message}");
            }
        }

        #endregion
    }
}