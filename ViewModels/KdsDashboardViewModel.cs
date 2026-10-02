using PottaKDS.Models;
using PottaKDS.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace PottaKDS.ViewModels
{
    public class KdsDashboardViewModel : BaseViewModel
    {
        private readonly IKdsApiService _apiService;
        private readonly ISettingsService _settingsService;
        private readonly ILanDiscoveryService _lanDiscovery;
        private readonly IAudioService _audioService;

        private DispatcherTimer? _pollTimer;
        private DispatcherTimer? _clockTimer;
        private readonly HashSet<string> _knownOrderIds = new();
        private readonly HashSet<string> _knownRefiredOrderIds = new();

        #region Observables

        public ObservableCollection<KitchenOrder> KitchenOrders { get; } = new();
        public ObservableCollection<KitchenOrder> FilteredOrders { get; } = new();

        private string _selectedFilter = "All";
        public string SelectedFilter
        {
            get => _selectedFilter;
            set
            {
                if (SetField(ref _selectedFilter, value))
                {
                    ApplyFilter();
                    OnPropertyChanged(nameof(IsFilterAll));
                    OnPropertyChanged(nameof(IsFilterPending));
                    OnPropertyChanged(nameof(IsFilterDelayed));
                    OnPropertyChanged(nameof(IsFilterReady));
                }
            }
        }

        public bool IsFilterAll
        {
            get => SelectedFilter == "All";
            set { if (value) SelectedFilter = "All"; }
        }

        public bool IsFilterPending
        {
            get => SelectedFilter == "Pending";
            set { if (value) SelectedFilter = "Pending"; }
        }

        public bool IsFilterDelayed
        {
            get => SelectedFilter == "Delayed";
            set { if (value) SelectedFilter = "Delayed"; }
        }

        public bool IsFilterReady
        {
            get => SelectedFilter == "Ready";
            set { if (value) SelectedFilter = "Ready"; }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetField(ref _isLoading, value);
        }

        private bool _isConnected;
        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                if (SetField(ref _isConnected, value))
                {
                    OnPropertyChanged(nameof(ConnectionStatusText));
                    OnPropertyChanged(nameof(ConnectionStatusBrush));
                }
            }
        }

        public string ConnectionStatusText => IsConnected 
            ? $"Connected ({_settingsService.CurrentSettings.BaseUrl})" 
            : $"Connecting to {_settingsService.CurrentSettings.BaseUrl}...";

        public string ConnectionStatusBrush => IsConnected ? "#1B5E20" : "#EF4444";

        public string ServerUrlDisplay => _settingsService.CurrentSettings.BaseUrl;

        private string _lastRefreshedText = "Just now";
        public string LastRefreshedText
        {
            get => _lastRefreshedText;
            set => SetField(ref _lastRefreshedText, value);
        }

        public bool HasOrders => FilteredOrders.Count > 0;
        public bool HasNoOrders => FilteredOrders.Count == 0 && !IsLoading;

        public int TotalOrdersCount => KitchenOrders.Count;
        public int PendingOrdersCount => KitchenOrders.Count(o => o.Status == "Pending");
        public int DelayedOrdersCount => KitchenOrders.Count(o => o.Status == "Delayed");
        public int ReadyOrdersCount => KitchenOrders.Count(o => o.Status == "Ready");

        private bool _audioAlertsEnabled;
        public bool AudioAlertsEnabled
        {
            get => _audioAlertsEnabled;
            set
            {
                if (SetField(ref _audioAlertsEnabled, value))
                {
                    _settingsService.CurrentSettings.AudioAlertsEnabled = value;
                    _ = _settingsService.SaveSettingsAsync(_settingsService.CurrentSettings);
                }
            }
        }

        private bool _isFullscreen;
        public bool IsFullscreen
        {
            get => _isFullscreen;
            set => SetField(ref _isFullscreen, value);
        }

        #endregion

        #region Commands

        public ICommand RefreshCommand { get; }
        public ICommand SetFilterCommand { get; }
        public ICommand MarkOrderReadyCommand { get; }
        public ICommand MarkOrderDelayedCommand { get; }
        public ICommand CompleteOrderCommand { get; }
        public ICommand ToggleItemCompletionCommand { get; }
        public ICommand ToggleAudioCommand { get; }
        public ICommand OpenSettingsCommand { get; }
        public ICommand ToggleFullscreenCommand { get; }

        public event EventHandler? OpenSettingsRequested;
        public event EventHandler? ToggleFullscreenRequested;
        public event EventHandler? OrdersCollectionChanged;

        #endregion

        public KdsDashboardViewModel(
            IKdsApiService apiService,
            ISettingsService settingsService,
            ILanDiscoveryService lanDiscovery,
            IAudioService audioService)
        {
            _apiService = apiService;
            _settingsService = settingsService;
            _lanDiscovery = lanDiscovery;
            _audioService = audioService;

            _audioAlertsEnabled = _settingsService.CurrentSettings.AudioAlertsEnabled;

            RefreshCommand = new RelayCommand(async () => await LoadOrdersAsync(isManual: true));
            SetFilterCommand = new RelayCommand(param => SelectedFilter = param?.ToString() ?? "All");
            MarkOrderReadyCommand = new RelayCommand(async param =>
            {
                if (param is KitchenOrder order)
                    await MarkOrderReadyAsync(order);
            });
            MarkOrderDelayedCommand = new RelayCommand(async param =>
            {
                if (param is KitchenOrder order)
                    await MarkOrderDelayedAsync(order);
            });
            CompleteOrderCommand = new RelayCommand(async param =>
            {
                if (param is KitchenOrder order)
                    await CompleteOrderAsync(order);
            });
            ToggleItemCompletionCommand = new RelayCommand(async param =>
            {
                if (param is ValueTuple<KitchenOrder, KitchenOrderItemModel> tuple)
                    await ToggleItemCompletionAsync(tuple.Item1, tuple.Item2);
                else if (param is KitchenOrderItemModel item)
                {
                    var parentOrder = KitchenOrders.FirstOrDefault(o => o.CartItems.Contains(item));
                    if (parentOrder != null)
                        await ToggleItemCompletionAsync(parentOrder, item);
                }
            });
            ToggleAudioCommand = new RelayCommand(() => AudioAlertsEnabled = !AudioAlertsEnabled);
            OpenSettingsCommand = new RelayCommand(() => OpenSettingsRequested?.Invoke(this, EventArgs.Empty));
            ToggleFullscreenCommand = new RelayCommand(() => 
            {
                IsFullscreen = !IsFullscreen;
                ToggleFullscreenRequested?.Invoke(this, EventArgs.Empty);
            });

            _apiService.ConnectionStatusChanged += (_, connected) =>
            {
                Application.Current?.Dispatcher?.Invoke(() =>
                {
                    IsConnected = connected;
                });
            };
        }

        public async Task InitializeAsync()
        {
            await _settingsService.LoadSettingsAsync();
            AudioAlertsEnabled = _settingsService.CurrentSettings.AudioAlertsEnabled;

            // If configured for auto-discovery and current server is default localhost, attempt discovery
            if (_settingsService.CurrentSettings.AutoDiscoverServer &&
                _settingsService.CurrentSettings.ServerIp == "localhost")
            {
                try
                {
                    var discovered = await _lanDiscovery.DiscoverServerAsync();
                    if (discovered != null)
                    {
                        _settingsService.CurrentSettings.ServerIp = discovered.IpAddress;
                        _settingsService.CurrentSettings.ServerPort = discovered.Port;
                        await _settingsService.SaveSettingsAsync(_settingsService.CurrentSettings);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Auto-discovery error: {ex.Message}");
                }
            }

            OnPropertyChanged(nameof(ServerUrlDisplay));
            OnPropertyChanged(nameof(ConnectionStatusText));

            // Load first batch
            await LoadOrdersAsync();

            // Start auto-poll timer
            StartTimers();
        }

        public void RestartPolling()
        {
            StartTimers();
            OnPropertyChanged(nameof(ServerUrlDisplay));
            OnPropertyChanged(nameof(ConnectionStatusText));
            _ = LoadOrdersAsync(isManual: true);
        }

        private void StartTimers()
        {
            _pollTimer?.Stop();
            _pollTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(Math.Max(3, _settingsService.CurrentSettings.PollIntervalSeconds))
            };
            _pollTimer.Tick += async (_, _) => await LoadOrdersAsync();
            _pollTimer.Start();

            _clockTimer?.Stop();
            _clockTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(10)
            };
            _clockTimer.Tick += (_, _) =>
            {
                foreach (var order in KitchenOrders)
                {
                    order.UpdateWaitingTime();
                }
            };
            _clockTimer.Start();
        }

        public async Task LoadOrdersAsync(bool isManual = false)
        {
            if (isManual)
                IsLoading = true;

            try
            {
                var dtos = await _apiService.GetWaitingTransactionsAsync();
                IsConnected = _apiService.IsConnected;

                UpdateOrdersFromDtos(dtos);

                LastRefreshedText = DateTime.Now.ToString("HH:mm:ss");
                OnPropertyChanged(nameof(TotalOrdersCount));
                OnPropertyChanged(nameof(PendingOrdersCount));
                OnPropertyChanged(nameof(DelayedOrdersCount));
                OnPropertyChanged(nameof(ReadyOrdersCount));
            }
            finally
            {
                if (isManual)
                    IsLoading = false;
            }
        }

        private void UpdateOrdersFromDtos(List<WaitingTransactionDto> dtos)
        {
            bool hasNewOrders = false;
            bool hasNewRefires = false;

            var currentTxnIds = dtos.Select(d => d.TransactionId).ToHashSet();

            // 1. Remove completed/deleted orders
            for (int i = KitchenOrders.Count - 1; i >= 0; i--)
            {
                if (!currentTxnIds.Contains(KitchenOrders[i].TransactionId))
                {
                    _knownOrderIds.Remove(KitchenOrders[i].TransactionId);
                    _knownRefiredOrderIds.Remove(KitchenOrders[i].TransactionId);
                    KitchenOrders.RemoveAt(i);
                }
            }

            // 2. Add or Update orders in-place
            foreach (var dto in dtos)
            {
                var existing = KitchenOrders.FirstOrDefault(o => o.TransactionId == dto.TransactionId);

                if (existing == null)
                {
                    // New order
                    var newOrder = MapToKitchenOrder(dto);
                    KitchenOrders.Add(newOrder);

                    if (!_knownOrderIds.Contains(dto.TransactionId))
                    {
                        _knownOrderIds.Add(dto.TransactionId);
                        if (dto.IsRefired)
                        {
                            _knownRefiredOrderIds.Add(dto.TransactionId);
                            hasNewRefires = true;
                        }
                        else
                        {
                            hasNewOrders = true;
                        }
                    }
                }
                else
                {
                    // Update existing properties
                    existing.Status = dto.Status ?? "Pending";
                    existing.TableNumber = dto.TableNumber;
                    existing.TableName = dto.TableName;
                    existing.Notes = dto.Notes;
                    if (!string.IsNullOrEmpty(dto.CustomerName)) existing.CustomerName = dto.CustomerName;
                    existing.CustomerPhone = null; // Kitchen does not know customer phone
                    if (dto.IsOnlineOrder) existing.IsOnlineOrderExplicit = true;
                    if (dto.IsDelivery) existing.IsDeliveryExplicit = true;
                    existing.CustomerLocation = (dto.IsDelivery && KitchenOrder.IsRealAddress(dto.DeliveryAddress)) ? dto.DeliveryAddress : null;

                    if (string.IsNullOrEmpty(existing.CustomerName) || (dto.IsDelivery && string.IsNullOrEmpty(existing.CustomerLocation)))
                    {
                        existing.ExtractCustomerDetailsFromNotes();
                    }

                    // Check for refire change
                    if (dto.IsRefired && !existing.IsRefired)
                    {
                        existing.IsRefired = true;
                        existing.RefireReason = dto.RefireReason;
                        existing.RefiredAt = dto.RefiredAt;
                        existing.RefiredByStaffName = dto.RefiredByStaffName;

                        if (!_knownRefiredOrderIds.Contains(dto.TransactionId))
                        {
                            _knownRefiredOrderIds.Add(dto.TransactionId);
                            hasNewRefires = true;
                        }
                    }

                    // Sync items completion status without destroying ObservableCollection
                    SyncCartItems(existing, dto.Items);
                }
            }

            // Sort orders: Refired first, then oldest CreatedDate first
            var sorted = KitchenOrders
                .OrderByDescending(o => o.IsRefired)
                .ThenBy(o => o.CreatedDate)
                .ToList();

            for (int i = 0; i < sorted.Count; i++)
            {
                int oldIndex = KitchenOrders.IndexOf(sorted[i]);
                if (oldIndex != i)
                {
                    KitchenOrders.Move(oldIndex, i);
                }
            }

            ApplyFilter();

            // Trigger alerts
            if (hasNewRefires)
            {
                _audioService.PlayRefireOrderAlert();
            }
            else if (hasNewOrders)
            {
                _audioService.PlayNewOrderAlert();
            }
        }

        private KitchenOrder MapToKitchenOrder(WaitingTransactionDto dto)
        {
            var order = new KitchenOrder
            {
                TransactionId = dto.TransactionId,
                TableId = dto.TableId,
                TableNumber = dto.TableNumber,
                TableName = dto.TableName,
                StaffId = dto.StaffId,
                Status = dto.Status ?? "Pending",
                Notes = dto.Notes,
                CreatedDate = dto.CreatedDate,
                IsRefired = dto.IsRefired,
                RefireReason = dto.RefireReason,
                RefiredAt = dto.RefiredAt,
                RefiredByStaffName = dto.RefiredByStaffName,
                CustomerName = dto.CustomerName,
                CustomerPhone = null, // Kitchen does not know customer phone
                CustomerLocation = (dto.IsDelivery && KitchenOrder.IsRealAddress(dto.DeliveryAddress)) ? dto.DeliveryAddress : null,
                IsOnlineOrderExplicit = dto.IsOnlineOrder ? true : null,
                IsDeliveryExplicit = dto.IsDelivery ? true : null
            };

            if (string.IsNullOrEmpty(order.CustomerName) || (dto.IsDelivery && string.IsNullOrEmpty(order.CustomerLocation)))
            {
                order.ExtractCustomerDetailsFromNotes();
            }

            if (dto.Items != null)
            {
                foreach (var itemDto in dto.Items)
                {
                    order.CartItems.Add(new KitchenOrderItemModel
                    {
                        ProductId = itemDto.ProductId,
                        Name = itemDto.Name,
                        Quantity = itemDto.Quantity,
                        Price = itemDto.Price,
                        Discount = itemDto.Discount,
                        Total = itemDto.Total,
                        IsCompleted = itemDto.IsCompleted,
                        AppliedModifiers = itemDto.AppliedModifiers
                    });
                }
            }

            return order;
        }

        private void SyncCartItems(KitchenOrder order, List<WaitingTransactionItemDto>? itemDtos)
        {
            if (itemDtos == null) return;

            for (int i = 0; i < itemDtos.Count && i < order.CartItems.Count; i++)
            {
                var dtoItem = itemDtos[i];
                var modelItem = order.CartItems[i];

                if (modelItem.IsCompleted != dtoItem.IsCompleted)
                {
                    modelItem.IsCompleted = dtoItem.IsCompleted;
                }
            }
        }

        private void ApplyFilter()
        {
            FilteredOrders.Clear();

            var query = SelectedFilter switch
            {
                "Pending" => KitchenOrders.Where(o => o.Status == "Pending"),
                "Delayed" => KitchenOrders.Where(o => o.Status == "Delayed"),
                "Ready" => KitchenOrders.Where(o => o.Status == "Ready"),
                _ => KitchenOrders
            };

            foreach (var order in query)
            {
                FilteredOrders.Add(order);
            }

            OnPropertyChanged(nameof(HasOrders));
            OnPropertyChanged(nameof(HasNoOrders));
            OrdersCollectionChanged?.Invoke(this, EventArgs.Empty);
        }

        #region Order Operations

        public async Task MarkOrderReadyAsync(KitchenOrder order)
        {
            if (order == null) return;

            // Mark all items as checked locally
            foreach (var item in order.CartItems)
            {
                item.IsCompleted = true;
            }

            order.Status = "Ready";
            var success = await _apiService.UpdateOrderStatusAsync(order.TransactionId, "Ready");

            if (success)
            {
                // Also update items on API
                await PushItemsStatusToApi(order);
                _audioService.PlayOrderCompletedSound();
                ApplyFilter();
            }
        }

        public async Task MarkOrderDelayedAsync(KitchenOrder order)
        {
            if (order == null) return;

            order.Status = "Delayed";
            var success = await _apiService.UpdateOrderStatusAsync(order.TransactionId, "Delayed");

            if (success)
            {
                ApplyFilter();
            }
        }

        public async Task CompleteOrderAsync(KitchenOrder order)
        {
            if (order == null) return;

            var success = await _apiService.CompleteOrderAsync(order.TransactionId);
            if (success)
            {
                KitchenOrders.Remove(order);
                _knownOrderIds.Remove(order.TransactionId);
                _knownRefiredOrderIds.Remove(order.TransactionId);
                _audioService.PlayOrderCompletedSound();
                ApplyFilter();
            }
        }

        public async Task ToggleItemCompletionAsync(KitchenOrder order, KitchenOrderItemModel item)
        {
            if (order == null || item == null) return;

            item.IsCompleted = !item.IsCompleted;

            // If all items are completed, automatically mark order as Ready
            if (order.AllItemsCompleted && order.Status == "Pending")
            {
                order.Status = "Ready";
                await _apiService.UpdateOrderStatusAsync(order.TransactionId, "Ready");
            }

            // Sync item change to PottaAPI
            await PushItemsStatusToApi(order);
        }

        private async Task PushItemsStatusToApi(KitchenOrder order)
        {
            var itemDtos = order.CartItems.Select(i => new WaitingTransactionItemDto
            {
                ProductId = i.ProductId,
                Name = i.Name,
                Quantity = i.Quantity,
                Price = i.Price,
                Discount = i.Discount,
                Total = i.Total,
                SubTotal = (i.Price * i.Quantity) - i.Discount,
                IsCompleted = i.IsCompleted,
                AppliedModifiers = i.AppliedModifiers
            }).ToList();

            await _apiService.UpdateOrderItemsAsync(order.TransactionId, itemDtos);
        }

        #endregion
    }
}
