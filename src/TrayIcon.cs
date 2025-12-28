using System.Drawing;
using System.Windows;
using Forms = System.Windows.Forms;

namespace FreeMyRam;

/// <summary>
/// Manages the system tray icon and context menu
/// </summary>
public class TrayIcon : IDisposable
{
    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly MemoryCleaner _memoryCleaner;
    private readonly AppSettings _settings;
    private readonly Action _showWindowAction;
    private readonly Action _exitAction;
    private readonly Action _onSettingsChanged;
    private Forms.ToolStripMenuItem? _cleanOnStartupItem;
    private Forms.ToolStripMenuItem? _languageItem;
    private bool _disposed;

    public TrayIcon(MemoryCleaner memoryCleaner, AppSettings settings, Action showWindowAction, Action exitAction, Action onSettingsChanged)
    {
        _memoryCleaner = memoryCleaner;
        _settings = settings;
        _showWindowAction = showWindowAction;
        _exitAction = exitAction;
        _onSettingsChanged = onSettingsChanged;

        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = CreateDefaultIcon(),
            Text = "FreeMyRam - Click to open",
            Visible = true,
            ContextMenuStrip = CreateContextMenu()
        };

        _notifyIcon.DoubleClick += (s, e) => _showWindowAction();
        
        // Subscribe to language changes
        Localization.LanguageChanged += OnLanguageChanged;
    }

    private void OnLanguageChanged()
    {
        // Refresh the context menu when language changes
        _notifyIcon.ContextMenuStrip?.Dispose();
        _notifyIcon.ContextMenuStrip = CreateContextMenu();
    }

    public void RefreshMenu()
    {
        OnLanguageChanged();
    }

    private static Icon CreateDefaultIcon()
    {
        // Create a better looking icon programmatically
        using var bitmap = new Bitmap(32, 32);
        using var g = Graphics.FromImage(bitmap);
        
        // Enable anti-aliasing for smoother edges
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        
        // Dark gradient background
        using var bgBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
            new Rectangle(0, 0, 32, 32),
            Color.FromArgb(26, 26, 46),
            Color.FromArgb(15, 52, 96),
            System.Drawing.Drawing2D.LinearGradientMode.ForwardDiagonal);
        g.FillRectangle(bgBrush, 0, 0, 32, 32);
        
        // RAM chip body with gradient
        using var chipBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
            new Rectangle(6, 9, 20, 14),
            Color.FromArgb(233, 69, 96),
            Color.FromArgb(199, 62, 84),
            System.Drawing.Drawing2D.LinearGradientMode.ForwardDiagonal);
        
        // Draw rounded RAM chip
        using var chipPath = new System.Drawing.Drawing2D.GraphicsPath();
        chipPath.AddRoundedRectangle(new Rectangle(6, 9, 20, 14), 2);
        g.FillPath(chipBrush, chipPath);
        
        // RAM notch
        g.FillRectangle(new SolidBrush(Color.FromArgb(26, 26, 46)), 13, 9, 4, 3);
        
        // Draw pins (top)
        using var pinBrush = new SolidBrush(Color.FromArgb(234, 234, 234));
        int[] pinPositions = { 8, 12, 18, 22 };
        foreach (int x in pinPositions)
        {
            g.FillRectangle(pinBrush, x, 5, 2, 4);
            g.FillRectangle(pinBrush, x, 23, 2, 4);
        }
        
        // Memory chips on RAM
        using var memBrush = new SolidBrush(Color.FromArgb(22, 33, 62));
        g.FillRectangle(memBrush, 8, 12, 4, 8);
        g.FillRectangle(memBrush, 14, 12, 4, 8);
        g.FillRectangle(memBrush, 20, 12, 4, 8);
        
        // Sparkle effect (clean indicator)
        using var sparkleBrush = new SolidBrush(Color.FromArgb(230, 255, 255, 255));
        g.FillEllipse(sparkleBrush, 25, 3, 5, 5);
        using var sparkle2Brush = new SolidBrush(Color.FromArgb(150, 255, 255, 255));
        g.FillEllipse(sparkle2Brush, 28, 7, 3, 3);

        return Icon.FromHandle(bitmap.GetHicon());
    }

    private Forms.ContextMenuStrip CreateContextMenu()
    {
        var menu = new Forms.ContextMenuStrip();
        
        // Header
        var header = new Forms.ToolStripLabel("🧹 FreeMyRam")
        {
            Font = new Font("Segoe UI", 9, System.Drawing.FontStyle.Bold)
        };
        menu.Items.Add(header);
        menu.Items.Add(new Forms.ToolStripSeparator());

        // Show Window
        var showItem = new Forms.ToolStripMenuItem(Localization.ShowWindow);
        showItem.Click += (s, e) => _showWindowAction();
        menu.Items.Add(showItem);

        menu.Items.Add(new Forms.ToolStripSeparator());

        // Quick Actions
        var cleanAllItem = new Forms.ToolStripMenuItem(Localization.CleanAllMemory);
        cleanAllItem.Click += (s, e) => CleanAllMemory();
        menu.Items.Add(cleanAllItem);

        menu.Items.Add(new Forms.ToolStripSeparator());

        // Advanced submenu
        var advancedMenu = new Forms.ToolStripMenuItem(Localization.Advanced);
        
        var workingSetsItem = new Forms.ToolStripMenuItem(Localization.FlushWorkingSets);
        workingSetsItem.Click += (s, e) => { _memoryCleaner.EmptyWorkingSets(); ShowBalloon(Localization.Flushed("Working Sets")); };
        advancedMenu.DropDownItems.Add(workingSetsItem);

        var systemWorkingSetItem = new Forms.ToolStripMenuItem(Localization.FlushSystemWorkingSet);
        systemWorkingSetItem.Click += (s, e) => { _memoryCleaner.EmptySystemWorkingSet(); ShowBalloon(Localization.Flushed("System Working Set")); };
        advancedMenu.DropDownItems.Add(systemWorkingSetItem);

        var modifiedPageItem = new Forms.ToolStripMenuItem(Localization.FlushModifiedPageList);
        modifiedPageItem.Click += (s, e) => { _memoryCleaner.EmptyModifiedPageList(); ShowBalloon(Localization.Flushed("Modified Page List")); };
        advancedMenu.DropDownItems.Add(modifiedPageItem);

        var standbyItem = new Forms.ToolStripMenuItem(Localization.FlushStandbyList);
        standbyItem.Click += (s, e) => { _memoryCleaner.EmptyStandbyList(); ShowBalloon(Localization.Flushed("Standby List")); };
        advancedMenu.DropDownItems.Add(standbyItem);

        var priority0Item = new Forms.ToolStripMenuItem(Localization.FlushPriority0StandbyList);
        priority0Item.Click += (s, e) => { _memoryCleaner.EmptyPriority0StandbyList(); ShowBalloon(Localization.Flushed("Priority 0 Standby List")); };
        advancedMenu.DropDownItems.Add(priority0Item);

        menu.Items.Add(advancedMenu);

        menu.Items.Add(new Forms.ToolStripSeparator());

        // Settings submenu
        var settingsMenu = new Forms.ToolStripMenuItem(Localization.SettingsMenu);
        
        _cleanOnStartupItem = new Forms.ToolStripMenuItem(Localization.CleanOnStartup)
        {
            CheckOnClick = true,
            Checked = _settings.CleanOnStartup
        };
        _cleanOnStartupItem.Click += (s, e) =>
        {
            _settings.CleanOnStartup = _cleanOnStartupItem.Checked;
            _settings.Save();
            _onSettingsChanged();
        };
        settingsMenu.DropDownItems.Add(_cleanOnStartupItem);

        // Language toggle
        _languageItem = new Forms.ToolStripMenuItem("🌐 " + Localization.Language_Option);
        _languageItem.Click += (s, e) =>
        {
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
            _onSettingsChanged();
        };
        settingsMenu.DropDownItems.Add(_languageItem);

        // Theme toggle
        var themeItem = new Forms.ToolStripMenuItem(Localization.ThemeOption);
        themeItem.Click += (s, e) =>
        {
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
            _onSettingsChanged();
        };
        settingsMenu.DropDownItems.Add(themeItem);

        menu.Items.Add(settingsMenu);

        menu.Items.Add(new Forms.ToolStripSeparator());

        // Exit
        var exitItem = new Forms.ToolStripMenuItem(Localization.Exit);
        exitItem.Click += (s, e) => _exitAction();
        menu.Items.Add(exitItem);

        return menu;
    }

    public void UpdateCleanOnStartupMenu(bool value)
    {
        if (_cleanOnStartupItem != null)
        {
            _cleanOnStartupItem.Checked = value;
        }
    }

    private void CleanAllMemory()
    {
        var memBefore = MemoryInfo.GetMemoryStatus();
        long usedBefore = (long)(memBefore.TotalPhysicalMemory - memBefore.AvailablePhysicalMemory);

        _memoryCleaner.EmptyWorkingSets();
        _memoryCleaner.EmptySystemWorkingSet();
        _memoryCleaner.EmptyModifiedPageList();
        _memoryCleaner.EmptyStandbyList();

        var memAfter = MemoryInfo.GetMemoryStatus();
        long usedAfter = (long)(memAfter.TotalPhysicalMemory - memAfter.AvailablePhysicalMemory);
        long freedBytes = usedBefore - usedAfter;

        if (freedBytes > 0)
        {
            double freedMB = freedBytes / (1024.0 * 1024);
            ShowBalloon(Localization.FreedMemoryBalloon(freedMB));
        }
        else
        {
            ShowBalloon(Localization.MemoryOptimizedBalloon);
        }
    }

    public void ShowBalloon(string message, string title = "FreeMyRam")
    {
        _notifyIcon.ShowBalloonTip(2000, title, message, Forms.ToolTipIcon.Info);
    }

    public void UpdateTooltip(string text)
    {
        // Limit tooltip text to 63 chars (Windows limitation)
        _notifyIcon.Text = text.Length > 63 ? text[..63] : text;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Localization.LanguageChanged -= OnLanguageChanged;
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
