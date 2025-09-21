using NLog;
using System.Windows;

namespace WWCduDcsBiosBridge;

public partial class App : Application
{
    public static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    protected override void OnStartup(StartupEventArgs e)
    {
        // Handle any unhandled exceptions
        this.DispatcherUnhandledException += (sender, args) =>
        {
            MessageBox.Show($"An unexpected error occurred: {args.Exception.Message}", 
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        base.OnStartup(e);
        Logger.Info("Application started.");
        // Other startup logic...
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Logger.Info("Application exited.");
        base.OnExit(e);
    }
}