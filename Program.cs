using ClassLibraryCommon;
using DCS_BIOS;
using DCS_BIOS.ControlLocator;
using HidSharp.Utility;
using McduDotNet;
using Newtonsoft.Json;
using System.DirectoryServices;
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


        static void Main(string[] args)
        {

            
            ConfigManager.Save(config);

            
            var json = File.ReadAllText("resources/a10c-font-21x31.json");
            var result = JsonConvert.DeserializeObject<McduFontFile>(json);

            Mcdu.UseFont( result , true);


            Mcdu.Output
                .Clear().Green()
                .MiddleLine().Centered("A10C from DCSBios")
                .NewLine().Small().Centered("(WIP)").Large()
                .NewLine().White().Centered("by Cerppo");
            
            
            Mcdu.RefreshDisplay();

            initDCSBios();
            ListenToBios();
            Console.WriteLine("A10C Dcsbios - Mcdu started");
            Console.WriteLine($"Using config : {System.Text.Json.JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true })} ");

        }

        private static void initDCSBios()
        {
            dCSBIOS = new DCSBIOS(config.ReceiveFromIpUdp,config.SendToIpUdp, config.ReceivePortUdp, config.SendPortUdp, DcsBiosNotificationMode.Parse);
            if (!dCSBIOS.HasLastException())
            {
                if (!dCSBIOS.IsRunning) {
                    dCSBIOS.Startup();
                }

            }
            
        }

        private static void ListenToBios()
        {
            // Infinite Loop to Listen

            DCSAircraft.Init();
            
            DCSAircraft.FillModulesListFromDcsBios(config.dcsBiosJsonLocation,true);

            DCSBIOSControlLocator.DCSAircraft = DCSAircraft.GetAircraft(5);
            DCSBIOSControlLocator.JSONDirectory = config.dcsBiosJsonLocation;

            var listener = new A10cListener(Mcdu);
            

        }

    }
}
 