using Microsoft.Extensions.Configuration;

namespace OpenCodeSleepGuard;

public class AppSettings
{
    private static readonly string DefaultDbPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".local",
        "share",
        "opencode",
        "opencode.db");

    public List<string> ProcessNames { get; set; } = new() { "opencode", "node" };
    public int CheckIntervalSeconds { get; set; } = 5;
    public string DbPath { get; set; } = DefaultDbPath;

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

            if (settings.CheckIntervalSeconds <= 0)
                settings.CheckIntervalSeconds = 5;

            if (string.IsNullOrWhiteSpace(settings.DbPath))
            {
                settings.DbPath = DefaultDbPath;
            }
            else
            {
                settings.DbPath = Environment.ExpandEnvironmentVariables(settings.DbPath);
            }
        }
        catch
        {
            // Return defaults on any configuration error
        }

        return settings;
    }
}
