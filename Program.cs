using ClassLibraryCommon;
using DCS_BIOS;
using DCS_BIOS.ControlLocator;
using McduDotNet;
using Newtonsoft.Json;
using System.CommandLine;
using System.CommandLine.Parsing;
using NLog;

namespace McduDcsBiosBridge
{
    internal class Program
    {
        // Configuration constants
        private const string FontResourcePath = "resources/a10c-font-21x31.json";
        private const int McduConnectionDelayMs = 200;
        private const int RefreshDelayMs = 100;
        
        // Aircraft numbers
        private const int A10CAircraftNumber = 5;
        private const int AH64DAircraftNumber = 46;
        private const int FA18CAircraftNumber = 20;
        private const int CH47FAircraftNumber = 50;

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static DcsBiosConfig? config;
        private static ICdu Mcdu = CduFactory.ConnectLocal();
        private static DCSBIOS? dcsBios;

        private static int selectedAircraft = -1;

        private static bool displayBottomAligned = false;
        private static bool displayCMS = false;

        private static bool pilot = true;

        /// <summary>
        /// Main entry point for the MCDU DCS-BIOS Bridge application.
        /// </summary>
        /// <param name="args">Command line arguments</param>
        /// <returns>Exit code (0 for success, non-zero for errors)</returns>
        static async Task<int> Main(string[] args)
        {
            int exitCode = 0;

            try
            {
                SetupLogging();
                LoadConfig();
                await ConnectToMcduAsync();
                ParseOptions(args);
                ShowStartupScreen();
                await GetAircraftSelectionAsync();
                InitDcsBios();
                StartBridge();
            }
            catch (ConfigException cex)
            {
                Logger.Error(cex.Message);
                exitCode = 3;
            }
            catch (Exception ex)
            {
                Logger.Error("Fatal error: " + ex.Message);
                CleanupAndExit();
                exitCode = 2;
            }

            LogManager.Shutdown();
            return exitCode;
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
        }

        /// <summary>
        /// Establishes connection to the MCDU device.
        /// </summary>
        private static async Task ConnectToMcduAsync()
        {
            Logger.Info("Connecting to MCDU");
            while (Mcdu == null)
            {
                await Task.Delay(McduConnectionDelayMs);
                Console.Write(".");
                Mcdu = CduFactory.ConnectLocal();
            }

            Logger.Info("Starting DcsBios - Mcdu bridge");
            Logger.Info(Mcdu.DeviceId);
        }

        /// <summary>
        /// Parses command line options and sets global flags.
        /// </summary>
        /// <param name="args">Command line arguments</param>
        private static void ParseOptions(string[] args)
        {
            RootCommand rootCommand = new("Winwing MCDU DCSBios bridge ") {
                    Options.DisplayBottomAligned,
                    Options.AircraftNumber,
                    Options.DisplayCMS
            };

            rootCommand.TreatUnmatchedTokensAsErrors = true;

            ParseResult parsed = rootCommand.Parse(args);
            displayBottomAligned = parsed.GetValue(Options.DisplayBottomAligned);
            displayCMS = parsed.GetValue(Options.DisplayCMS);
        }

        /// <summary>
        /// Displays the startup screen with aircraft selection menu.
        /// </summary>
        private static void ShowStartupScreen()
        {
            Mcdu.UseFont(JsonConvert.DeserializeObject<McduFontFile>(File.ReadAllText(FontResourcePath)), true);

            Mcdu.Output
                .Clear().Green()
                .Line(0).Centered("DCSbios-MCDU Bridge")
                .NewLine().Yellow().Small().Centered("(WIP)").Large()
                .NewLine().Centered("by Cerppo")
                .White().LeftLabel(3, "A10C")
                .RightLabel(3, "AH64D")
                .White().LeftLabel(4, "FA18C")
                .White().RightLabel(4, "CH-47 (PLT)")
                .White().RightLabel(5, "CH-47 (CPLT)")
                .BottomLine().WriteLine("Menu key to exit");

            Mcdu.RefreshDisplay();
            Mcdu.KeyDown += ReadMenu;
        }

