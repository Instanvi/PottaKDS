using System.Configuration;
using System.Data;
using System.Windows;
using System.Diagnostics;
using System.Threading;

namespace PottaKDS;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private Mutex? _singleInstanceMutex;
    // UNIQUE GUID FOR POTTAKDS - DIFFERENT FROM POTTA FINANCE
    private const string KDS_MUTEX_GUID = "A7D9E1F3-5C8B-4E2A-9D6F-1B4C7E9A2F5D";

    protected override void OnStartup(StartupEventArgs e)
    {
        Debug.WriteLine("===========================================================");
        Debug.WriteLine("POTTAKDS STARTING");
        Debug.WriteLine("===========================================================");
        Debug.WriteLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
        Debug.WriteLine($"Machine: {Environment.MachineName}");
        Debug.WriteLine($"User: {Environment.UserName}");

        // Check for single instance using UNIQUE GUID
        Debug.WriteLine("Checking for existing PottaKDS instance...");
        _singleInstanceMutex = new Mutex(true, $"Global\\{KDS_MUTEX_GUID}", out bool isFirstInstance);

        if (!isFirstInstance)
        {
            Debug.WriteLine("PottaKDS is already running!");
            Debug.WriteLine("Shutting down this instance...");
            MessageBox.Show(
                "PottaKDS is already running.\n\nOnly one instance can run at a time.",
                "Already Running",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
            Shutdown();
            return;
        }

        Debug.WriteLine("First PottaKDS instance confirmed - continuing startup");
        Debug.WriteLine("===========================================================");

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Debug.WriteLine("===========================================================");
        Debug.WriteLine("POTTAKDS SHUTDOWN INITIATED");
        Debug.WriteLine("===========================================================");

        // Release single instance mutex
        try
        {
            _singleInstanceMutex?.ReleaseMutex();
            _singleInstanceMutex?.Dispose();
            Debug.WriteLine("Mutex released");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Mutex release error: {ex.Message}");
        }

        Debug.WriteLine("POTTAKDS SHUTDOWN COMPLETE");
        Debug.WriteLine("===========================================================");

        base.OnExit(e);
    }
}


