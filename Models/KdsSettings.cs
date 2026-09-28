namespace PottaKDS.Models
{
    public class KdsSettings
    {
        public string ServerIp { get; set; } = "localhost";
        public int ServerPort { get; set; } = 5001;
        public bool UseHttps { get; set; } = false;
        public int PollIntervalSeconds { get; set; } = 5;
        public bool AutoDiscoverServer { get; set; } = true;
        public bool AudioAlertsEnabled { get; set; } = true;
        public bool FullscreenOnStartup { get; set; } = false;
        public int PreferredColumnCount { get; set; } = 0; // 0 = Auto

        public string BaseUrl
        {
            get
            {
                var scheme = UseHttps ? "https" : "http";
                var host = string.IsNullOrWhiteSpace(ServerIp) ? "localhost" : ServerIp.Trim();
                
                // If the user already provided http:// or https:// in the IP field, clean it
                if (host.StartsWith("http://", System.StringComparison.OrdinalIgnoreCase))
                    return host.TrimEnd('/');
                if (host.StartsWith("https://", System.StringComparison.OrdinalIgnoreCase))
                    return host.TrimEnd('/');

                return $"{scheme}://{host}:{ServerPort}";
            }
        }
    }
}