        /// <summary>
        /// Waits for user to select an aircraft from the menu.
        /// </summary>
        private static async Task GetAircraftSelectionAsync()
        {
            while (selectedAircraft == -1)
            {
                Mcdu.RefreshDisplay();
                await Task.Delay(RefreshDelayMs);
            }

            Mcdu.KeyDown -= ReadMenu;
        }

        /// <summary>
        /// Initializes the DCS-BIOS connection.
        /// </summary>
        private static void InitDcsBios()
        {
            if (config == null)
                throw new InvalidOperationException("Configuration not loaded");

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

        /// <summary>
        /// Starts the bridge and begins listening to DCS-BIOS data.
        /// </summary>
        private static void StartBridge()
        {
            ListenToBios(displayBottomAligned, selectedAircraft, displayCMS);
        }

        /// <summary>
        /// Cleans up resources and prepares for exit.
        /// </summary>
        private static void CleanupAndExit()
        {
            Mcdu.Output.Clear();
            Mcdu.RefreshDisplay();
            Mcdu.Cleanup();
        }

        private static void ReadMenu(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.LineSelectLeft3)
            {
                Logger.Info("Starting A10");
                selectedAircraft = A10CAircraftNumber;
            }

            if (e.Key == Key.LineSelectRight3)
            {
                Logger.Info("Starting AH64D");
                selectedAircraft = AH64DAircraftNumber;
            }

            if (e.Key == Key.LineSelectLeft4)
            {
                Logger.Info("Starting FA18C");
                selectedAircraft = FA18CAircraftNumber;
            }

            if (e.Key == Key.LineSelectRight4)
            {
                Logger.Info("Starting CH-47");
                selectedAircraft = CH47FAircraftNumber;
                pilot = true;
            }

            if (e.Key == Key.LineSelectRight5)
            {
                Logger.Info("Starting CH-47");
                selectedAircraft = CH47FAircraftNumber;
                pilot = false;
            }

            if (e.Key == Key.McduMenu || e.Key == Key.Menu)
            {
                Logger.Info("Exiting...");
                Mcdu.Cleanup();
                Environment.Exit(0);
            }
        }

        private static void ListenToBios(bool displayBottomAligned, int aircraftNumber, bool displayCMS)
        {
            try
            {
                if (config == null)
                    throw new InvalidOperationException("Configuration not loaded");

                DCSAircraft.Init();
                DCSAircraft.FillModulesListFromDcsBios(config.DcsBiosJsonLocation, true);
                DCSBIOSControlLocator.JSONDirectory = config.DcsBiosJsonLocation;

                var aircraftMap = new Dictionary<int, Func<IDcsBiosListener>>
                {
                    [A10CAircraftNumber] =  () => new A10C_Listener(Mcdu, displayBottomAligned, displayCMS),
                    [AH64DAircraftNumber] = () => new AH64D_Listener(Mcdu, displayBottomAligned),
                    [FA18CAircraftNumber] = () => new FA18C_Listener(Mcdu, displayBottomAligned),
                    [CH47FAircraftNumber] = () => new CH47F_Listener(Mcdu, false, pilot )
                };

                if (!aircraftMap.TryGetValue(aircraftNumber, out var listenerFactory))
                {
                    Mcdu.Output.Newline().Red().WriteLine("Unknown Aircraft Number");
                    Mcdu.RefreshDisplay();
                    return;
                }

                var listener = listenerFactory();
                listener.Start();
            }
            catch (Exception ex)
            {
                Logger.Error("Error while listening to DCS-BIOS: " + ex.Message);
                Logger.Error("Current DcsBiosJsonLocation: " + config?.DcsBiosJsonLocation);
                Mcdu.Output.Clear().Red().WriteLine("Error DCS-BIOS")
                    .NewLine().WriteLine("Check log.txt");
                Mcdu.RefreshDisplay();
            }
        }

    }
}