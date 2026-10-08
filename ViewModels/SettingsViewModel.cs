using PottaKDS.Models;
using PottaKDS.Services.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace PottaKDS.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        private readonly ISettingsService _settingsService;
        private readonly IKdsApiService _apiService;
        private readonly ILanDiscoveryService _lanDiscovery;

        private string _serverIp;
        public string ServerIp
        {
            get => _serverIp;
            set { SetField(ref _serverIp, value); TestStatusMessage = null; }
        }

        private int _serverPort;
        public int ServerPort
        {
            get => _serverPort;
            set { SetField(ref _serverPort, value); TestStatusMessage = null; }
        }

        private int _pollIntervalSeconds;
        public int PollIntervalSeconds
        {
            get => _pollIntervalSeconds;
            set => SetField(ref _pollIntervalSeconds, value);
        }

        private bool _audioAlertsEnabled;
        public bool AudioAlertsEnabled
        {
            get => _audioAlertsEnabled;
            set => SetField(ref _audioAlertsEnabled, value);
        }

        private bool _fullscreenOnStartup;
        public bool FullscreenOnStartup
        {
            get => _fullscreenOnStartup;
            set => SetField(ref _fullscreenOnStartup, value);
        }

        private bool _isScanning;
        public bool IsScanning
        {
            get => _isScanning;
            set => SetField(ref _isScanning, value);
        }

        private bool _isTesting;
        public bool IsTesting
        {
            get => _isTesting;
            set => SetField(ref _isTesting, value);
        }

        private string? _testStatusMessage;
        public string? TestStatusMessage
        {
            get => _testStatusMessage;
            set => SetField(ref _testStatusMessage, value);
        }

        private bool? _isTestSuccess;
        public bool? IsTestSuccess
        {
            get => _isTestSuccess;
            set => SetField(ref _isTestSuccess, value);
        }

        private string _scanStatusText = "Ready";
        public string ScanStatusText
        {
            get => _scanStatusText;
            set => SetField(ref _scanStatusText, value);
        }

        public ObservableCollection<DiscoveredServer> DiscoveredServers { get; } = new();

        public ICommand TestConnectionCommand { get; }
        public ICommand ScanNetworkCommand { get; }
        public ICommand SelectServerCommand { get; }
        public ICommand SaveSettingsCommand { get; }

        public event EventHandler? RequestClose;

        public SettingsViewModel(
            ISettingsService settingsService,
            IKdsApiService apiService,
            ILanDiscoveryService lanDiscovery)
        {
            _settingsService = settingsService;
            _apiService = apiService;
            _lanDiscovery = lanDiscovery;

            var settings = _settingsService.CurrentSettings;
            _serverIp = settings.ServerIp;
            _serverPort = settings.ServerPort;
            _pollIntervalSeconds = settings.PollIntervalSeconds;
            _audioAlertsEnabled = settings.AudioAlertsEnabled;
            _fullscreenOnStartup = settings.FullscreenOnStartup;

            TestConnectionCommand = new RelayCommand(async () => await TestConnectionAsync());
            ScanNetworkCommand = new RelayCommand(async () => await ScanNetworkAsync());
            SelectServerCommand = new RelayCommand(param =>
            {
                if (param is DiscoveredServer server)
                {
                    ServerIp = server.IpAddress;
                    ServerPort = server.Port;
                    _ = TestConnectionAsync();
                }
            });
            SaveSettingsCommand = new RelayCommand(async () => await SaveSettingsAsync());

            // Auto-scan on dialog open if no valid connection
            _ = AutoScanOnDialogOpen();
        }

        private async Task AutoScanOnDialogOpen()
        {
            // Wait a moment for UI to render
            await Task.Delay(300);

            // If current server is localhost/default and not connected, auto-scan
            if ((ServerIp == "localhost" || ServerIp == "127.0.0.1") && !_apiService.IsConnected)
            {
                ScanStatusText = "Auto-scanning network for Potta POS servers...";
                await ScanNetworkAsync();
            }
        }

        public async Task TestConnectionAsync()
        {
            if (IsTesting) return;

            try
            {
                IsTesting = true;
                TestStatusMessage = "Testing connection...";
                IsTestSuccess = null;

                var tempSettings = new KdsSettings
                {
                    ServerIp = ServerIp,
                    ServerPort = ServerPort
                };

                var success = await _apiService.TestConnectionAsync(tempSettings.BaseUrl);
                IsTestSuccess = success;

                if (success)
                {
                    TestStatusMessage = $"Connected successfully to {tempSettings.BaseUrl}!";
                }
                else
                {
                    TestStatusMessage = $"Connection failed: {_apiService.LastErrorMessage ?? "No response from server"}";
                }
            }
            finally
            {
                IsTesting = false;
            }
        }

        public async Task ScanNetworkAsync()
        {
            if (IsScanning) return;

            try
            {
                IsScanning = true;
                DiscoveredServers.Clear();

                var progress = new Progress<string>(msg => ScanStatusText = msg);
                int targetPort = ServerPort > 0 ? ServerPort : 5001;
                var servers = await _lanDiscovery.ScanNetworkAsync(targetPort, progress);

                foreach (var server in servers)
                {
                    DiscoveredServers.Add(server);
                }

                if (DiscoveredServers.Count > 0)
                {
                    ScanStatusText = $"Found {DiscoveredServers.Count} server(s). Select one to connect.";
                }
                else
                {
                    ScanStatusText = "No Potta POS server found on LAN. Enter IP manually.";
                }
            }
            finally
            {
                IsScanning = false;
            }
        }

        public async Task SaveSettingsAsync()
        {
            var settings = _settingsService.CurrentSettings;
            settings.ServerIp = ServerIp.Trim();
            settings.ServerPort = ServerPort;
            settings.PollIntervalSeconds = PollIntervalSeconds;
            settings.AudioAlertsEnabled = AudioAlertsEnabled;
            settings.FullscreenOnStartup = FullscreenOnStartup;

            await _settingsService.SaveSettingsAsync(settings);
            RequestClose?.Invoke(this, EventArgs.Empty);
        }
    }
}
