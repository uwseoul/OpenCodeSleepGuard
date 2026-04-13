using Microsoft.Extensions.Configuration;

namespace OpenCodeSleepGuard;

public class AppSettings
{
    public List<string> ProcessNames { get; set; } = new() { "opencode", "node" };
    public double CpuThreshold { get; set; } = 5.0;
    public int IdleTimeoutSeconds { get; set; } = 180;
    public int CheckIntervalSeconds { get; set; } = 5;

    public static AppSettings Load()
    {
        var settings = new AppSettings();
        var configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

        if (!File.Exists(configPath))
            return settings;

        try
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .Build();

            configuration.Bind(settings);

            // Ensure ProcessNames has defaults if empty or null
            if (settings.ProcessNames == null || settings.ProcessNames.Count == 0)
                settings.ProcessNames = new List<string> { "opencode", "node" };

            // Validate numeric bounds
            if (settings.CpuThreshold <= 0)
                settings.CpuThreshold = 5.0;
            if (settings.IdleTimeoutSeconds <= 0)
                settings.IdleTimeoutSeconds = 180;
            if (settings.CheckIntervalSeconds <= 0)
                settings.CheckIntervalSeconds = 5;
        }
        catch
        {
            // Return defaults on any configuration error
        }

        return settings;
    }
}
