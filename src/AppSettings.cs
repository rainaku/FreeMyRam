using System.IO;
using System.Text.Json;

namespace FreeMyRam;

/// <summary>
/// Application settings manager
/// </summary>
public class AppSettings
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FreeMyRam",
        "settings.json"
    );

    public bool CleanOnStartup { get; set; } = false;
    public string Language { get; set; } = "English";
    public string Theme { get; set; } = "Light";
    
    // Auto clean interval in minutes (0 = disabled, options: 5, 10, 15, 30, 45, 60, 120, 180)
    public int AutoCleanIntervalMinutes { get; set; } = 0;
    
    // Auto clean when RAM usage exceeds threshold (true = enabled)
    public bool AutoCleanOnHighUsage { get; set; } = false;
    
    // RAM usage threshold percentage (default 70%)
    public int RamUsageThreshold { get; set; } = 70;

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                string json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch
        {
            // If loading fails, return default settings
        }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            string? directory = Path.GetDirectoryName(SettingsPath);
            if (directory != null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // Ignore save errors
        }
    }
}
