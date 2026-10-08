using PottaKDS.Components;
using PottaKDS.Models;
using PottaKDS.ViewModels;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PottaKDS.Views.Components
{
    public partial class KitchenOrderCard : UserControl
    {
        public KitchenOrderCard()
        {
            InitializeComponent();
        }

        private void DoneButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is KitchenOrder order &&
                Application.Current?.MainWindow?.DataContext is KdsDashboardViewModel vm)
            {
                // Check if already marked as Ready
                if (order.Status == "Ready")
                {
                    CustomMessageBox.Show(
                        $"{order.DisplayOrderId} is already marked as Ready.",
                        "Already Ready",
                        CustomMessageBox.MessageBoxType.Info,
                        CustomMessageBox.MessageBoxButtons.OK,
                        Application.Current.MainWindow);
                    return;
                }

                var message = $"Are you sure you want to mark {order.DisplayOrderId} as Ready?\n\nThis will mark all items as completed.";
                var result = CustomMessageBox.Show(
                    message,
                    "Mark Order Ready",
                    CustomMessageBox.MessageBoxType.Confirmation,
                    CustomMessageBox.MessageBoxButtons.YesNo,
                    Application.Current.MainWindow);
                
                if (result == true)
                {
                    // Disable the button to prevent double-clicking
                    if (sender is Button btn)
                    {
                        btn.IsEnabled = false;
                    }

                    vm.MarkOrderReadyCommand.Execute(order);

                    // Re-enable after a short delay
                    if (sender is Button button)
                    {
                        Task.Delay(1000).ContinueWith(_ =>
                        {
                            Application.Current?.Dispatcher?.Invoke(() =>
                            {
                                button.IsEnabled = true;
                            });
                        });
                    }
                }
            }
        }

        private void DelayButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is KitchenOrder order &&
                Application.Current?.MainWindow?.DataContext is KdsDashboardViewModel vm)
            {
                // Check if already marked as Delayed
                if (order.Status == "Delayed")
                {
                    CustomMessageBox.Show(
                        $"{order.DisplayOrderId} is already marked as Delayed.",
                        "Already Delayed",
                        CustomMessageBox.MessageBoxType.Info,
                        CustomMessageBox.MessageBoxButtons.OK,
                        Application.Current.MainWindow);
                    return;
                }

                var message = $"Are you sure you want to mark {order.DisplayOrderId} as Delayed?\n\nThis indicates the order will take longer than expected.";
                var result = CustomMessageBox.Show(
                    message,
                    "Mark Order Delayed",
                    CustomMessageBox.MessageBoxType.Warning,
                    CustomMessageBox.MessageBoxButtons.YesNo,
                    Application.Current.MainWindow);

                if (result == true)
                {
                    // Disable the button to prevent double-clicking
                    if (sender is Button btn)
                    {
                        btn.IsEnabled = false;
                    }

                    vm.MarkOrderDelayedCommand.Execute(order);

                    // Re-enable after a short delay
                    if (sender is Button button)
                    {
                        Task.Delay(1000).ContinueWith(_ =>
                        {
                            Application.Current?.Dispatcher?.Invoke(() =>
                            {
                                button.IsEnabled = true;
                            });
                        });
                    }
                }
            }
        }

        private void CompleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is KitchenOrder order &&
                Application.Current?.MainWindow?.DataContext is KdsDashboardViewModel vm)
            {
                var message = $"Are you sure you want to complete and clear {order.DisplayOrderId} from the kitchen display?\n\nThis will remove it from the screen.";
                var result = CustomMessageBox.Show(
                    message, 
                    "Complete Order", 
                    CustomMessageBox.MessageBoxType.Confirmation, 
                    CustomMessageBox.MessageBoxButtons.YesNo,
                    Application.Current.MainWindow);

                if (result == true)
                {
                    // Disable the button to prevent double-clicking
                    if (sender is Button btn)
                    {
                        btn.IsEnabled = false;
                    }

                    vm.CompleteOrderCommand.Execute(order);

                    // Re-enable after a short delay (in case the action fails)
                    if (sender is Button button)
                    {
                        Task.Delay(1000).ContinueWith(_ =>
                        {
                            Application.Current?.Dispatcher?.Invoke(() =>
                            {
                                button.IsEnabled = true;
                            });
                        });
                    }
                }
            }
        }

        private void ItemCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox chk && chk.DataContext is KitchenOrderItemModel item &&
                DataContext is KitchenOrder order &&
                Application.Current?.MainWindow?.DataContext is KdsDashboardViewModel vm)
            {
                // Prevent MouseButtonUp from also firing (stops double-toggle)
                e.Handled = true;
                
                _ = vm.ToggleItemCompletionAsync(order, item);
            }
        }

        private void ItemRow_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Check if click originated from CheckBox - if so, ignore (let checkbox handle it)
            if (e.OriginalSource is FrameworkElement elem)
            {
                // Walk up visual tree to check if we're inside a CheckBox
                DependencyObject parent = elem;
                while (parent != null)
                {
                    if (parent is CheckBox)
                    {
                        // Click originated from checkbox - let checkbox handle it exclusively
                        e.Handled = true;
                        return;
                    }
                    parent = System.Windows.Media.VisualTreeHelper.GetParent(parent);
                }
            }

            // Click originated from item row (not checkbox) - toggle completion
            if (sender is FrameworkElement rowElem && rowElem.DataContext is KitchenOrderItemModel item &&
                DataContext is KitchenOrder order &&
                Application.Current?.MainWindow?.DataContext is KdsDashboardViewModel vm)
            {
                _ = vm.ToggleItemCompletionAsync(order, item);
            }
        }
    }
}
