namespace FreeMyRam;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // Load settings and apply theme before MainWindow is created
        var settings = AppSettings.Load();
        ThemeManager.CurrentTheme = settings.Theme == "Light" 
            ? ThemeManager.Theme.Light 
            : ThemeManager.Theme.Dark;
        
        // Force apply theme at startup (in case default matches saved setting)
        ThemeManager.ApplyTheme();
    }
}
