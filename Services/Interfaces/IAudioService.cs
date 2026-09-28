namespace PottaKDS.Services.Interfaces
{
    public interface IAudioService
    {
        void PlayNewOrderAlert();
        void PlayRefireOrderAlert();
        void PlayOrderCompletedSound();
    }
}
