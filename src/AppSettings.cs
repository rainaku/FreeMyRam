using System.IO;
using System.Text.Json;
using Microsoft.Win32;

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
    
    private const string StartupRegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "FreeMyRam";

    public bool CleanOnStartup { get; set; } = false;
    public string Language { get; set; } = "English";
    public string Theme { get; set; } = "Light";
    
    // Auto clean interval in minutes (0 = disabled, options: 5, 10, 15, 30, 45, 60, 120, 180)
    public int AutoCleanIntervalMinutes { get; set; } = 0;
    
    // Auto clean when RAM usage exceeds threshold (true = enabled)
    public bool AutoCleanOnHighUsage { get; set; } = false;
    
    // RAM usage threshold percentage (default 70%)
    public int RamUsageThreshold { get; set; } = 70;
    
    // Start with Windows
    public bool StartWithWindows { get; set; } = false;

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                string json = File.ReadAllText(SettingsPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                // Sync StartWithWindows with actual registry state
                settings.StartWithWindows = IsInStartup();
                return settings;
            }
        }
        catch
        {
            // If loading fails, return default settings
        }
        var defaultSettings = new AppSettings();
        defaultSettings.StartWithWindows = IsInStartup();
        return defaultSettings;
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
    
    /// <summary>
    /// Check if app is registered to start with Windows
    /// </summary>
    public static bool IsInStartup()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, false);
            return key?.GetValue(AppName) != null;
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Add or remove app from Windows startup
    /// </summary>
    public static void SetStartWithWindows(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, true);
            if (key == null) return;
            
            if (enable)
            {
                // Get the current executable path
                string exePath = Environment.ProcessPath ?? System.Reflection.Assembly.GetExecutingAssembly().Location;
                key.SetValue(AppName, $"\"{exePath}\"");
            }
            else
            {
                key.DeleteValue(AppName, false);
            }
        }
        catch
        {
            // Ignore registry errors
        }
    }
}

