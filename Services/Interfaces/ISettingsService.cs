using PottaKDS.Models;
using System.Threading.Tasks;

namespace PottaKDS.Services.Interfaces
{
    public interface ISettingsService
    {
        KdsSettings CurrentSettings { get; }
        Task<KdsSettings> LoadSettingsAsync();
        Task SaveSettingsAsync(KdsSettings settings);
    }
}
