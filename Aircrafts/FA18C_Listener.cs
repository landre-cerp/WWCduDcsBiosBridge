using DCS_BIOS.ControlLocator;
using DCS_BIOS.EventArgs;
using DCS_BIOS.Serialized;
using McduDotNet;
using System;

namespace WWCduDcsBiosBridge.Aircrafts;

internal class FA18C_Listener : AircraftListener
{
    private DCSBIOSOutput? UFC_OPTION_DISPLAY_1;
    private DCSBIOSOutput? UFC_OPTION_CUEING_1;

    private DCSBIOSOutput? UFC_OPTION_DISPLAY_2;
    private DCSBIOSOutput? UFC_OPTION_CUEING_2;

    private DCSBIOSOutput? UFC_OPTION_DISPLAY_3;
    private DCSBIOSOutput? UFC_OPTION_CUEING_3;

    private DCSBIOSOutput? UFC_OPTION_DISPLAY_4;
    private DCSBIOSOutput? UFC_OPTION_CUEING_4;

    private DCSBIOSOutput? UFC_OPTION_DISPLAY_5;
    private DCSBIOSOutput? UFC_OPTION_CUEING_5;

    private DCSBIOSOutput? UFC_SCRATCHPAD_NUMBER_DISPLAY;
    private DCSBIOSOutput? UFC_SCRATCHPAD_STRING_1_DISPLAY;
    private DCSBIOSOutput? UFC_SCRATCHPAD_STRING_2_DISPLAY;

    private DCSBIOSOutput? MASTER_CAUTION_LT;

    string _cue1 = " ", _cue2 = " ", _cue3 = " ", _cue4 = " ", _cue5 = " ";
    string _option1 = "    ", _option2 = "    ", _option3 = "    ", _option4 = "    ", _option5 = "    ";

    string _scratchPadNumber = "        "; //8
    string _scratchPad1 = "  ";
    string _scratchPad2 = "  ";

    uint _masterCaution = 0;

    protected override string GetFontFile() => "resources/a10c-font-21x31.json";
    protected override string GetAircraftName() => SupportedAircrafts.FA18C_Name;

    public FA18C_Listener(ICdu mcdu, UserOptions options) : base(mcdu, SupportedAircrafts.FA18C, options)
    {
    }

    protected override void InitializeDcsBiosControls()
    {
        UFC_OPTION_DISPLAY_1 = DCSBIOSControlLocator.GetStringDCSBIOSOutput("UFC_OPTION_DISPLAY_1");
        UFC_OPTION_CUEING_1 = DCSBIOSControlLocator.GetStringDCSBIOSOutput("UFC_OPTION_CUEING_1");
        UFC_OPTION_DISPLAY_2 = DCSBIOSControlLocator.GetStringDCSBIOSOutput("UFC_OPTION_DISPLAY_2");
        UFC_OPTION_CUEING_2 = DCSBIOSControlLocator.GetStringDCSBIOSOutput("UFC_OPTION_CUEING_2");
        UFC_OPTION_DISPLAY_3 = DCSBIOSControlLocator.GetStringDCSBIOSOutput("UFC_OPTION_DISPLAY_3");
        UFC_OPTION_CUEING_3 = DCSBIOSControlLocator.GetStringDCSBIOSOutput("UFC_OPTION_CUEING_3");
        UFC_OPTION_DISPLAY_4 = DCSBIOSControlLocator.GetStringDCSBIOSOutput("UFC_OPTION_DISPLAY_4");
        UFC_OPTION_CUEING_4 = DCSBIOSControlLocator.GetStringDCSBIOSOutput("UFC_OPTION_CUEING_4");
        UFC_OPTION_DISPLAY_5 = DCSBIOSControlLocator.GetStringDCSBIOSOutput("UFC_OPTION_DISPLAY_5");
        UFC_OPTION_CUEING_5 = DCSBIOSControlLocator.GetStringDCSBIOSOutput("UFC_OPTION_CUEING_5");
        UFC_SCRATCHPAD_NUMBER_DISPLAY = DCSBIOSControlLocator.GetStringDCSBIOSOutput("UFC_SCRATCHPAD_NUMBER_DISPLAY");
        UFC_SCRATCHPAD_STRING_1_DISPLAY = DCSBIOSControlLocator.GetStringDCSBIOSOutput("UFC_SCRATCHPAD_STRING_1_DISPLAY");
        UFC_SCRATCHPAD_STRING_2_DISPLAY = DCSBIOSControlLocator.GetStringDCSBIOSOutput("UFC_SCRATCHPAD_STRING_2_DISPLAY");
        MASTER_CAUTION_LT = DCSBIOSControlLocator.GetUIntDCSBIOSOutput("MASTER_CAUTION_LT");
    }

    public override void DcsBiosDataReceived(object sender, DCSBIOSDataEventArgs e)
    {
        try
        {
            UpdateCounter(e.Address, e.Data);

            if (MASTER_CAUTION_LT != null && e.Address.Equals(MASTER_CAUTION_LT.Address))
            {
                uint newMasterCaution = MASTER_CAUTION_LT.GetUIntValue(e.Data);
                if (_masterCaution != newMasterCaution)
                {
                    _masterCaution = newMasterCaution;
                    if (_masterCaution == 0)
                    {
                        mcdu.Leds.Fail = false;
                    }
                    else
                    {
                        mcdu.Leds.Fail = true;
                    }
                    mcdu.RefreshLeds();
                }
            }
        }
        catch (Exception ex)
        {
            App.Logger.Error(ex, "Failed to process DCS-BIOS data");
        }
    }

