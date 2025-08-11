using ClassLibraryCommon;
using DCS_BIOS;
using DCS_BIOS.ControlLocator;
using HidSharp.Utility;
using McduDcsBiosBridge;
using McduDotNet;
using Newtonsoft.Json;
using System.CommandLine;
using System.DirectoryServices;
using System.Drawing.Text;
using System.Net;
using System.Text.Json;
using System.Windows.Controls;

namespace McduDcsBiosBridge
{
    internal class Program
    {
        private static DcsBiosConfig config = ConfigManager.Load();
        private static IMcdu Mcdu = McduFactory.ConnectLocal();
        private static DCSBIOS dCSBIOS;


        static int Main(string[] args)
        {

            int exitCode = 0;
            bool worked = false;

            try {

                Console.WriteLine("Starting Dcsbios - Mcdu bridge");


                RootCommand rootCommand = new("Winwing MCDU DCSBios brigde ") {
                        Options.DisplayBottomAligned,
                        Options.AircraftNumber,
                        Options.DisplayCMS
                };

                rootCommand.TreatUnmatchedTokensAsErrors = true;

                rootCommand.SetAction(
                    parseResult => {

                        var displayBottomAligned = parseResult.GetValue(Options.DisplayBottomAligned);
                        var aircraft = parseResult.GetValue(Options.AircraftNumber);
                        var alignementString = displayBottomAligned ? "bottom" : "top";
                        var displayCMS = parseResult.GetValue(Options.DisplayCMS);


                        Mcdu.Output
                            .Clear().Green()
                            .Line(2).Centered("DCSbios-MCDU Bridge")
                            .NewLine().Yellow().Small().Centered("(WIP)").Large()
                            .NewLine().Centered("by Cerppo");
                            

                        Mcdu.RefreshDisplay();

                        initDCSBios();
                        ListenToBios(displayBottomAligned, aircraft, displayCMS);


                    }
                );
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
            // Infinite Loop to Listen

            try
            {
                IDcsBiosListener listener;
                DCSAircraft.Init();
                DCSAircraft.FillModulesListFromDcsBios(config.dcsBiosJsonLocation, true);
                DCSBIOSControlLocator.JSONDirectory = config.dcsBiosJsonLocation;

                string json;
                McduFontFile? result;

                switch (aircraftNumber)
                {
                    case 5:
                        json = File.ReadAllText("resources/a10c-font-21x31.json");
                        result = JsonConvert.DeserializeObject<McduFontFile>(json);

                        Mcdu.UseFont(result, true);


                        Mcdu.Output.Clear().Green().WriteLine("Starting A10C");
                        DCSBIOSControlLocator.DCSAircraft = DCSAircraft.GetAircraft(aircraftNumber);
                        listener = new A10C_Listener(Mcdu, displayBottomAligned,displayCMS);
                        Mcdu.RefreshDisplay();
                        break;
                    case 46:
                        json = File.ReadAllText("resources/ah64d-font-21x31.json");
                        result = JsonConvert.DeserializeObject<McduFontFile>(json);

                        Mcdu.UseFont(result, true);


                        Mcdu.Output.Clear().Green().WriteLine("Starting AH64D");
                        DCSBIOSControlLocator.DCSAircraft = DCSAircraft.GetAircraft(aircraftNumber);
                        listener = new AH64D_Listener(Mcdu, displayBottomAligned);
                        Mcdu.RefreshDisplay();
                        break;

                    default:
                        Mcdu.Output.Newline().Red().WriteLine("Unknown Aircraft Number");
                        Mcdu.RefreshDisplay();
                        return;
                }

                Console.WriteLine($"Using Aircraft {DCSBIOSControlLocator.DCSAircraft.ModuleLuaName} with number {aircraftNumber}");

                listener.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while listening to DCS-BIOS: " + ex.Message);
                Mcdu.Output.Clear().Red().WriteLine("Error DCS-BIOS");
                Mcdu.Output.NewLine().WriteLine("Check console");
                Mcdu.RefreshDisplay();
            }


        }

    }
}
 