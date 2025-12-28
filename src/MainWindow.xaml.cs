using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Diagnostics;

namespace FreeMyRam;

public partial class MainWindow : Window
{
    private readonly MemoryCleaner _memoryCleaner;
    private readonly DispatcherTimer _updateTimer;
    private readonly DispatcherTimer _autoCleanTimer;
    private readonly AppSettings _settings;
    private TrayIcon? _trayIcon;
    private long _memoryBeforeClean;
    private bool _isExiting;
    
    // Auto clean interval options in minutes
    private static readonly int[] _intervalOptions = { 0, 5, 10, 15, 30, 45, 60, 120, 180 };
    
    // Cooldown for RAM threshold auto clean (10 minutes)
    private DateTime _lastHighRamClean = DateTime.MinValue;
    private static readonly TimeSpan _highRamCooldown = TimeSpan.FromMinutes(10);
    
    // Cached brushes for performance (avoid creating new objects)
    private static readonly SolidColorBrush ToggleOnBrush = new((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#4CAF50"));
    private static readonly SolidColorBrush ToggleOffBrush = new((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#555555"));
    
    static MainWindow()
    {
        // Freeze brushes for better performance
        ToggleOnBrush.Freeze();
        ToggleOffBrush.Freeze();
    }

    public MainWindow()
    {
        InitializeComponent();
        _memoryCleaner = new MemoryCleaner();
        _settings = AppSettings.Load();
        
        // Apply settings to UI
        UpdateToggleVisual(_settings.CleanOnStartup);
        UpdateHighRamToggleVisual(_settings.AutoCleanOnHighUsage);
        UpdateAutoCleanIntervalUI();
        
        // Apply language setting
        Localization.CurrentLanguage = _settings.Language == "Vietnamese" 
            ? Localization.Language.Vietnamese 
            : Localization.Language.English;
        Localization.LanguageChanged += UpdateUILanguage;
        
        // Apply theme setting
        ThemeManager.CurrentTheme = _settings.Theme == "Light" 
            ? ThemeManager.Theme.Light 
            : ThemeManager.Theme.Dark;
        ThemeManager.ThemeChanged += UpdateUITheme;
        
        UpdateUILanguage();
        UpdateUITheme();
        
        // Initialize system tray
        _trayIcon = new TrayIcon(_memoryCleaner, _settings, ShowWindow, ExitApplication, OnSettingsChanged);
        
        // Update memory info every 2 seconds (optimized from 1s)
        _updateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _updateTimer.Tick += UpdateTimer_Tick;
        _updateTimer.Start();
        
        // Auto clean timer
        _autoCleanTimer = new DispatcherTimer();
        _autoCleanTimer.Tick += async (s, e) => await PerformAutoClean();
        SetupAutoCleanTimer();
        
        UpdateMemoryInfo();
        
        // Auto-clean on startup if enabled
        Loaded += async (s, e) =>
        {
            if (_settings.CleanOnStartup)
            {
                await AutoCleanOnStartup();
            }
        };
    }

    private void SetupAutoCleanTimer()
    {
        if (_settings.AutoCleanIntervalMinutes > 0)
        {
            _autoCleanTimer.Interval = TimeSpan.FromMinutes(_settings.AutoCleanIntervalMinutes);
            _autoCleanTimer.Start();
        }
        else
        {
            _autoCleanTimer.Stop();
        }
    }

    private async Task PerformAutoClean()
    {
        SaveMemoryBeforeClean();
        
        await Task.Run(() =>
        {
            _memoryCleaner.EmptyWorkingSets();
            _memoryCleaner.EmptySystemWorkingSet();
            _memoryCleaner.EmptyModifiedPageList();
            _memoryCleaner.EmptyStandbyList();
        });
        
        var memInfo = MemoryInfo.GetMemoryStatus();
        long memoryAfterClean = (long)(memInfo.TotalPhysicalMemory - memInfo.AvailablePhysicalMemory);
        long freedBytes = _memoryBeforeClean - memoryAfterClean;
        
        if (freedBytes > 0)
        {
            double freedMB = freedBytes / (1024.0 * 1024);
            StatusText.Text = Localization.AutoCleanedRam;
            FreedMemoryText.Text = Localization.FreedMB(freedMB);
            _trayIcon?.ShowBalloon(Localization.AutoCleanBalloon(freedMB), Localization.AutoCleanTitle);
        }
        
        UpdateMemoryInfo();
    }

    private void UpdateTimer_Tick(object? sender, EventArgs e)
    {
        UpdateMemoryInfo();
        CheckRamThreshold();
    }

    private async void CheckRamThreshold()
    {
        if (!_settings.AutoCleanOnHighUsage)
            return;
        
        // Check cooldown
        if (DateTime.Now - _lastHighRamClean < _highRamCooldown)
            return;
        
        var memInfo = MemoryInfo.GetMemoryStatus();
        if (memInfo.MemoryLoad >= _settings.RamUsageThreshold)
        {
            _lastHighRamClean = DateTime.Now;
            await PerformAutoClean();
        }
    }

    private void UpdateUILanguage()
    {
        // Update all UI text
        MemoryUsageLabel.Text = Localization.MemoryUsage;
        QuickActionsLabel.Text = Localization.QuickActions;
        CleanAllButton.Content = Localization.CleanAllMemory;
        AdvancedOptionsLabel.Text = Localization.AdvancedOptions;
        FlushWorkingSetsBtn.Content = Localization.FlushWorkingSets;
        FlushSystemWorkingSetBtn.Content = Localization.FlushSystemWorkingSet;
        FlushModifiedPageListBtn.Content = Localization.FlushModifiedPageList;
        FlushStandbyListBtn.Content = Localization.FlushStandbyList;
        FlushPriority0StandbyListBtn.Content = Localization.FlushPriority0StandbyList;
        CleanDiskBtn.Content = Localization.CleanDisk;
        SettingsLabel.Text = Localization.Settings;
        CleanOnStartupText.Text = Localization.CleanOnStartup;
        AutoCleanIntervalText.Text = Localization.AutoCleanInterval;
        UpdateAutoCleanIntervalUI();
        AutoCleanHighRamText.Text = Localization.AutoCleanOnHighRam;
        LanguageToggleBtn.Content = Localization.Language_Option;
        ThemeToggleBtn.Content = Localization.ThemeOption;
        StatusText.Text = Localization.Ready;
    }

    private static readonly Thickness ToggleOnMargin = new(0, 0, 2, 0);
    private static readonly Thickness ToggleOffMargin = new(2, 0, 0, 0);

    private void UpdateToggleVisual(bool isOn)
    {
        if (isOn)
        {
            ToggleBorder.Background = ToggleOnBrush;
            ToggleCircle.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
            ToggleCircle.Margin = ToggleOnMargin;
        }
        else
        {
            ToggleBorder.Background = ToggleOffBrush;
            ToggleCircle.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            ToggleCircle.Margin = ToggleOffMargin;
        }
    }

    private void UpdateHighRamToggleVisual(bool isOn)
    {
        if (isOn)
        {
            ToggleHighRamBorder.Background = ToggleOnBrush;
            ToggleHighRamCircle.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
            ToggleHighRamCircle.Margin = ToggleOnMargin;
        }
        else
        {
            ToggleHighRamBorder.Background = ToggleOffBrush;
            ToggleHighRamCircle.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            ToggleHighRamCircle.Margin = ToggleOffMargin;
        }
    }

    private void UpdateAutoCleanIntervalUI()
    {
        if (_settings.AutoCleanIntervalMinutes == 0)
        {
            AutoCleanIntervalValue.Text = Localization.AutoCleanDisabled;
        }
        else
        {
            AutoCleanIntervalValue.Text = $"{_settings.AutoCleanIntervalMinutes} {Localization.Minutes}";
        }
    }

    private void UpdateUITheme()
    {
        ThemeToggleBtn.Content = Localization.ThemeOption;
    }

    private void OnSettingsChanged()
    {
        // Sync UI with settings when changed from tray
        UpdateToggleVisual(_settings.CleanOnStartup);
        
        // Sync language
        Localization.CurrentLanguage = _settings.Language == "Vietnamese" 
            ? Localization.Language.Vietnamese 
            : Localization.Language.English;
        
        // Sync theme
        ThemeManager.CurrentTheme = _settings.Theme == "Light" 
            ? ThemeManager.Theme.Light 
            : ThemeManager.Theme.Dark;
    }

    private async Task AutoCleanOnStartup()
    {
        StatusText.Text = Localization.AutoCleaningOnStartup;
        FreedMemoryText.Text = "";
        SaveMemoryBeforeClean();
        
        await Task.Run(() =>
        {
            _memoryCleaner.EmptyWorkingSets();
            _memoryCleaner.EmptySystemWorkingSet();
            _memoryCleaner.EmptyModifiedPageList();
            _memoryCleaner.EmptyStandbyList();
        });
        
        var memInfo = MemoryInfo.GetMemoryStatus();
        long memoryAfterClean = (long)(memInfo.TotalPhysicalMemory - memInfo.AvailablePhysicalMemory);
        long freedBytes = _memoryBeforeClean - memoryAfterClean;
        
        if (freedBytes > 0)
        {
            double freedMB = freedBytes / (1024.0 * 1024);
            StatusText.Text = Localization.StartupCleanCompleted;
            FreedMemoryText.Text = Localization.FreedOnStartup(freedMB);
            _trayIcon?.ShowBalloon(Localization.StartupCleanBalloon(freedMB), Localization.StartupCleanTitle);
        }
        else
        {
            StatusText.Text = Localization.Ready;
            FreedMemoryText.Text = Localization.MemoryAlreadyOptimized;
        }
        
        UpdateMemoryInfo();
    }

    private void UpdateMemoryInfo()
    {
        var memInfo = MemoryInfo.GetMemoryStatus();
        
        double usedGB = (memInfo.TotalPhysicalMemory - memInfo.AvailablePhysicalMemory) / (1024.0 * 1024 * 1024);
        double totalGB = memInfo.TotalPhysicalMemory / (1024.0 * 1024 * 1024);
        double usagePercent = memInfo.MemoryLoad;
        
        MemoryUsageText.Text = $"{usedGB:F1} / {totalGB:F1} GB";
        MemoryPercentText.Text = $"{usagePercent}%";
        MemoryProgressBar.Value = usagePercent;
        
        // Update tray tooltip
        _trayIcon?.UpdateTooltip(Localization.TrayTooltip(usedGB, totalGB, usagePercent));
    }

    private void SaveMemoryBeforeClean()
    {
        var memInfo = MemoryInfo.GetMemoryStatus();
        _memoryBeforeClean = (long)(memInfo.TotalPhysicalMemory - memInfo.AvailablePhysicalMemory);
    }

    private void ShowFreedMemory()
    {
        var memInfo = MemoryInfo.GetMemoryStatus();
        long memoryAfterClean = (long)(memInfo.TotalPhysicalMemory - memInfo.AvailablePhysicalMemory);
        long freedBytes = _memoryBeforeClean - memoryAfterClean;
        
        if (freedBytes > 0)
        {
            double freedMB = freedBytes / (1024.0 * 1024);
            FreedMemoryText.Text = Localization.FreedMB(freedMB);
        }
        else
        {
            FreedMemoryText.Text = Localization.MemoryOptimized;
        }
        
        UpdateMemoryInfo();
    }

    private void CleanOnStartup_Click(object sender, RoutedEventArgs e)
    {
        _settings.CleanOnStartup = !_settings.CleanOnStartup;
        UpdateToggleVisual(_settings.CleanOnStartup);
        _settings.Save();
        _trayIcon?.UpdateCleanOnStartupMenu(_settings.CleanOnStartup);
    }

    private void AutoCleanInterval_Click(object sender, RoutedEventArgs e)
    {
        // Cycle through interval options: 0 -> 5 -> 10 -> 15 -> 30 -> 45 -> 60 -> 120 -> 180 -> 0
        int currentIndex = Array.IndexOf(_intervalOptions, _settings.AutoCleanIntervalMinutes);
        int nextIndex = (currentIndex + 1) % _intervalOptions.Length;
        _settings.AutoCleanIntervalMinutes = _intervalOptions[nextIndex];
        _settings.Save();
        
        UpdateAutoCleanIntervalUI();
        SetupAutoCleanTimer();
    }

    private void AutoCleanHighRam_Click(object sender, RoutedEventArgs e)
    {
        _settings.AutoCleanOnHighUsage = !_settings.AutoCleanOnHighUsage;
        UpdateHighRamToggleVisual(_settings.AutoCleanOnHighUsage);
        _settings.Save();
    }

    private void LanguageToggle_Click(object sender, RoutedEventArgs e)
    {
        // Toggle language
        if (Localization.CurrentLanguage == Localization.Language.English)
        {
            Localization.CurrentLanguage = Localization.Language.Vietnamese;
            _settings.Language = "Vietnamese";
        }
        else
        {
            Localization.CurrentLanguage = Localization.Language.English;
            _settings.Language = "English";
        }
        _settings.Save();
        _trayIcon?.RefreshMenu();
    }

    private void ThemeToggle_Click(object sender, RoutedEventArgs e)
    {
        // Toggle theme
        if (ThemeManager.CurrentTheme == ThemeManager.Theme.Dark)
        {
            ThemeManager.CurrentTheme = ThemeManager.Theme.Light;
            _settings.Theme = "Light";
        }
        else
        {
            ThemeManager.CurrentTheme = ThemeManager.Theme.Dark;
            _settings.Theme = "Dark";
        }
        _settings.Save();
        _trayIcon?.RefreshMenu();
    }

    private async void CleanAll_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = Localization.CleaningAllMemory;
        FreedMemoryText.Text = "";
        SaveMemoryBeforeClean();
        
        await Task.Run(() =>
        {
            _memoryCleaner.EmptyWorkingSets();
            _memoryCleaner.EmptySystemWorkingSet();
            _memoryCleaner.EmptyModifiedPageList();
            _memoryCleaner.EmptyStandbyList();
        });
        
        StatusText.Text = Localization.Completed;
        ShowFreedMemory();
    }

    private async void EmptyWorkingSets_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = Localization.Flushing("Working Sets");
        FreedMemoryText.Text = "";
        SaveMemoryBeforeClean();
        
        await Task.Run(() => _memoryCleaner.EmptyWorkingSets());
        
        StatusText.Text = Localization.Completed;
        ShowFreedMemory();
    }

    private async void EmptySystemWorkingSet_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = Localization.Flushing("System Working Set");
        FreedMemoryText.Text = "";
        SaveMemoryBeforeClean();
        
        await Task.Run(() => _memoryCleaner.EmptySystemWorkingSet());
        
        StatusText.Text = Localization.Completed;
        ShowFreedMemory();
    }

    private async void EmptyModifiedPageList_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = Localization.Flushing("Modified Page List");
        FreedMemoryText.Text = "";
        SaveMemoryBeforeClean();
        
        await Task.Run(() => _memoryCleaner.EmptyModifiedPageList());
        
        StatusText.Text = Localization.Completed;
        ShowFreedMemory();
    }

