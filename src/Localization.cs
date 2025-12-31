namespace FreeMyRam;

/// <summary>
/// Manages application localization/language strings
/// </summary>
public static class Localization
{
    public enum Language
    {
        English,
        Vietnamese
    }

    private static Language _currentLanguage = Language.English;
    public static event Action? LanguageChanged;

    public static Language CurrentLanguage
    {
        get => _currentLanguage;
        set
        {
            if (_currentLanguage != value)
            {
                _currentLanguage = value;
                LanguageChanged?.Invoke();
            }
        }
    }

    // UI Strings
    public static string AppTitle => CurrentLanguage switch
    {
        Language.Vietnamese => "🧹 FreeMyRam v1.2 by rainaku",
        _ => "🧹 FreeMyRam v1.2 by rainaku"
    };

    public static string MemoryUsage => CurrentLanguage switch
    {
        Language.Vietnamese => "RAM đang sử dụng",
        _ => "Memory Usage"
    };

    public static string CachedMemory => CurrentLanguage switch
    {
        Language.Vietnamese => "RAM Cached",
        _ => "Cached"
    };

    public static string QuickActions => CurrentLanguage switch
    {
        Language.Vietnamese => "THAO TÁC NHANH",
        _ => "QUICK ACTIONS"
    };

    public static string CleanAllMemory => CurrentLanguage switch
    {
        Language.Vietnamese => "Dọn dẹp nhanh",
        _ => "Clean All Memory"
    };

    public static string AdvancedOptions => CurrentLanguage switch
    {
        Language.Vietnamese => "🔧 TÙY CHỌN NÂNG CAO",
        _ => "🔧 ADVANCED OPTIONS"
    };

    public static string DevModeActivated => CurrentLanguage switch
    {
        Language.Vietnamese => "🔓 Chế độ Developer đã kích hoạt",
        _ => "🔓 Developer Mode Activated"
    };

    public static string HideDevMode => CurrentLanguage switch
    {
        Language.Vietnamese => "🔒 Ẩn chế độ Developer",
        _ => "🔒 Hide Developer Mode"
    };

    public static string FlushWorkingSets => CurrentLanguage switch
    {
        Language.Vietnamese => "Xóa Working Sets",
        _ => "Flush Working Sets"
    };

    public static string FlushSystemWorkingSet => CurrentLanguage switch
    {
        Language.Vietnamese => "Xóa System Working Set",
        _ => "Flush System Working Set"
    };

    public static string FlushModifiedPageList => CurrentLanguage switch
    {
        Language.Vietnamese => "Xóa Modified Page List",
        _ => "Flush Modified Page List"
    };

    public static string FlushStandbyList => CurrentLanguage switch
    {
        Language.Vietnamese => "Xóa Standby List",
        _ => "Flush Standby List"
    };

    public static string FlushPriority0StandbyList => CurrentLanguage switch
    {
        Language.Vietnamese => "Xóa Priority 0 Standby List",
        _ => "Flush Priority 0 Standby List"
    };

    public static string CleanTempFiles => CurrentLanguage switch
    {
        Language.Vietnamese => "Dọn dẹp File Tạm (Temp)",
        _ => "Clean Temp Files"
    };

    public static string EmptyRecycleBin => CurrentLanguage switch
    {
        Language.Vietnamese => "Dọn dẹp Thùng Rác",
        _ => "Empty Recycle Bin"
    };

    public static string CleanDisk => CurrentLanguage switch
    {
        Language.Vietnamese => "Dọn dẹp file tạm & thùng rác",
        _ => "Clean Temp & Recycle Bin"
    };

    public static string DiskCleanup => CurrentLanguage switch
    {
        Language.Vietnamese => "DỌN DẸP Ổ ĐĨA",
        _ => "DISK CLEANUP"
    };

    public static string CleaningTempFiles => CurrentLanguage switch
    {
        Language.Vietnamese => "Đang dọn dẹp file tạm...",
        _ => "Cleaning temp files..."
    };

    public static string CleaningDisk => CurrentLanguage switch
    {
        Language.Vietnamese => "Đang dọn dẹp ổ đĩa...",
        _ => "Cleaning disk..."
    };

    public static string EmptyingRecycleBin => CurrentLanguage switch
    {
        Language.Vietnamese => "Đang dọn dẹp thùng rác...",
        _ => "Emptying recycle bin..."
    };

