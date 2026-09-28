using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace PottaKDS.Models
{
    public class KitchenOrderItemModel : INotifyPropertyChanged
    {
        private string _productId = string.Empty;
        public string ProductId
        {
            get => _productId;
            set { _productId = value; OnPropertyChanged(); }
        }

        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        private int _quantity = 1;
        public int Quantity
        {
            get => _quantity;
            set { _quantity = value; OnPropertyChanged(); }
        }

        private decimal _price;
        public decimal Price
        {
            get => _price;
            set { _price = value; OnPropertyChanged(); }
        }

        private decimal _discount;
        public decimal Discount
        {
            get => _discount;
            set { _discount = value; OnPropertyChanged(); }
        }

        private decimal _total;
        public decimal Total
        {
            get => _total;
            set { _total = value; OnPropertyChanged(); }
        }

        private bool _isCompleted;
        public bool IsCompleted
        {
            get => _isCompleted;
            set 
            { 
                if (_isCompleted != value)
                {
                    _isCompleted = value; 
                    OnPropertyChanged(); 
                }
            }
        }

        private bool _isRefired;
        public bool IsRefired
        {
            get => _isRefired;
            set { _isRefired = value; OnPropertyChanged(); }
        }

        private List<AppliedModifierDto>? _appliedModifiers;
        public List<AppliedModifierDto>? AppliedModifiers
        {
            get => _appliedModifiers;
            set 
            { 
                _appliedModifiers = value; 
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasModifiers));
                OnPropertyChanged(nameof(ModifiersText));
            }
        }

        public bool HasModifiers => AppliedModifiers != null && AppliedModifiers.Count > 0;

        public string ModifiersText
        {
            get
            {
                if (AppliedModifiers == null || AppliedModifiers.Count == 0)
                    return string.Empty;

                return string.Join(", ", AppliedModifiers.Select(m => 
                    m.PriceChange > 0 
                        ? $"+ {m.ModifierName} ({m.PriceChange:N0})" 
                        : $"+ {m.ModifierName}"));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
