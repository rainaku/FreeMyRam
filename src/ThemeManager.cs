using System.Windows;
using System.Windows.Media;

namespace FreeMyRam;

/// <summary>
/// Manages application theme (Dark/Light mode)
/// </summary>
public static class ThemeManager
{
    public enum Theme
    {
        Dark,
        Light
    }

    private static Theme _currentTheme = Theme.Light;
    public static event Action? ThemeChanged;

    public static Theme CurrentTheme
    {
        get => _currentTheme;
        set
        {
            if (_currentTheme != value)
            {
                _currentTheme = value;
                ApplyTheme();
                ThemeChanged?.Invoke();
            }
        }
    }

    public static void ApplyTheme()
    {
        var resources = System.Windows.Application.Current.Resources;

        if (_currentTheme == Theme.Light)
        {
            // Light Theme Colors
            resources["PrimaryColor"] = ColorFromHex("#f8f9fa");
            resources["SecondaryColor"] = ColorFromHex("#e9ecef");
            resources["AccentColor"] = ColorFromHex("#dee2e6");
            resources["HighlightColor"] = ColorFromHex("#0d6efd");
            resources["TextColor"] = ColorFromHex("#212529");
            resources["SubTextColor"] = ColorFromHex("#6c757d");

            resources["PrimaryBrush"] = new SolidColorBrush(ColorFromHex("#f8f9fa"));
            resources["SecondaryBrush"] = new SolidColorBrush(ColorFromHex("#e9ecef"));
            resources["AccentBrush"] = new SolidColorBrush(ColorFromHex("#dee2e6"));
            resources["HighlightBrush"] = new SolidColorBrush(ColorFromHex("#0d6efd"));
            resources["TextBrush"] = new SolidColorBrush(ColorFromHex("#212529"));
            resources["SubTextBrush"] = new SolidColorBrush(ColorFromHex("#6c757d"));
        }
        else
        {
            // Dark Theme Colors
            resources["PrimaryColor"] = ColorFromHex("#1a1a2e");
            resources["SecondaryColor"] = ColorFromHex("#16213e");
            resources["AccentColor"] = ColorFromHex("#0f3460");
            resources["HighlightColor"] = ColorFromHex("#e94560");
            resources["TextColor"] = ColorFromHex("#eaeaea");
            resources["SubTextColor"] = ColorFromHex("#a0a0a0");

            resources["PrimaryBrush"] = new SolidColorBrush(ColorFromHex("#1a1a2e"));
            resources["SecondaryBrush"] = new SolidColorBrush(ColorFromHex("#16213e"));
            resources["AccentBrush"] = new SolidColorBrush(ColorFromHex("#0f3460"));
            resources["HighlightBrush"] = new SolidColorBrush(ColorFromHex("#e94560"));
            resources["TextBrush"] = new SolidColorBrush(ColorFromHex("#eaeaea"));
            resources["SubTextBrush"] = new SolidColorBrush(ColorFromHex("#a0a0a0"));
        }
    }

    private static System.Windows.Media.Color ColorFromHex(string hex)
    {
        return (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex);
    }
}
