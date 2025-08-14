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

namespace McduDcsBiosBridge
{
    internal class Program
    {
        private static DcsBiosConfig config = ConfigManager.Load();
        private static ICdu Mcdu = CduFactory.ConnectLocal();
        private static DCSBIOS dCSBIOS;

        private static int selected_aircraft = -1;

        private static bool displayBottomAligned = false;
        private static bool displayCMS = false;

        static async Task<int> Main(string[] args)
        {

            int exitCode = 0;
            bool worked = false;

            try {
                Console.WriteLine("Connecting to MCDU...");
                while (Mcdu == null)
                {
                    Thread.Sleep(100);
                    Console.Write(".");
                    Mcdu = CduFactory.ConnectLocal();

                }

                Console.WriteLine("Starting Dcsbios - Mcdu bridge");
                Console.WriteLine(Mcdu.DeviceId);

                RootCommand rootCommand = new("Winwing MCDU DCSBios brigde ") {
                        Options.DisplayBottomAligned,
                        Options.AircraftNumber,
                        Options.DisplayCMS
                };

                rootCommand.TreatUnmatchedTokensAsErrors = true;

                    
                ParseResult parsed=rootCommand.Parse(args);
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
                    Thread.Sleep(100);
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
            catch(Exception ex) {
                    Console.WriteLine("Caught exception during processing:");
                    Console.WriteLine(ex);
                    Mcdu.Output.Clear();
                    Mcdu.RefreshDisplay();
                    Mcdu.Cleanup();
                
                exitCode = 2;
                }

            return exitCode;

        }

        private static void ReadMenu(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.LineSelectLeft3)
            {
                Console.WriteLine("Starting A10");
                selected_aircraft = 5;

            }

            if (e.Key == Key.LineSelectRight3)
            {
                Console.WriteLine("Starting AH64D");
                selected_aircraft = 46;
            }

            if (e.Key == Key.LineSelectLeft4)
            {
                Console.WriteLine("Starting FA18C");
                selected_aircraft = 20;
            }

            if (e.Key == Key.McduMenu || e.Key == Key.Menu)
            {
                Console.WriteLine("Exiting...");
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

                Console.WriteLine("DCS-BIOS started successfully.");

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
                Console.WriteLine("Error while listening to DCS-BIOS: " + ex.Message);
                Mcdu.Output.Clear().Red().WriteLine("Error DCS-BIOS")
                    .NewLine().WriteLine("Check console");
                Mcdu.RefreshDisplay();
            }
        }

    }
}
 