    private async void EmptyStandbyList_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = Localization.Flushing("Standby List");
        FreedMemoryText.Text = "";
        SaveMemoryBeforeClean();
        
        await Task.Run(() => _memoryCleaner.EmptyStandbyList());
        
        StatusText.Text = Localization.Completed;
        ShowFreedMemory();
    }

    private async void EmptyPriority0StandbyList_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = Localization.Flushing("Priority 0 Standby List");
        FreedMemoryText.Text = "";
        SaveMemoryBeforeClean();
        
        await Task.Run(() => _memoryCleaner.EmptyPriority0StandbyList());
        
        StatusText.Text = Localization.Completed;
        ShowFreedMemory();
    }

    private async void CleanDisk_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = Localization.CleaningDisk;
        FreedMemoryText.Text = "";
        
        var result = await Task.Run(() =>
        {
            var tempResult = _memoryCleaner.CleanTempFiles();
            _memoryCleaner.EmptyRecycleBin();
            return tempResult;
        });
        
        StatusText.Text = Localization.Completed;
        double mbFreed = result.bytesFreed / (1024.0 * 1024);
        FreedMemoryText.Text = Localization.DiskCleanResult(result.filesDeleted, mbFreed);
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
            return;
        DragMove();
    }

    private void Minimize_Click(object sender, RoutedEventArgs e)
    {
        // Minimize to system tray
        Hide();
        _trayIcon?.ShowBalloon(Localization.RunningInBackground, Localization.MinimizedToTray);
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        // Minimize to tray instead of closing
        Hide();
        _trayIcon?.ShowBalloon(Localization.StillRunning, Localization.MinimizedToTray);
    }

    private void ShowWindow()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void ExitApplication()
    {
        _isExiting = true;
        _trayIcon?.Dispose();
        _trayIcon = null;
        System.Windows.Application.Current.Shutdown();
    }

    private void Facebook_Click(object sender, RoutedEventArgs e)
    {
        OpenUrl("https://www.facebook.com/rain.107/");
    }

    private void GitHub_Click(object sender, RoutedEventArgs e)
    {
        OpenUrl("https://github.com/rainaku/");
    }

    private void Website_Click(object sender, RoutedEventArgs e)
    {
        OpenUrl("https://rainaku.id.vn");
    }

    private void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch
        {
            // Ignore if can't open browser
        }
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (!_isExiting)
        {
            // Minimize to tray instead of closing
            e.Cancel = true;
            Hide();
        }
        base.OnClosing(e);
    }
}
