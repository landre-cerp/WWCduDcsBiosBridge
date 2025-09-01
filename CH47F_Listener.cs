using DCS_BIOS.ControlLocator;
using DCS_BIOS.EventArgs;
using DCS_BIOS.Serialized;
using McduDotNet;
using NLog;
using System.Drawing.Imaging;

namespace McduDcsBiosBridge
{
    internal class CH47F_Listener : AircraftListener
    {
        
        private DCSBIOSOutput _CDU_LINE_1;
        private DCSBIOSOutput _CDU_LINE_2;
        private DCSBIOSOutput _CDU_LINE_3;
        private DCSBIOSOutput _CDU_LINE_4;
        private DCSBIOSOutput _CDU_LINE_5;
        private DCSBIOSOutput _CDU_LINE_6;
        private DCSBIOSOutput _CDU_LINE_7;
        private DCSBIOSOutput _CDU_LINE_8;
        private DCSBIOSOutput _CDU_LINE_9;
        private DCSBIOSOutput _CDU_LINE_10;
        private DCSBIOSOutput _CDU_LINE_11;
        private DCSBIOSOutput _CDU_LINE_12;
        private DCSBIOSOutput _CDU_LINE_13;
        private DCSBIOSOutput _CDU_LINE_14;

        private DCSBIOSOutput _CDU_LINE1_COLOR;
        private DCSBIOSOutput _CDU_LINE2_COLOR;
        private DCSBIOSOutput _CDU_LINE3_COLOR;
        private DCSBIOSOutput _CDU_LINE4_COLOR;
        private DCSBIOSOutput _CDU_LINE5_COLOR;
        private DCSBIOSOutput _CDU_LINE6_COLOR;
        private DCSBIOSOutput _CDU_LINE7_COLOR;
        private DCSBIOSOutput _CDU_LINE8_COLOR;
        private DCSBIOSOutput _CDU_LINE9_COLOR;
        private DCSBIOSOutput _CDU_LINE10_COLOR;
        private DCSBIOSOutput _CDU_LINE11_COLOR;
        private DCSBIOSOutput _CDU_LINE12_COLOR;
        private DCSBIOSOutput _CDU_LINE13_COLOR;
        private DCSBIOSOutput _CDU_LINE14_COLOR;

        private Dictionary<uint, int> lineMap;


        private Dictionary<uint, int> colorLines;

        private readonly string[] colorMap = Enumerable.Repeat(new string(' ', 24), 14).ToArray();

        protected override string GetAircraftName() =>"CH-47F";
        
        protected override string GetFontFile() => "resources/ch47f-font-21x31.json";

        const int _AircraftNumber = 50;

        private readonly Dictionary<string, Colour> _Colours = new Dictionary<string, Colour>
        {
                { " " , Colour.Black },
                { "g",  Colour.Green} ,
                { "p",  Colour.Magenta} ,
                { "w",  Colour.White}
        };
        
        public CH47F_Listener(ICdu mcdu, bool bottomAligned) : base(mcdu, _AircraftNumber, bottomAligned) {
        }

