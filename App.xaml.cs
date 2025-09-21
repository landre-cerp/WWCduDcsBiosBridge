using System.Windows;

namespace WWCduDcsBiosBridge
{
    public partial class App : Application
    {
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
        }
    }
}