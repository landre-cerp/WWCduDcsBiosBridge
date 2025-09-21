﻿using ClassLibraryCommon;
using DCS_BIOS.ControlLocator;
using McduDotNet;
using Newtonsoft.Json;

namespace WWCduDcsBiosBridge
{
    internal class DeviceContext
    {
        public ICdu Mcdu { get; }
        public int SelectedAircraft { get; private set; } = -1;
        public bool Pilot { get; private set; } = true;
        private readonly DcsBiosConfig? config;
        private readonly UserOptions options;

        public DeviceContext(ICdu mcdu,
            UserOptions options,
            DcsBiosConfig? config)
        {
            Mcdu = mcdu;
            this.options = options;
            this.config = config;
        }

        public void ShowStartupScreen()
        {
            Mcdu.UseFont(JsonConvert.DeserializeObject<McduFontFile>(File.ReadAllText("resources/a10c-font-21x31.json")), true);
            Mcdu.Output.Clear().Green()
                .Line(0).Centered("DCSbios/WWCDU Bridge")
                .NewLine().Large().Yellow().Centered("by Cerppo")
                .White()
                .LeftLabel(2, "A10C").RightLabel(2, "AH64D")
                .LeftLabel(3, "FA18C").RightLabel(3, "CH-47 (PLT)")
                .LeftLabel(4, "F15E").RightLabel(4, "CH-47 (CPLT)")
                .BottomLine().WriteLine("Close app to exit");
            Mcdu.RefreshDisplay();
            Mcdu.KeyDown += ReadMenu;
        }

        private void ReadMenu(object? sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.LineSelectLeft2: SelectedAircraft = 5; break;   // A10
                case Key.LineSelectRight2: SelectedAircraft = 46; break; // AH64D
                case Key.LineSelectLeft3: SelectedAircraft = 20; break;  // FA18C
                case Key.LineSelectRight3: SelectedAircraft = 50; Pilot = true; break; // CH-47 PLT
                case Key.LineSelectLeft4: SelectedAircraft = 44; break;  // F15E
                case Key.LineSelectRight4: SelectedAircraft = 50; Pilot = false; break; // CH-47 CPLT
            }
            Mcdu.KeyDown -= ReadMenu;

        }

        public void StartBridge()
        {

            DCSAircraft.Init();
            DCSAircraft.FillModulesListFromDcsBios(config!.DcsBiosJsonLocation, true);
            DCSBIOSControlLocator.JSONDirectory = config.DcsBiosJsonLocation;

            var aircraftMap = new Dictionary<int, Func<IDcsBiosListener>>
            {
                [5] = () => new A10C_Listener(Mcdu, options),
                [46] = () => new AH64D_Listener(Mcdu, options),
                [20] = () => new FA18C_Listener(Mcdu, options),
                [44] = () => new F15E_Listener(Mcdu, options),
                [50] = () => new CH47F_Listener(Mcdu, options, Pilot),
            };

            if (aircraftMap.TryGetValue(SelectedAircraft, out var factory))
                factory().Start();
            else
            {
                Mcdu.Output.Newline().Red().WriteLine("Unknown Aircraft");
                Mcdu.RefreshDisplay();
            }
        }
    }
}