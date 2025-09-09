using ClassLibraryCommon;
using DCS_BIOS;
using DCS_BIOS.ControlLocator;
using McduDotNet;
using Newtonsoft.Json;
using NLog;
using System.CommandLine;
using System.Drawing;
using System.Linq;

namespace WWCduDcsBiosBridge
{
    internal class Program
    {

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static DcsBiosConfig? config;

        private static DCSBIOS? dcsBios;

        private static bool displayBottomAligned = false;
        private static bool displayCMS = false;

        /// <summary>
        /// Main entry point for the MCDU DCS-BIOS Bridge application.
        /// </summary>
        /// <param name="args">Command line arguments</param>
        /// <returns>Exit code (0 for success, non-zero for errors)</returns>
        static async Task<int> Main(string[] args)
        {
            SetupLogging();
            LoadConfig();

            var devices = CduFactory.FindLocalDevices().ToList();
            var contexts = devices.Select(dev => new DeviceContext(
                CduFactory.ConnectLocal(dev), displayBottomAligned, displayCMS, config)).ToList();

            foreach (var ctx in contexts)
                ctx.ShowStartupScreen();

            while (contexts.Any(c => c.SelectedAircraft == -1))
                await Task.Delay(100);
            InitDcsBios();

            foreach (var ctx in contexts)
                ctx.StartBridge();

            await Task.Delay(-1);
            return 0;
        }

        /// <summary>
        /// Sets up logging configuration.
        /// </summary>
        private static void SetupLogging()
        {
            LogManager.ThrowConfigExceptions = true;
        }

        /// <summary>
        /// Loads configuration from the config file.
        /// </summary>
        private static void LoadConfig()
        {
            config = ConfigManager.Load();
            if (config == null)
                throw new InvalidOperationException("Configuration not loaded");
        }


        /// <summary>
        /// Initializes the DCS-BIOS connection.
        /// </summary>
        private static void InitDcsBios()
        {

            dcsBios = new DCSBIOS(config.ReceiveFromIpUdp, config.SendToIpUdp, 
                                 config.ReceivePortUdp, config.SendPortUdp, 
                                 DcsBiosNotificationMode.Parse);
            
            if (!dcsBios.HasLastException())
            {
                if (!dcsBios.IsRunning)
                {
                    dcsBios.Startup();
                }
                Logger.Info("DCS-BIOS started successfully.");
            }
            else
            {
                Logger.Error(dcsBios.GetLastException().Message);
            }
        }


    }
}