    public static string TempFilesResult(int filesDeleted, double mbFreed) => CurrentLanguage switch
    {
        Language.Vietnamese => $"✓ Đã xóa {filesDeleted} file ({mbFreed:F1} MB)",
        _ => $"✓ Deleted {filesDeleted} files ({mbFreed:F1} MB)"
    };

    public static string DiskCleanResult(int filesDeleted, double mbFreed) => CurrentLanguage switch
    {
        Language.Vietnamese => $"✓ Đã dọn dẹp {filesDeleted} file temp + thùng rác ({mbFreed:F1} MB)",
        _ => $"✓ Cleaned {filesDeleted} temp files + recycle bin ({mbFreed:F1} MB)"
    };

    public static string RecycleBinEmptied => CurrentLanguage switch
    {
        Language.Vietnamese => "✓ Đã dọn dẹp thùng rác",
        _ => "✓ Recycle bin emptied"
    };

    public static string RecycleBinAlreadyEmpty => CurrentLanguage switch
    {
        Language.Vietnamese => "✓ Thùng rác đã trống",
        _ => "✓ Recycle bin already empty"
    };

    public static string Settings => CurrentLanguage switch
    {
        Language.Vietnamese => "CÀI ĐẶT",
        _ => "SETTINGS"
    };

    public static string CleanOnStartup => CurrentLanguage switch
    {
        Language.Vietnamese => "Tự động dọn dẹp khi khởi động",
        _ => "Clean on Startup"
    };

    public static string StartWithWindows => CurrentLanguage switch
    {
        Language.Vietnamese => "Khởi động cùng Windows",
        _ => "Start with Windows"
    };

    public static string AutoCleanInterval => CurrentLanguage switch
    {
        Language.Vietnamese => "Tự động dọn dẹp mỗi",
        _ => "Auto clean every"
    };

    public static string AutoCleanDisabled => CurrentLanguage switch
    {
        Language.Vietnamese => "Tắt",
        _ => "Off"
    };

    public static string Minutes => CurrentLanguage switch
    {
        Language.Vietnamese => "phút",
        _ => "min"
    };

    public static string AutoCleanOnHighRam => CurrentLanguage switch
    {
        Language.Vietnamese => "Tự động dọn khi RAM > 70%",
        _ => "Auto clean when RAM > 70%"
    };

    public static string AutoCleanedRam => CurrentLanguage switch
    {
        Language.Vietnamese => "Đã tự động dọn dẹp RAM",
        _ => "Auto cleaned RAM"
    };

    public static string AutoCleanBalloon(double mb) => CurrentLanguage switch
    {
        Language.Vietnamese => $"Đã tự động dọn dẹp {mb:F0} MB RAM!",
        _ => $"Auto cleaned {mb:F0} MB of RAM!"
    };

    public static string AutoCleanTitle => CurrentLanguage switch
    {
        Language.Vietnamese => "FreeMyRam - Tự động dọn dẹp",
        _ => "FreeMyRam - Auto Clean"
    };

    public static string Language_Option => CurrentLanguage switch
    {
        Language.Vietnamese => "Ngôn ngữ: Tiếng Việt",
        _ => "Language: English"
    };

    public static string Theme_Dark => CurrentLanguage switch
    {
        Language.Vietnamese => "Giao diện: Tối",
        _ => "Theme: Dark"
    };

    public static string Theme_Light => CurrentLanguage switch
    {
        Language.Vietnamese => "Giao diện: Sáng",
        _ => "Theme: Light"
    };

    public static string ThemeOption => ThemeManager.CurrentTheme == ThemeManager.Theme.Dark 
        ? Theme_Dark 
        : Theme_Light;

    public static string Ready => CurrentLanguage switch
    {
        Language.Vietnamese => "Sẵn sàng",
        _ => "Ready"
    };

    public static string Completed => CurrentLanguage switch
    {
        Language.Vietnamese => "Hoàn thành!",
        _ => "Completed!"
    };

    public static string CleaningAllMemory => CurrentLanguage switch
    {
        Language.Vietnamese => "Đang dọn dẹp toàn bộ bộ nhớ...",
        _ => "Cleaning all memory..."
    };

    public static string AutoCleaningOnStartup => CurrentLanguage switch
    {
        Language.Vietnamese => "Đang tự động dọn dẹp khi khởi động...",
        _ => "Auto-cleaning on startup..."
    };

