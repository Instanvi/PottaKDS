using PottaKDS.Models;
using PottaKDS.Services.Interfaces;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace PottaKDS.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly string _settingsFilePath;
        private KdsSettings _currentSettings = new();

        public KdsSettings CurrentSettings => _currentSettings;

        public SettingsService()
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appDirectory = Path.Combine(localAppData, "PottaPOS", "PottaKDS");
            Directory.CreateDirectory(appDirectory);
            _settingsFilePath = Path.Combine(appDirectory, "settings.json");
        }

        public async Task<KdsSettings> LoadSettingsAsync()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    var json = await File.ReadAllTextAsync(_settingsFilePath);
                    var settings = JsonSerializer.Deserialize<KdsSettings>(json);
                    if (settings != null)
                    {
                        _currentSettings = settings;
                        return _currentSettings;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load settings: {ex.Message}");
            }

            // Fallback default
            _currentSettings = new KdsSettings();
            return _currentSettings;
        }

        public async Task SaveSettingsAsync(KdsSettings settings)
        {
            try
            {
                _currentSettings = settings;
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(_settingsFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
            }
        }
    }
}
