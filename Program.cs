using ClassLibraryCommon;
using DCS_BIOS;
using DCS_BIOS.ControlLocator;
using HidSharp.Utility;
using McduDcsBiosBridge;
using McduDotNet;
using Newtonsoft.Json;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.DirectoryServices;
using System.Drawing.Text;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Controls;
using NLog;

namespace McduDcsBiosBridge
{
    internal class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static DcsBiosConfig? config;
        private static ICdu Mcdu = CduFactory.ConnectLocal();
        private static DCSBIOS? dCSBIOS;

        private static int selected_aircraft = -1;

        private static bool displayBottomAligned = false;
        private static bool displayCMS = false;

        static async Task<int> Main(string[] args)
        {
            int exitCode = 0;
            bool worked = false;

            try
            {
                LogManager.ThrowConfigExceptions = true;
                config = ConfigManager.Load();

                Logger.Info("Connecting to MCDU");
                while (Mcdu == null)
                {
                    await Task.Delay(200); // Use await to avoid CS1998
                    Console.Write(".");
                    Mcdu = CduFactory.ConnectLocal();
                }

                Logger.Info("Starting Dcsbios - Mcdu bridge");
                Logger.Info(Mcdu.DeviceId);

                RootCommand rootCommand = new("Winwing MCDU DCSBios brigde ") {
                        Options.DisplayBottomAligned,
                        Options.AircraftNumber,
                        Options.DisplayCMS
                };

                rootCommand.TreatUnmatchedTokensAsErrors = true;

                ParseResult parsed = rootCommand.Parse(args);
                displayBottomAligned = parsed.GetValue(Options.DisplayBottomAligned);
                displayCMS = parsed.GetValue(Options.DisplayCMS);

                Mcdu.UseFont(JsonConvert.DeserializeObject<McduFontFile>(File.ReadAllText("resources/a10c-font-21x31.json")), true);

                Mcdu.Output
                    .Clear().Green()
                    .Line(0).Centered("DCSbios-MCDU Bridge")
                    .NewLine().Yellow().Small().Centered("(WIP)").Large()
                    .NewLine().Centered("by Cerppo")
                    .White().LeftLabel(3, "A10C")
                    .RightLabel(3, "AH64D")
                    .BottomLine().WriteLine("Menu key to exit")
                    .White().LeftLabel(4, "FA18C");

                Mcdu.RefreshDisplay();

                Mcdu.KeyDown += ReadMenu;

                while (selected_aircraft == -1)
                {
                    Mcdu.RefreshDisplay();
                    await Task.Delay(100); // Use await to avoid CS1998
                }

                Mcdu.KeyDown -= ReadMenu;

                initDCSBios();
                ListenToBios(displayBottomAligned, selected_aircraft, displayCMS);

                exitCode = rootCommand.Parse(args).Invoke();

                if (!worked)
                {
                    exitCode = 1;
                }
            }
            catch (ConfigException cex)
            {
                Logger.Error(cex.Message);
                exitCode = 3;
            }
            catch (Exception ex)
            {
                Logger.Error("Fatal error: " + ex.Message);
                Mcdu.Output.Clear();
                Mcdu.RefreshDisplay();
                Mcdu.Cleanup();

                exitCode = 2;
            }

            LogManager.Shutdown(); // Add before return exitCode;

            return exitCode;
        }

        private static void ReadMenu(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.LineSelectLeft3)
            {
                Logger.Info("Starting A10");
                selected_aircraft = 5;

            }

            if (e.Key == Key.LineSelectRight3)
            {
                Logger.Info("Starting AH64D");
                selected_aircraft = 46;
            }

            if (e.Key == Key.LineSelectLeft4)
            {
                Logger.Info("Starting FA18C");
                selected_aircraft = 20;
            }

            if (e.Key == Key.McduMenu || e.Key == Key.Menu)
            {
                Logger.Info("Exiting...");
                Mcdu.Cleanup();
                Environment.Exit(0);
            }
            
        }

        private static void initDCSBios()
        {
            dCSBIOS = new DCSBIOS(config.ReceiveFromIpUdp,config.SendToIpUdp, config.ReceivePortUdp, config.SendPortUdp, DcsBiosNotificationMode.Parse);
            if (!dCSBIOS.HasLastException())
            {
                if (!dCSBIOS.IsRunning) {
                    dCSBIOS.Startup();
                }

                Logger.Info("DCS-BIOS started successfully.");

            }
            else
            {
                Logger.Error(dCSBIOS.GetLastException().Message);
            }
            
        }
        private static void ListenToBios(bool displayBottomAligned, int aircraftNumber, bool displayCMS)
        {
            try
            {
                DCSAircraft.Init();
                DCSAircraft.FillModulesListFromDcsBios(config.dcsBiosJsonLocation, true);
                DCSBIOSControlLocator.JSONDirectory = config.dcsBiosJsonLocation;

                var aircraftMap = new Dictionary<int, Func<IDcsBiosListener>>
                {
                    [5] =  () => new A10C_Listener(Mcdu, displayBottomAligned, displayCMS),
                    [46] = () => new AH64D_Listener(Mcdu, displayBottomAligned),
                    [20] = () => new FA18C_Listener(Mcdu, displayBottomAligned),
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
                Logger.Error("Current dcsBiosJsonLocation: " + config.dcsBiosJsonLocation);
                Mcdu.Output.Clear().Red().WriteLine("Error DCS-BIOS")
                    .NewLine().WriteLine("Check log.txt");
                Mcdu.RefreshDisplay();
            }
        }

    }
}