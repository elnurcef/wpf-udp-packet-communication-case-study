using System.Windows;

namespace Baykar.UserInterface;

public static class Program
{
    [STAThread]
    public static void Main()
    {
        App app = new()
        {
            ShutdownMode = ShutdownMode.OnMainWindowClose
        };

        MainWindow mainWindow = new()
        {
            ShowInTaskbar = true,
            WindowState = WindowState.Normal,
            Topmost = true
        };

        mainWindow.Loaded += (_, _) =>
        {
            mainWindow.Activate();
            mainWindow.Topmost = false;
        };

        app.Run(mainWindow);
    }
}
