namespace FreeMyRam;

public partial class App : System.Windows.Application
{
    private SingleInstanceManager? _singleInstanceManager;
    private MainWindow? _mainWindow;
    
    // Cached settings to avoid loading twice
    public static AppSettings? CachedSettings { get; private set; }
    
    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // Check for single instance
        _singleInstanceManager = new SingleInstanceManager();
        if (!_singleInstanceManager.TryAcquireLock())
        {
            // Another instance is already running - exit this one
            Shutdown();
            return;
        }
        
        // Subscribe to event when another instance tries to start
        _singleInstanceManager.SecondInstanceStarted += OnSecondInstanceStarted;
        
        // Load settings once and cache for MainWindow to use
        CachedSettings = AppSettings.Load();
        var settings = CachedSettings;
        ThemeManager.CurrentTheme = settings.Theme == "Light" 
            ? ThemeManager.Theme.Light 
            : ThemeManager.Theme.Dark;
        
        // Force apply theme at startup (in case default matches saved setting)
        ThemeManager.ApplyTheme();
    }
    
    protected override void OnActivated(System.EventArgs e)
    {
        base.OnActivated(e);
        
        // Store reference to MainWindow for later use
        if (_mainWindow == null && MainWindow is MainWindow mw)
        {
            _mainWindow = mw;
        }
    }
    
    private void OnSecondInstanceStarted()
    {
        // Show the main window when another instance tries to start
        if (_mainWindow != null)
        {
            _mainWindow.Show();
            _mainWindow.WindowState = System.Windows.WindowState.Normal;
            _mainWindow.Activate();
            
            // Bring window to front
            _mainWindow.Topmost = true;
            _mainWindow.Topmost = false;
            _mainWindow.Focus();
        }
        else if (MainWindow != null)
        {
            MainWindow.Show();
            MainWindow.WindowState = System.Windows.WindowState.Normal;
            MainWindow.Activate();
            
            // Bring window to front
            MainWindow.Topmost = true;
            MainWindow.Topmost = false;
            MainWindow.Focus();
        }
    }
    
    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
        // Clean up the single instance manager
        _singleInstanceManager?.Dispose();
        _singleInstanceManager = null;
        
        base.OnExit(e);
    }
}
