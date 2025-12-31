using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Diagnostics;
using FreeMyRam.Developer;

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
    
    // Developer mode manager - secret code entry
    private readonly DevModeManager _devModeManager;
    
    // Auto clean interval options in minutes
    private static readonly int[] _intervalOptions = { 0, 5, 10, 15, 30, 45, 60, 120, 180 };
    
    // Cooldown for RAM threshold auto clean (10 minutes)
    private DateTime _lastHighRamClean = DateTime.MinValue;
    private static readonly TimeSpan _highRamCooldown = TimeSpan.FromMinutes(10);
    
    // Cached brushes for performance (avoid creating new objects)
    private static readonly SolidColorBrush ToggleOnBrush = new((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#4CAF50"));
    private static readonly SolidColorBrush ToggleOffBrush = new((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#555555"));
    
    // Memory Status Brushes
    private static readonly SolidColorBrush NormalMemBrush = new((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#0d6efd")); // Blue (matches Light theme highlight)
    private static readonly SolidColorBrush WarningMemBrush = new((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFD700")); // Gold/Yellow
    private static readonly SolidColorBrush CriticalMemBrush = new((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF4444")); // Red
    
    static MainWindow()
    {
        // Freeze brushes for better performance
        ToggleOnBrush.Freeze();
        ToggleOffBrush.Freeze();
        NormalMemBrush.Freeze();
        WarningMemBrush.Freeze();
        CriticalMemBrush.Freeze();
    }

    public MainWindow()
    {
        InitializeComponent();
        _memoryCleaner = new MemoryCleaner();
        // Use cached settings from App if available, otherwise load (fallback)
        _settings = App.CachedSettings ?? AppSettings.Load();
        
        // Apply settings to UI immediately (lightweight operations)
        UpdateToggleVisual(_settings.CleanOnStartup);
        UpdateStartupToggleVisual(_settings.StartWithWindows);
        UpdateHighRamToggleVisual(_settings.AutoCleanOnHighUsage);
        UpdateAutoCleanIntervalUI();
        
        // Apply language setting (already set in App.xaml.cs, just subscribe)
        Localization.CurrentLanguage = _settings.Language == "Vietnamese" 
            ? Localization.Language.Vietnamese 
            : Localization.Language.English;
        Localization.LanguageChanged += UpdateUILanguage;
        
        // Apply theme setting (already applied in App.xaml.cs, just subscribe)
        ThemeManager.ThemeChanged += UpdateUITheme;
        
        UpdateUILanguage();
        UpdateUITheme();
        
        // Initialize timers (but don't start yet)
        _updateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500) // Update every 0.5 seconds for responsive UI
        };
        _updateTimer.Tick += UpdateTimer_Tick;
        
        _autoCleanTimer = new DispatcherTimer();
        _autoCleanTimer.Tick += async (s, e) => await PerformAutoClean();
        
        // Setup developer mode manager
        _devModeManager = new DevModeManager();
        _devModeManager.DevModeActivated += OnDevModeActivated;
        _devModeManager.DevModeDeactivated += OnDevModeDeactivated;
        
        // Ensure window can receive keyboard input
        Focusable = true;
        
        // Defer heavy initialization to after window is shown (improves perceived startup time)
        Loaded += OnWindowLoaded;
    }
    
    private async void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        // Initialize system tray (deferred for faster window display)
        _trayIcon = new TrayIcon(_memoryCleaner, _settings, ShowWindow, ExitApplication, OnSettingsChanged);
        
        // Start timers after window is loaded
        _updateTimer.Start();
        SetupAutoCleanTimer();
        
        // Initial memory update
        UpdateMemoryInfo();
        
        // Focus the window
        Focus();
        
        // Auto-clean on startup if enabled
        if (_settings.CleanOnStartup)
        {
            await AutoCleanOnStartup();
        }
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
        CachedMemoryLabel.Text = Localization.CachedMemory;
        QuickActionsLabel.Text = Localization.QuickActions;
        CleanAllButton.Content = Localization.CleanAllMemory;
        HiddenAdvancedOptionsLabel.Text = Localization.AdvancedOptions;
        FlushWorkingSetsBtn.Content = Localization.FlushWorkingSets;
        FlushSystemWorkingSetBtn.Content = Localization.FlushSystemWorkingSet;
        FlushModifiedPageListBtn.Content = Localization.FlushModifiedPageList;
        FlushStandbyListBtn.Content = Localization.FlushStandbyList;
        FlushPriority0StandbyListBtn.Content = Localization.FlushPriority0StandbyList;
        CleanDiskBtn.Content = Localization.CleanDisk;
        DevModeWarningText.Text = Localization.DevModeWarning;
        SettingsLabel.Text = Localization.Settings;
        CleanOnStartupText.Text = Localization.CleanOnStartup;
        StartWithWindowsText.Text = Localization.StartWithWindows;
        AutoCleanIntervalText.Text = Localization.AutoCleanInterval;
        UpdateAutoCleanIntervalUI();
        AutoCleanHighRamText.Text = Localization.AutoCleanOnHighRam;
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

    private void UpdateStartupToggleVisual(bool isOn)
    {
        if (isOn)
        {
            ToggleStartupBorder.Background = ToggleOnBrush;
            ToggleStartupCircle.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
            ToggleStartupCircle.Margin = ToggleOnMargin;
        }
        else
        {
            ToggleStartupBorder.Background = ToggleOffBrush;
            ToggleStartupCircle.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            ToggleStartupCircle.Margin = ToggleOffMargin;
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
        
        // Animate progress bar
        var animation = new DoubleAnimation
        {
            To = usagePercent,
            Duration = TimeSpan.FromMilliseconds(500),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        MemoryProgressBar.BeginAnimation(System.Windows.Controls.Primitives.RangeBase.ValueProperty, animation);
        
        // Update progress bar color based on usage
        if (usagePercent > 80)
        {
            MemoryProgressBar.Foreground = CriticalMemBrush;
            MemoryPercentText.Foreground = CriticalMemBrush;
        }
        else if (usagePercent > 50)
        {
            MemoryProgressBar.Foreground = WarningMemBrush;
            MemoryPercentText.Foreground = WarningMemBrush;
        }
        else
        {
            MemoryProgressBar.Foreground = NormalMemBrush;
            MemoryPercentText.Foreground = NormalMemBrush;
        }
        
        // Update cached memory
        ulong cachedBytes = MemoryInfo.GetCachedBytes();
        double cachedGB = cachedBytes / (1024.0 * 1024 * 1024);
        CachedMemoryText.Text = $"{cachedGB:F2} GB";
        
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

    private void StartWithWindows_Click(object sender, RoutedEventArgs e)
    {
        _settings.StartWithWindows = !_settings.StartWithWindows;
        UpdateStartupToggleVisual(_settings.StartWithWindows);
        AppSettings.SetStartWithWindows(_settings.StartWithWindows);
        _settings.Save();
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

    private void VietnameseLanguage_Click(object sender, RoutedEventArgs e)
    {
        if (Localization.CurrentLanguage != Localization.Language.Vietnamese)
        {
            Localization.CurrentLanguage = Localization.Language.Vietnamese;
            _settings.Language = "Vietnamese";
            _settings.Save();
            _trayIcon?.RefreshMenu();
        }
    }

    private void EnglishLanguage_Click(object sender, RoutedEventArgs e)
    {
        if (Localization.CurrentLanguage != Localization.Language.English)
        {
            Localization.CurrentLanguage = Localization.Language.English;
            _settings.Language = "English";
            _settings.Save();
            _trayIcon?.RefreshMenu();
        }
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

    #region Developer Mode - Secret Code: 10720040
    
    /// <summary>
    /// Handle keyboard input for secret developer code detection.
    /// The code is typed directly without any input field.
    /// </summary>
    private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        // Process the key through DevModeManager
        _devModeManager.ProcessKeyDown(e);
    }
    
    /// <summary>
    /// Called when developer mode is activated via secret code.
    /// </summary>
    private void OnDevModeActivated()
    {
        // Show the developer panel with animation
        SecretDevPanel.Visibility = Visibility.Visible;
        
        // Create slide-down and fade-in animation
        var slideAnimation = new DoubleAnimation
        {
            From = -20,
            To = 0,
            Duration = TimeSpan.FromMilliseconds(300),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        
        var fadeAnimation = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = TimeSpan.FromMilliseconds(300)
        };
        
        // Apply transform if not exists
        if (SecretDevPanel.RenderTransform is not TranslateTransform)
        {
            SecretDevPanel.RenderTransform = new TranslateTransform();
        }
        
        SecretDevPanel.RenderTransform.BeginAnimation(TranslateTransform.YProperty, slideAnimation);
        SecretDevPanel.BeginAnimation(OpacityProperty, fadeAnimation);
        
        // Show notification
        StatusText.Text = "🔓 Developer Mode Activated";
        FreedMemoryText.Text = "";
    }
    
    /// <summary>
    /// Called when developer mode is deactivated.
    /// </summary>
    private void OnDevModeDeactivated()
    {
        // Hide the developer panel with animation
        var fadeAnimation = new DoubleAnimation
        {
            From = 1,
            To = 0,
            Duration = TimeSpan.FromMilliseconds(200)
        };
        
        fadeAnimation.Completed += (s, e) =>
        {
            SecretDevPanel.Visibility = Visibility.Collapsed;
        };
        
        SecretDevPanel.BeginAnimation(OpacityProperty, fadeAnimation);
        
        // Show notification
        StatusText.Text = Localization.Ready;
    }
    
    /// <summary>
    /// Hide developer mode button click handler.
    /// </summary>
    private void HideDevMode_Click(object sender, RoutedEventArgs e)
    {
        _devModeManager.DeactivateDevMode();
    }

    [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
    private static extern IntPtr VirtualAlloc(IntPtr lpAddress, IntPtr dwSize, uint flAllocationType, uint flProtect);

    [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
    private static extern bool VirtualFree(IntPtr lpAddress, IntPtr dwSize, uint dwFreeType);

    private const uint MEM_COMMIT = 0x1000;
    private const uint MEM_RESERVE = 0x2000;
    private const uint PAGE_READWRITE = 0x04;
    private const uint MEM_RELEASE = 0x8000;

    // List to hold allocated memory pointers
    private List<IntPtr> _memoryStressDump = new();
    private bool _isStressTesting = false;

    private async void TestHighRam_Click(object sender, RoutedEventArgs e)
    {
        if (_isStressTesting)
        {
            // Stop stress test
            _isStressTesting = false;
            
            // Free unmanaged memory
            foreach (var ptr in _memoryStressDump)
            {
                VirtualFree(ptr, IntPtr.Zero, MEM_RELEASE);
            }
            _memoryStressDump.Clear();
            
            GC.Collect();
            GC.WaitForPendingFinalizers();
            
            TestHighRamBtn.Content = "⚠️ Test: Fill RAM to 85%";
            StatusText.Text = "Stress test stopped";
            return;
        }

        _isStressTesting = true;
        TestHighRamBtn.Content = "⏹️ Stop Stress Test";
        StatusText.Text = "Filling RAM to 85%...";
        
        await Task.Run(async () =>
        {
            try 
            {
                var memInfo = MemoryInfo.GetMemoryStatus();
                ulong totalRam = memInfo.TotalPhysicalMemory;
                ulong targetUsage = (ulong)(totalRam * 0.85);
                long allocatedSoFar = 0;
                
                // 100MB chunks
                int chunkSize = 100 * 1024 * 1024;
                
                while (_isStressTesting)
                {
                    memInfo = MemoryInfo.GetMemoryStatus();
                    ulong currentUsage = memInfo.TotalPhysicalMemory - memInfo.AvailablePhysicalMemory;
                    
                    if (currentUsage >= targetUsage)
                    {
                        // Update UI with status
                        System.Windows.Application.Current.Dispatcher.Invoke(() => 
                        {
                            double allocatedGB = allocatedSoFar / (1024.0 * 1024 * 1024);
                            StatusText.Text = $"Holding @ 85% (Alloc: {allocatedGB:F1} GB)";
                        });
                        
                        await Task.Delay(500);
                        continue;
                    }
                    
                    // Reduce chunk size if close to target
                    if (targetUsage > currentUsage && (targetUsage - currentUsage) < (ulong)chunkSize)
                    {
                        chunkSize = 10 * 1024 * 1024; // 10MB
                    }
                    
                    try
                    {
                        // Use VirtualAlloc for better control over large allocations
                        IntPtr ptr = VirtualAlloc(IntPtr.Zero, (IntPtr)chunkSize, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
                        
                        if (ptr == IntPtr.Zero)
                        {
                            // Allocation failed
                            await Task.Delay(100);
                            break;
                        }
                        
                        // Touch memory to ensure it's actually committed to RAM
                        unsafe 
                        {
                            byte* p = (byte*)ptr;
                            // Touch every 4KB (Page Size) to force OS to back it with physical RAM
                            int pageSize = 4096;
                            for(int i=0; i < chunkSize; i += pageSize)
                            {
                                *(p + i) = 1;
                            }
                            // Touch last byte
                            *(p + chunkSize - 1) = 1;
                        }
                        
                        _memoryStressDump.Add(ptr);
                        allocatedSoFar += chunkSize;
                        
                        // Update UI occasionally
                        if (_memoryStressDump.Count % 5 == 0)
                        {
                            System.Windows.Application.Current.Dispatcher.Invoke(() => 
                            {
                                double allocatedGB = allocatedSoFar / (1024.0 * 1024 * 1024);
                                StatusText.Text = $"Filling... (Alloc: {allocatedGB:F1} GB)";
                            });
                        }
                        
                        await Task.Delay(10); 
                    }
                    catch (Exception)
                    {
                        // Ignore allocation failures and continue or break
                        break;
                    }
                }
            }
            catch (Exception)
            {
                // Ignore errors
            }
        });
        
        if (!_isStressTesting)
        {
             foreach (var ptr in _memoryStressDump)
             {
                 VirtualFree(ptr, IntPtr.Zero, MEM_RELEASE);
             }
             _memoryStressDump.Clear();
             GC.Collect();
        }
    }
    
    #endregion
}
