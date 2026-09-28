using PottaKDS.Services.Interfaces;
using System;
using System.IO;
using System.Media;
using System.Threading.Tasks;

namespace PottaKDS.Services
{
    public class AudioService : IAudioService
    {
        private readonly ISettingsService _settingsService;

        public AudioService(ISettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        public void PlayNewOrderAlert()
        {
            if (!_settingsService.CurrentSettings.AudioAlertsEnabled)
                return;

            Task.Run(() =>
            {
                try
                {
                    SystemSounds.Asterisk.Play();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Audio playback error: {ex.Message}");
                }
            });
        }

        public void PlayRefireOrderAlert()
        {
            if (!_settingsService.CurrentSettings.AudioAlertsEnabled)
                return;

            Task.Run(() =>
            {
                try
                {
                    // Urgent double alert for Refire
                    SystemSounds.Exclamation.Play();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Audio playback error: {ex.Message}");
                }
            });
        }

        public void PlayOrderCompletedSound()
        {
            if (!_settingsService.CurrentSettings.AudioAlertsEnabled)
                return;

            Task.Run(() =>
            {
                try
                {
                    SystemSounds.Hand.Play();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Audio playback error: {ex.Message}");
                }
            });
        }
    }
}