    public static string StartupCleanCompleted => CurrentLanguage switch
    {
        Language.Vietnamese => "Dọn dẹp khởi động hoàn tất!",
        _ => "Startup clean completed!"
    };

    public static string MemoryAlreadyOptimized => CurrentLanguage switch
    {
        Language.Vietnamese => "✓ Bộ nhớ đã được tối ưu",
        _ => "✓ Memory already optimized"
    };

    public static string MemoryOptimized => CurrentLanguage switch
    {
        Language.Vietnamese => "✓ Đã tối ưu bộ nhớ",
        _ => "✓ Memory optimized"
    };

    public static string FreedMB(double mb) => CurrentLanguage switch
    {
        Language.Vietnamese => $"✓ Đã dọn dẹp {mb:F0} MB",
        _ => $"✓ Cleaned {mb:F0} MB"
    };

    public static string FreedOnStartup(double mb) => CurrentLanguage switch
    {
        Language.Vietnamese => $"✓ Đã dọn dẹp {mb:F0} MB khi khởi động",
        _ => $"✓ Cleaned {mb:F0} MB on startup"
    };

    public static string Flushing(string item) => CurrentLanguage switch
    {
        Language.Vietnamese => $"Đang xóa {item}...",
        _ => $"Flushing {item}..."
    };

    // Tray menu strings
    public static string ShowWindow => CurrentLanguage switch
    {
        Language.Vietnamese => "📊 Hiển thị cửa sổ",
        _ => "📊 Show Window"
    };

    public static string Advanced => CurrentLanguage switch
    {
        Language.Vietnamese => "🔧 Nâng cao",
        _ => "🔧 Advanced"
    };

    public static string SettingsMenu => CurrentLanguage switch
    {
        Language.Vietnamese => "⚙️ Cài đặt",
        _ => "⚙️ Settings"
    };

    public static string Exit => CurrentLanguage switch
    {
        Language.Vietnamese => "❌ Thoát",
        _ => "❌ Exit"
    };

    public static string MinimizedToTray => CurrentLanguage switch
    {
        Language.Vietnamese => "Thu nhỏ vào khay",
        _ => "Minimized to Tray"
    };

    public static string RunningInBackground => CurrentLanguage switch
    {
        Language.Vietnamese => "FreeMyRam đang chạy ở nền",
        _ => "FreeMyRam is running in the background"
    };

    public static string StillRunning => CurrentLanguage switch
    {
        Language.Vietnamese => "FreeMyRam vẫn đang chạy. Nhấp chuột phải vào biểu tượng khay để thoát.",
        _ => "FreeMyRam is still running. Right-click tray icon to exit."
    };

    public static string TrayTooltip(double usedGB, double totalGB, double percent) => CurrentLanguage switch
    {
        Language.Vietnamese => $"FreeMyRam - {usedGB:F1}/{totalGB:F1} GB ({percent}%)",
        _ => $"FreeMyRam - {usedGB:F1}/{totalGB:F1} GB ({percent}%)"
    };

    public static string StartupCleanBalloon(double mb) => CurrentLanguage switch
    {
        Language.Vietnamese => $"Đã dọn dẹp {mb:F0} MB bộ nhớ!",
        _ => $"Cleaned {mb:F0} MB of memory!"
    };

    public static string StartupCleanTitle => CurrentLanguage switch
    {
        Language.Vietnamese => "FreeMyRam - Dọn dẹp khởi động",
        _ => "FreeMyRam - Startup Clean"
    };

    public static string Flushed(string item) => CurrentLanguage switch
    {
        Language.Vietnamese => $"Đã xóa {item}",
        _ => $"{item} flushed"
    };

    public static string FreedMemoryBalloon(double mb) => CurrentLanguage switch
    {
        Language.Vietnamese => $"Đã dọn dẹp {mb:F0} MB bộ nhớ!",
        _ => $"Cleaned {mb:F0} MB of memory!"
    };

    public static string MemoryOptimizedBalloon => CurrentLanguage switch
    {
        Language.Vietnamese => "Bộ nhớ đã được tối ưu!",
        _ => "Memory optimized!"
    };

    public static string DevModeWarning => CurrentLanguage switch
    {
        Language.Vietnamese => "Đừng tùy tiện thử trừ khi bạn biết bạn đang làm gì !",
        _ => "Do not try this unless you know what you're doing!"
    };
}
