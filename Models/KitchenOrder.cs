using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace PottaKDS.Models
{
    public class KitchenOrder : INotifyPropertyChanged
    {
        private string _transactionId = string.Empty;
        public string TransactionId
        {
            get => _transactionId;
            set 
            { 
                _transactionId = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(DisplayOrderId));
                OnPropertyChanged(nameof(DisplayTransactionId));
            }
        }

        private string? _tableId;
        public string? TableId
        {
            get => _tableId;
            set { _tableId = value; OnPropertyChanged(); }
        }

        private int? _tableNumber;
        public int? TableNumber
        {
            get => _tableNumber;
            set 
            { 
                _tableNumber = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(DisplayOrderId)); 
                OnPropertyChanged(nameof(DisplayTableInfo)); 
            }
        }

        private string? _tableName;
        public string? TableName
        {
            get => _tableName;
            set 
            { 
                _tableName = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(DisplayTableInfo)); 
            }
        }

        private ObservableCollection<KitchenOrderItemModel> _cartItems = new();
        public ObservableCollection<KitchenOrderItemModel> CartItems
        {
            get => _cartItems;
            set
            {
                if (_cartItems != null)
                {
                    _cartItems.CollectionChanged -= CartItems_CollectionChanged;
                    foreach (var item in _cartItems)
                    {
                        item.PropertyChanged -= Item_PropertyChanged;
                    }
                }

                _cartItems = value ?? new ObservableCollection<KitchenOrderItemModel>();
                _cartItems.CollectionChanged += CartItems_CollectionChanged;

                foreach (var item in _cartItems)
                {
                    item.PropertyChanged += Item_PropertyChanged;
                }

                OnPropertyChanged();
                NotifyItemAggregates();
            }
        }

        private DateTime _createdDate = DateTime.Now;
        public DateTime CreatedDate
        {
            get => _createdDate;
            set
            {
                _createdDate = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayOrderTime));
                OnPropertyChanged(nameof(WaitingTime));
                OnPropertyChanged(nameof(IsUrgent));
            }
        }

        private string _status = "Pending";
        public string Status
        {
            get => _status;
            set 
            { 
                _status = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(StatusBrushKey));
            }
        }

        private string? _notes;
        public string? Notes
        {
            get => _notes;
            set 
            { 
                _notes = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(HasNotes));
            }
        }

        public bool HasNotes => !string.IsNullOrWhiteSpace(Notes);

        private int? _staffId;
        public int? StaffId
        {
            get => _staffId;
            set { _staffId = value; OnPropertyChanged(); }
        }

        // Refire properties
        private bool _isRefired;
        public bool IsRefired
        {
            get => _isRefired;
            set 
            { 
                _isRefired = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(DisplayRefireTime)); 
            }
        }

        private string? _refireReason;
        public string? RefireReason
        {
            get => _refireReason;
            set { _refireReason = value; OnPropertyChanged(); }
        }

        private DateTime? _refiredAt;
        public DateTime? RefiredAt
        {
            get => _refiredAt;
            set 
            { 
                _refiredAt = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(DisplayRefireTime)); 
            }
        }

        private string? _refiredByStaffName;
        public string? RefiredByStaffName
        {
            get => _refiredByStaffName;
            set { _refiredByStaffName = value; OnPropertyChanged(); }
        }

        private List<int> _refiredItemIndices = new();
        public List<int> RefiredItemIndices
        {
            get => _refiredItemIndices;
            set { _refiredItemIndices = value; OnPropertyChanged(); }
        }

        // Computed display properties
        public string DisplayOrderId => TableNumber.HasValue 
            ? $"Table #{TableNumber}" 
            : (!string.IsNullOrEmpty(TableName) ? TableName : $"Order #{TransactionId}");

        public string DisplayTransactionId => $"ID: {TransactionId}";

        public string DisplayTableInfo => !string.IsNullOrEmpty(TableName) 
            ? TableName 
            : (TableNumber.HasValue ? $"Table {TableNumber}" : "Takeaway / Direct");

        public string DisplayOrderTime => CreatedDate.ToString("HH:mm:ss (dd/MM)");

        public string DisplayRefireTime
        {
            get
            {
                if (!IsRefired || !RefiredAt.HasValue) return string.Empty;
                
                var elapsed = DateTime.Now - RefiredAt.Value;
                
                // Less than 1 minute
                if (elapsed.TotalSeconds < 60)
                    return "Refired just now";
                
                // Less than 1 hour - show minutes
                if (elapsed.TotalMinutes < 60)
                {
                    int minutes = (int)elapsed.TotalMinutes;
                    return $"Refired {minutes} min{(minutes != 1 ? "s" : "")} ago";
                }
                
                // Less than 24 hours - show hours and minutes
                if (elapsed.TotalHours < 24)
                {
                    int hours = (int)elapsed.TotalHours;
                    int minutes = elapsed.Minutes;
                    
                    if (minutes == 0)
                        return $"Refired {hours} hr{(hours != 1 ? "s" : "")} ago";
                    
                    return $"Refired {hours}h {minutes}m ago";
                }
                
                // 24 hours or more - show days
                int days = (int)elapsed.TotalDays;
                int remainingHours = elapsed.Hours;
                
                if (remainingHours == 0)
                    return $"Refired {days} day{(days != 1 ? "s" : "")} ago";
                
                return $"Refired {days}d {remainingHours}h ago";
            }
        }

        public string WaitingTime
        {
            get
            {
                var elapsed = DateTime.Now - CreatedDate;
                
                // Less than 1 minute
                if (elapsed.TotalSeconds < 60)
                    return "Just now";
                
                // Less than 1 hour - show minutes
                if (elapsed.TotalMinutes < 60)
                {
                    int minutes = (int)elapsed.TotalMinutes;
                    return $"{minutes} min{(minutes != 1 ? "s" : "")}";
                }
                
                // Less than 24 hours - show hours and minutes
                if (elapsed.TotalHours < 24)
                {
                    int hours = (int)elapsed.TotalHours;
                    int minutes = elapsed.Minutes;
                    
                    if (minutes == 0)
                        return $"{hours} hr{(hours != 1 ? "s" : "")}";
                    
                    return $"{hours}h {minutes}m";
                }
                
                // 24 hours or more - show days and hours
                int days = (int)elapsed.TotalDays;
                int remainingHours = elapsed.Hours;
                
                if (remainingHours == 0)
                    return $"{days} day{(days != 1 ? "s" : "")}";
                
                return $"{days}d {remainingHours}h";
            }
        }

        public bool IsUrgent
        {
            get
            {
                var elapsed = DateTime.Now - CreatedDate;
                return elapsed.TotalMinutes >= 15; // Highlight in amber/red if waiting > 15 mins
            }
        }

        public int TotalQuantity => CartItems?.Sum(i => i.Quantity) ?? 0;

        public decimal TotalAmount => CartItems?.Sum(i => i.Total) ?? 0;

        public string DisplayTotalAmount => $"XAF {TotalAmount:N0}";

        public bool AllItemsCompleted => CartItems != null && CartItems.Count > 0 && CartItems.All(i => i.IsCompleted);

        public string CompletionProgress
        {
            get
            {
                if (CartItems == null || CartItems.Count == 0) return "0/0";
                int completed = CartItems.Count(i => i.IsCompleted);
                return $"{completed}/{CartItems.Count} items completed";
            }
        }

        public string StatusBrushKey => Status switch
        {
            "Pending" => "WarningBrush",
            "Delayed" => "DangerBrush",
            "Cancelled" => "DangerBrush",
            "Ready" => "PrimaryBrush",
            "Completed" => "AccentBrush",
            _ => "TextSecondaryBrush"
        };

        public KitchenOrder()
        {
            _cartItems.CollectionChanged += CartItems_CollectionChanged;
        }

        public void UpdateWaitingTime()
        {
            OnPropertyChanged(nameof(WaitingTime));
            OnPropertyChanged(nameof(IsUrgent));
            OnPropertyChanged(nameof(DisplayRefireTime));
        }

        private void CartItems_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (KitchenOrderItemModel item in e.NewItems)
                {
                    item.PropertyChanged += Item_PropertyChanged;
                }
            }

            if (e.OldItems != null)
            {
                foreach (KitchenOrderItemModel item in e.OldItems)
                {
                    item.PropertyChanged -= Item_PropertyChanged;
                }
            }

            NotifyItemAggregates();
        }

        private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(KitchenOrderItemModel.IsCompleted) ||
                e.PropertyName == nameof(KitchenOrderItemModel.Quantity) ||
                e.PropertyName == nameof(KitchenOrderItemModel.Total))
            {
                NotifyItemAggregates();
            }
        }

        private void NotifyItemAggregates()
        {
            OnPropertyChanged(nameof(TotalQuantity));
            OnPropertyChanged(nameof(TotalAmount));
            OnPropertyChanged(nameof(DisplayTotalAmount));
            OnPropertyChanged(nameof(AllItemsCompleted));
            OnPropertyChanged(nameof(CompletionProgress));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
