using DCS_BIOS;
using McduDotNet;
using NLog;
using System;
using System.Windows;

namespace WWCduDcsBiosBridge
{
    public class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Main entry point for the MCDU DCS-BIOS Bridge application.
        /// </summary>
        /// <param name="args">Command line arguments</param>
        [STAThread]
        public static int Main()
        {
            try
            {
                var app = new Application();
                var mainWindow = new MainWindow();
                app.Run(mainWindow);
                // Return 0 for success, or another code if you want to indicate an error
                return 0;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to start WPF application");
                MessageBox.Show($"Failed to start application: {ex.Message}", "Error", 
                                MessageBoxButton.OK, MessageBoxImage.Error);
                return 1;
            }
        }
    }
}