        protected override void initBiosControls()
        {
            
            _CDU_LINE_1 = DCSBIOSControlLocator.GetStringDCSBIOSOutput("PLT_CDU_LINE1");
            _CDU_LINE_2 = DCSBIOSControlLocator.GetStringDCSBIOSOutput("PLT_CDU_LINE2");
            _CDU_LINE_3 = DCSBIOSControlLocator.GetStringDCSBIOSOutput("PLT_CDU_LINE3");
            _CDU_LINE_4 = DCSBIOSControlLocator.GetStringDCSBIOSOutput("PLT_CDU_LINE4");
            _CDU_LINE_5 = DCSBIOSControlLocator.GetStringDCSBIOSOutput("PLT_CDU_LINE5");
            _CDU_LINE_6 = DCSBIOSControlLocator.GetStringDCSBIOSOutput("PLT_CDU_LINE6");
            _CDU_LINE_7 = DCSBIOSControlLocator.GetStringDCSBIOSOutput("PLT_CDU_LINE7");
            _CDU_LINE_8 = DCSBIOSControlLocator.GetStringDCSBIOSOutput("PLT_CDU_LINE8");
            _CDU_LINE_9 = DCSBIOSControlLocator.GetStringDCSBIOSOutput("PLT_CDU_LINE9");
            _CDU_LINE_10 = DCSBIOSControlLocator.GetStringDCSBIOSOutput("PLT_CDU_LINE10");
            _CDU_LINE_11 = DCSBIOSControlLocator.GetStringDCSBIOSOutput("PLT_CDU_LINE11");
            _CDU_LINE_12 = DCSBIOSControlLocator.GetStringDCSBIOSOutput("PLT_CDU_LINE12");
            _CDU_LINE_13 = DCSBIOSControlLocator.GetStringDCSBIOSOutput("PLT_CDU_LINE13");
            _CDU_LINE_14 = DCSBIOSControlLocator.GetStringDCSBIOSOutput("PLT_CDU_LINE14");

            _CDU_LINE1_COLOR = DCSBIOSControlLocator.GetStringDCSBIOSOutput("PLT_CDU_LINE1_COLOR");
            _CDU_LINE2_COLOR = DCSBIOSControlLocator.GetStringDCSBIOSOutput("PLT_CDU_LINE2_COLOR");
            _CDU_LINE3_COLOR = DCSBIOSControlLocator.GetStringDCSBIOSOutput("PLT_CDU_LINE3_COLOR");
            _CDU_LINE4_COLOR = DCSBIOSControlLocator.GetStringDCSBIOSOutput("PLT_CDU_LINE4_COLOR");
            _CDU_LINE5_COLOR = DCSBIOSControlLocator.GetStringDCSBIOSOutput("PLT_CDU_LINE5_COLOR");
            _CDU_LINE6_COLOR = DCSBIOSControlLocator.GetStringDCSBIOSOutput("PLT_CDU_LINE6_COLOR");
            _CDU_LINE7_COLOR = DCSBIOSControlLocator.GetStringDCSBIOSOutput("PLT_CDU_LINE7_COLOR");
            _CDU_LINE8_COLOR = DCSBIOSControlLocator.GetStringDCSBIOSOutput("PLT_CDU_LINE8_COLOR");
            _CDU_LINE9_COLOR = DCSBIOSControlLocator.GetStringDCSBIOSOutput("PLT_CDU_LINE9_COLOR");
            _CDU_LINE10_COLOR = DCSBIOSControlLocator.GetStringDCSBIOSOutput("PLT_CDU_LINE10_COLOR");
            _CDU_LINE11_COLOR = DCSBIOSControlLocator.GetStringDCSBIOSOutput("PLT_CDU_LINE11_COLOR");
            _CDU_LINE12_COLOR = DCSBIOSControlLocator.GetStringDCSBIOSOutput("PLT_CDU_LINE12_COLOR");
            _CDU_LINE13_COLOR = DCSBIOSControlLocator.GetStringDCSBIOSOutput("PLT_CDU_LINE13_COLOR");
            _CDU_LINE14_COLOR = DCSBIOSControlLocator.GetStringDCSBIOSOutput("PLT_CDU_LINE14_COLOR");

            lineMap = new Dictionary<uint, int>
            {
                { _CDU_LINE_1.Address, 1 },
                { _CDU_LINE_2.Address, 2},
                { _CDU_LINE_3.Address, 3 },
                { _CDU_LINE_4.Address, 4 },
                { _CDU_LINE_5.Address, 5 },
                { _CDU_LINE_6.Address, 6 },
                { _CDU_LINE_7.Address, 7 },
                { _CDU_LINE_8.Address, 8 },
                { _CDU_LINE_9.Address, 9 },
                { _CDU_LINE_10.Address, 10 },
                { _CDU_LINE_11.Address, 11 },
                { _CDU_LINE_12.Address, 12 },
                { _CDU_LINE_13.Address, 13 },
                { _CDU_LINE_14.Address, 14 },
            };

            colorLines = new Dictionary<uint, int>
            {
                { _CDU_LINE1_COLOR.Address, 1 },
                { _CDU_LINE2_COLOR.Address, 2},
                { _CDU_LINE3_COLOR.Address, 3 },
                { _CDU_LINE4_COLOR.Address, 4 },
                { _CDU_LINE5_COLOR.Address, 5 },
                { _CDU_LINE6_COLOR.Address, 6 },
                { _CDU_LINE7_COLOR.Address, 7 },
                { _CDU_LINE8_COLOR.Address, 8 },
                { _CDU_LINE9_COLOR.Address, 9 },
                { _CDU_LINE10_COLOR.Address, 10 },
                { _CDU_LINE11_COLOR.Address, 11 },
                { _CDU_LINE12_COLOR.Address, 12 },
                { _CDU_LINE13_COLOR.Address, 13 },
                { _CDU_LINE14_COLOR.Address, 14 },
            };


        }

        public override void DCSBIOSStringReceived(object sender, DCSBIOSStringDataEventArgs e)
        {
            try
            {

                string data = e.StringData
                    .Replace("»", "→")
                    .Replace("«", "←")
                    .Replace("¡", "☐")
                    .Replace("}", "↓")
                    .Replace("®", "Δ");

                    //.Replace("©", "^")
                    //.Replace("?", "%");

                mcdu.Output.White();

                if (colorLines.TryGetValue(e.Address, out int colorLine))
                {
                    colorMap[colorLine - 1] = data;
                }


                if (lineMap.TryGetValue(e.Address, out int lineIndex))
                {
                    
                    // update line with this fast method 
                    var screen = mcdu.Screen;
                    var row = screen.Rows[lineIndex-1];
                    var color = colorMap[lineIndex - 1];
                    for (var cellIdx = 0; cellIdx < row.Cells.Length; ++cellIdx)
                    {
                        var cell = row.Cells[cellIdx];
                        cell.Character = data[cellIdx];
                        _Colours.TryGetValue(color[cellIdx].ToString(), out Colour value);
                        cell.Colour = value; 
                        cell.Small = lineIndex%2 == 0  && lineIndex!= 14;
                    }

                }
                
                
            }
            catch (Exception ex)
            {
                // Optionnel : log l'erreur
                Console.WriteLine(ex.ToString());
            }
        }




    }
}