    public override void DCSBIOSStringReceived(object sender, DCSBIOSStringDataEventArgs e)
    {
        var output = GetCompositor(DEFAULT_PAGE);
        try
        {
            string incomingData;
            const string filler = "                   ";

            output.Green().Line(0).ClearRow();

            if (UFC_SCRATCHPAD_NUMBER_DISPLAY != null && e.Address.Equals(UFC_SCRATCHPAD_NUMBER_DISPLAY.Address))
            {
                incomingData = e.StringData;
                if (string.Compare(incomingData, "   pww0w") == 0)
                {
                    incomingData = "   ERROR";
                }
                if (string.Compare(incomingData, _scratchPadNumber) != 0)
                {
                    _scratchPadNumber = incomingData;
                }
            }
            if (UFC_SCRATCHPAD_STRING_1_DISPLAY != null && e.Address.Equals(UFC_SCRATCHPAD_STRING_1_DISPLAY.Address))
            {
                incomingData = e.StringData;
                if (string.Compare(incomingData, _scratchPad1) != 0)
                {
                    _scratchPad1 = incomingData;
                }
            }
            if (UFC_SCRATCHPAD_STRING_2_DISPLAY != null && e.Address.Equals(UFC_SCRATCHPAD_STRING_2_DISPLAY.Address))
            {
                incomingData = e.StringData;
                if (string.Compare(incomingData, _scratchPad2) != 0)
                {
                    _scratchPad2 = incomingData;
                }
            }

            output.Line(1).WriteLine(string.Format("{0,2}{1,2}{2,8}", _scratchPad1, _scratchPad2, _scratchPadNumber));

            if (UFC_OPTION_DISPLAY_1 != null && e.Address.Equals(UFC_OPTION_DISPLAY_1.Address))
            {
                incomingData = e.StringData;
                if (string.Compare(incomingData, _option1) != 0)
                {
                    _option1 = incomingData;
                }
            }

            if (UFC_OPTION_CUEING_1 != null && e.Address.Equals(UFC_OPTION_CUEING_1.Address))
            {
                incomingData = e.StringData;
                if (string.Compare(incomingData, _cue1) != 0)
                {
                    _cue1 = incomingData;
                }
            }
            output.Line(2).WriteLine(string.Format("{2,19}{0,1}{1,4}", _cue1, _option1, filler));
            output.Line(3).ClearRow();

            if (UFC_OPTION_DISPLAY_2 != null && e.Address.Equals(UFC_OPTION_DISPLAY_2.Address))
            {
                incomingData = e.StringData;
                if (string.Compare(incomingData, _option2) != 0)
                {
                    _option2 = incomingData;
                }
            }

            if (UFC_OPTION_CUEING_2 != null && e.Address.Equals(UFC_OPTION_CUEING_2.Address))
            {
                incomingData = e.StringData;
                if (string.Compare(incomingData, _cue2) != 0)
                {
                    _cue2 = incomingData;
                }
            }
            output.Line(4).WriteLine(string.Format("{2,19}{0,1}{1,4}", _cue2, _option2, filler));
            output.Line(5).ClearRow();

            if (UFC_OPTION_DISPLAY_3 != null && e.Address.Equals(UFC_OPTION_DISPLAY_3.Address))
            {
                incomingData = e.StringData;
                if (string.Compare(incomingData, _option3) != 0)
                {
                    _option3 = incomingData;
                }
            }

            if (UFC_OPTION_CUEING_3 != null && e.Address.Equals(UFC_OPTION_CUEING_3.Address))
            {
                incomingData = e.StringData;
                if (string.Compare(incomingData, _cue3) != 0)
                {
                    _cue3 = incomingData;
                }
            }
            output.Line(6).WriteLine(string.Format("{2,19}{0,1}{1,4}", _cue3, _option3, filler));
            output.Line(7).ClearRow();

            if (UFC_OPTION_DISPLAY_4 != null && e.Address.Equals(UFC_OPTION_DISPLAY_4.Address))
            {
                incomingData = e.StringData;
                if (string.Compare(incomingData, _option4) != 0)
                {
                    _option4 = incomingData;
                }
            }

            if (UFC_OPTION_CUEING_4 != null && e.Address.Equals(UFC_OPTION_CUEING_4.Address))
            {
                incomingData = e.StringData;
                if (string.Compare(incomingData, _cue4) != 0)
                {
                    _cue4 = incomingData;
                }
            }
            output.Line(8).WriteLine(string.Format("{2,19}{0,1}{1,4}", _cue4, _option4, filler));
            output.Line(9).ClearRow();

            if (UFC_OPTION_DISPLAY_5 != null && e.Address.Equals(UFC_OPTION_DISPLAY_5.Address))
            {
                incomingData = e.StringData;
                if (string.Compare(incomingData, _option5) != 0)
                {
                    _option5 = incomingData;
                }
            }

            if (UFC_OPTION_CUEING_5 != null && e.Address.Equals(UFC_OPTION_CUEING_5.Address))
            {
                incomingData = e.StringData;
                if (string.Compare(incomingData, _cue5) != 0)
                {
                    _cue5 = incomingData;
                }
            }
            output.Line(10).WriteLine(string.Format("{2,19}{0,1}{1,4}", _cue5, _option5, filler));
            output.Line(11).ClearRow();
        }
        catch (Exception ex)
        {
            App.Logger.Error(ex, "Failed to process DCS-BIOS string data");
        }
    }
}
