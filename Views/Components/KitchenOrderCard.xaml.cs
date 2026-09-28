using PottaKDS.Components;
using PottaKDS.Models;
using PottaKDS.ViewModels;
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
                vm.MarkOrderReadyCommand.Execute(order);
            }
        }

        private void DelayButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is KitchenOrder order &&
                Application.Current?.MainWindow?.DataContext is KdsDashboardViewModel vm)
            {
                vm.MarkOrderDelayedCommand.Execute(order);
            }
        }

        private void CompleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is KitchenOrder order &&
                Application.Current?.MainWindow?.DataContext is KdsDashboardViewModel vm)
            {
                var message = $"Are you sure you want to complete and clear {order.DisplayOrderId} from the kitchen display?";
                var result = CustomMessageBox.Show(
                    message, 
                    "Complete Order", 
                    CustomMessageBox.MessageBoxType.Confirmation, 
                    CustomMessageBox.MessageBoxButtons.YesNo,
                    Application.Current.MainWindow);

                if (result == true)
                {
                    vm.CompleteOrderCommand.Execute(order);
                }
            }
        }

        private void ItemCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox chk && chk.DataContext is KitchenOrderItemModel item &&
                DataContext is KitchenOrder order &&
                Application.Current?.MainWindow?.DataContext is KdsDashboardViewModel vm)
            {
                _ = vm.ToggleItemCompletionAsync(order, item);
            }
        }

        private void ItemRow_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement elem && elem.DataContext is KitchenOrderItemModel item &&
                DataContext is KitchenOrder order &&
                Application.Current?.MainWindow?.DataContext is KdsDashboardViewModel vm)
            {
                _ = vm.ToggleItemCompletionAsync(order, item);
            }
        }
    }
}
