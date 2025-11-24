﻿using DCS_BIOS.ControlLocator;
using DCS_BIOS.EventArgs;
using DCS_BIOS.Serialized;
using McduDotNet;
using System;
using System.Collections.Generic; 
using System.Diagnostics; 

namespace WWCduDcsBiosBridge.Aircrafts;

internal class M2000C_Listener : AircraftListener
{
    // --- Adresses des Registres ---
    private const uint PCN_LIGHTS_ADDRESS = 29380; 
    private const uint CLP_ADDR_1 = 29248; 
    private const uint CLP_ADDR_2 = 29238;
    private const uint CLP_ADDR_3 = 29250;

    // --- Masques PCN ---
    private const uint MASK_ALN = 4096;
    private const uint MASK_PRET = 2048;
    private const uint MASK_NDEG = 16384;

    // --- Masques CLP (Caution Light Panel) - Registre 29248 ---
    private const uint MASK_CLP_BP_D = 1;
    private const uint MASK_CLP_TRANSF = 2;
    private const uint MASK_CLP_NIVEAU = 4; 
    private const uint MASK_CLP_HYD_S = 32;
    private const uint MASK_CLP_EP = 64;     
    private const uint MASK_CLP_BINGO = 128;
    private const uint MASK_CLP_P_CAB = 256;
    private const uint MASK_CLP_TEMP = 512;
    private const uint MASK_CLP_REG_O2 = 1024;
    private const uint MASK_CLP_5MN_O2 = 2048;
    private const uint MASK_CLP_ANEMO = 8192;
    private const uint MASK_CLP_CC = 16384;
    private const uint MASK_CLP_DSV = 32768;

    // --- Masques CLP - Registre 29238 ---
    private const uint MASK_CLP_HYD_1 = 8;
    private const uint MASK_CLP_HYD_2 = 16;
    private const uint MASK_CLP_BATT = 32;
    private const uint MASK_CLP_TRN = 64; 
    private const uint MASK_CLP_ALT_1 = 128;
    private const uint MASK_CLP_ALT_2 = 256;
    private const uint MASK_CLP_HUILE = 512;
    private const uint MASK_CLP_T7 = 1024;
    private const uint MASK_CLP_CALC = 2048;
    private const uint MASK_CLP_SOURIS = 4096;
    private const uint MASK_CLP_PELLES = 8192;
    private const uint MASK_CLP_BP = 16384;
    private const uint MASK_CLP_BP_G = 32768;

    // --- Masques CLP - Registre 29250 ---
    private const uint MASK_CLP_CONDIT = 1;
    private const uint MASK_CLP_CONF = 2;
    private const uint MASK_CLP_PA = 4;
    private const uint MASK_CLP_MAN = 8;
    private const uint MASK_CLP_DOM = 16;
    private const uint MASK_CLP_BECS = 32;
    private const uint MASK_CLP_INCIDENCE = 128; 
    private const uint MASK_CLP_GAIN = 256;
    private const uint MASK_CLP_RPM = 512;
    private const uint MASK_CLP_DECOL = 1024;
    private const uint MASK_CLP_PARK = 2048;
    private const uint MASK_CLP_O2_HA = 4096;

    // --- Contrôles DCS-BIOS ---
    private DCSBIOSOutput? PCN_LIGHTS_REGISTER; 
    private DCSBIOSOutput? CLP_REGISTER_1; 
    private DCSBIOSOutput? CLP_REGISTER_2; 
    private DCSBIOSOutput? CLP_REGISTER_3; 
    
    private DCSBIOSOutput? PCN_DISP_L;
    private DCSBIOSOutput? PCN_DISP_R;
    private DCSBIOSOutput? PCN_DISP_PREP;
    private DCSBIOSOutput? PCN_DISP_DEST;

    private string _pcnDispL = "N00.00.0";
    private string _pcnDispR = "E00.00.0";
    private string _pcnPrep = "P-1"; 
    private string _pcnDest = "D-1"; 
    
    private ushort _clpValue1 = 0;
    private ushort _clpValue2 = 0;
    private ushort _clpValue3 = 0;

    private struct CautionItem 
    {
        public string Text;
        public bool IsRed; 
        public CautionItem(string text, bool isRed) { Text = text; IsRed = isRed; }
    }

    protected override string GetFontFile() => "resources/ah64d-font-21x31.json";
    protected override string GetAircraftName() => SupportedAircrafts.M2000C_Name;

    public M2000C_Listener(ICdu mcdu, UserOptions options) : base(mcdu, SupportedAircrafts.M2000C, options)
    {
    }

    protected override void InitializeDcsBiosControls()
    {
        PCN_DISP_L = DCSBIOSControlLocator.GetStringDCSBIOSOutput("PCN_DISP_L");
        PCN_DISP_R = DCSBIOSControlLocator.GetStringDCSBIOSOutput("PCN_DISP_R");
        PCN_DISP_PREP = DCSBIOSControlLocator.GetStringDCSBIOSOutput("PCN_DISP_PREP");
        PCN_DISP_DEST = DCSBIOSControlLocator.GetStringDCSBIOSOutput("PCN_DISP_DEST");
        
        PCN_LIGHTS_REGISTER = DCSBIOSControlLocator.GetStringDCSBIOSOutput("PCN_DISP_L");
        if (PCN_LIGHTS_REGISTER != null) PCN_LIGHTS_REGISTER.Address = PCN_LIGHTS_ADDRESS;

        CLP_REGISTER_1 = DCSBIOSControlLocator.GetStringDCSBIOSOutput("PCN_DISP_R");
        if (CLP_REGISTER_1 != null) CLP_REGISTER_1.Address = CLP_ADDR_1;

        CLP_REGISTER_2 = DCSBIOSControlLocator.GetStringDCSBIOSOutput("PCN_DISP_PREP");
        if (CLP_REGISTER_2 != null) CLP_REGISTER_2.Address = CLP_ADDR_2;

        CLP_REGISTER_3 = DCSBIOSControlLocator.GetStringDCSBIOSOutput("PCN_DISP_DEST");
        if (CLP_REGISTER_3 != null) CLP_REGISTER_3.Address = CLP_ADDR_3;
    }

    public override void DcsBiosDataReceived(object sender, DCSBIOSDataEventArgs e)
    {
        try
        {
            UpdateCounter(e.Address, e.Data);
            bool refreshLeds = false;
            bool refreshDisplay = false;
            
            if (e.Address == PCN_LIGHTS_ADDRESS)
            {
                ushort val = (ushort)e.Data;
                mcdu.Leds.Ind = (val & MASK_ALN) > 0;
                mcdu.Leds.Rdy = (val & MASK_PRET) > 0;
                mcdu.Leds.Fail = (val & MASK_NDEG) > 0;
                refreshLeds = true;
            }
            
            if (e.Address == CLP_ADDR_1) {
                ushort val = (ushort)e.Data;
                if (_clpValue1 != val) { _clpValue1 = val; refreshDisplay = true; }
            }
            if (e.Address == CLP_ADDR_2) {
                ushort val = (ushort)e.Data;
                if (_clpValue2 != val) { _clpValue2 = val; refreshDisplay = true; }
            }
            if (e.Address == CLP_ADDR_3) {
                ushort val = (ushort)e.Data;
                if (_clpValue3 != val) { _clpValue3 = val; refreshDisplay = true; }
            }
            
            if (refreshLeds) mcdu.RefreshLeds();
            if (refreshDisplay) UpdateCautionPanel();
        }
        catch (Exception ex)
        {
             Debug.WriteLine($"Error: {ex.Message}");
        }
    }

    public override void DCSBIOSStringReceived(object sender, DCSBIOSStringDataEventArgs e)
    {
        var output = GetCompositor(DEFAULT_PAGE);
        try
        {
            if (PCN_DISP_L != null && e.Address == PCN_DISP_L.Address)
            {
                _pcnDispL = e.StringData; 
                UpdateCombinedPCNDisplay(output);
            }
            else if (PCN_DISP_R != null && e.Address == PCN_DISP_R.Address)
            {
                _pcnDispR = e.StringData; 
                UpdateCombinedPCNDisplay(output);
            }
            else if (PCN_DISP_PREP != null && e.Address == PCN_DISP_PREP.Address)
            {
                _pcnPrep = e.StringData;
                UpdateCombinedPrepDestDisplay(output);
            }
            else if (PCN_DISP_DEST != null && e.Address == PCN_DISP_DEST.Address)
            {
                _pcnDest = e.StringData;
                UpdateCombinedPrepDestDisplay(output);
            }
            
            mcdu.RefreshDisplay();
        }
        catch (Exception ex)
        {
            App.Logger.Error(ex, "Failed to process DCS-BIOS string data");
        }
    }
    
    private void UpdateCautionPanel()
    {
        var output = GetCompositor(DEFAULT_PAGE);
        var activeItems = new List<CautionItem>();

        // --- Vérification des voyants (Couleurs mises à jour) ---
        
        // Registre 29238
        if ((_clpValue2 & MASK_CLP_HUILE) > 0) activeItems.Add(new CautionItem("HUILE", true)); 
        if ((_clpValue2 & MASK_CLP_BP) > 0)    activeItems.Add(new CautionItem("B.P.", true)); 
        if ((_clpValue2 & MASK_CLP_T7) > 0)    activeItems.Add(new CautionItem("T7", true)); 
        if ((_clpValue2 & MASK_CLP_BATT) > 0)  activeItems.Add(new CautionItem("BATT", false)); 
        if ((_clpValue2 & MASK_CLP_TRN) > 0)   activeItems.Add(new CautionItem("TR", false)); 
        if ((_clpValue2 & MASK_CLP_ALT_1) > 0) activeItems.Add(new CautionItem("ALT.1", false));
        if ((_clpValue2 & MASK_CLP_ALT_2) > 0) activeItems.Add(new CautionItem("ALT.2", false));
        if ((_clpValue2 & MASK_CLP_CALC) > 0)  activeItems.Add(new CautionItem("CALC", false));
        if ((_clpValue2 & MASK_CLP_SOURIS) > 0) activeItems.Add(new CautionItem("SOURIS", false));
        if ((_clpValue2 & MASK_CLP_PELLES) > 0) activeItems.Add(new CautionItem("PELLE", false));
        if ((_clpValue2 & MASK_CLP_BP_G) > 0)  activeItems.Add(new CautionItem("BP.G", false));
        if ((_clpValue2 & MASK_CLP_HYD_1) > 0) activeItems.Add(new CautionItem("HYD.1", false));
        if ((_clpValue2 & MASK_CLP_HYD_2) > 0) activeItems.Add(new CautionItem("HYD.2", false));

        // Registre 29248
        if ((_clpValue1 & MASK_CLP_DSV) > 0)    activeItems.Add(new CautionItem("DSV", true)); 
        if ((_clpValue1 & MASK_CLP_HYD_S) > 0)  activeItems.Add(new CautionItem("HYD.S", true)); 
        if ((_clpValue1 & MASK_CLP_P_CAB) > 0)  activeItems.Add(new CautionItem("P.CAB", true)); 
        if ((_clpValue1 & MASK_CLP_REG_O2) > 0) activeItems.Add(new CautionItem("REG.O2", true)); 
        if ((_clpValue1 & MASK_CLP_EP) > 0)     activeItems.Add(new CautionItem("EP", true)); 
        if ((_clpValue1 & MASK_CLP_ANEMO) > 0)  activeItems.Add(new CautionItem("ANEMO", false));
        if ((_clpValue1 & MASK_CLP_CC) > 0)     activeItems.Add(new CautionItem("CC", false));
        if ((_clpValue1 & MASK_CLP_TEMP) > 0)   activeItems.Add(new CautionItem("TEMP", false));
        if ((_clpValue1 & MASK_CLP_5MN_O2) > 0) activeItems.Add(new CautionItem("5mn.O2", false));
        if ((_clpValue1 & MASK_CLP_TRANSF) > 0) activeItems.Add(new CautionItem("TRANSF", false)); 
        if ((_clpValue1 & MASK_CLP_BINGO) > 0)  activeItems.Add(new CautionItem("BINGO", false));
        if ((_clpValue1 & MASK_CLP_NIVEAU) > 0) activeItems.Add(new CautionItem("NIVEAU", false));
        if ((_clpValue1 & MASK_CLP_BP_D) > 0)   activeItems.Add(new CautionItem("BP.D", false));

        // Registre 29250
        if ((_clpValue3 & MASK_CLP_PA) > 0)     activeItems.Add(new CautionItem("PA", true)); 
        if ((_clpValue3 & MASK_CLP_GAIN) > 0)   activeItems.Add(new CautionItem("GAIN", true)); 
        if ((_clpValue3 & MASK_CLP_DOM) > 0)    activeItems.Add(new CautionItem("DOM", true)); 
        if ((_clpValue3 & MASK_CLP_CONDIT) > 0) activeItems.Add(new CautionItem("CONDIT", true));
        if ((_clpValue3 & MASK_CLP_RPM) > 0)    activeItems.Add(new CautionItem("RPM", true));
        if ((_clpValue3 & MASK_CLP_DECOL) > 0)  activeItems.Add(new CautionItem("DECOL", true));
        if ((_clpValue3 & MASK_CLP_MAN) > 0)    activeItems.Add(new CautionItem("MAN", false));
        if ((_clpValue3 & MASK_CLP_BECS) > 0)   activeItems.Add(new CautionItem("BECS", false));
        if ((_clpValue3 & MASK_CLP_CONF) > 0)   activeItems.Add(new CautionItem("CONF", false));
        if ((_clpValue3 & MASK_CLP_PARK) > 0)   activeItems.Add(new CautionItem("PARK", false));
        if ((_clpValue3 & MASK_CLP_O2_HA) > 0)  activeItems.Add(new CautionItem("O2 HA", false));
        if ((_clpValue3 & MASK_CLP_INCIDENCE) > 0) activeItems.Add(new CautionItem("ALPHA", false));

        // --- Affichage Dynamique sur 3 Colonnes avec multi-couleurs ---
        
        const int COL_WIDTH_1 = 7; // Largeur colonne 1
        const int COL_WIDTH_2 = 7; // Largeur colonne 2
        const int COL_WIDTH_3 = 6; // Largeur colonne 3 (Total = 20)
        
        int currentLine = 0;
        
        for (int i = 0; i < activeItems.Count; i += 3)
        {
            if (currentLine > 9) break; // Arrêt si on dépasse la zone MCDU

            var lineObj = output.Line(currentLine);
            lineObj.ClearRow(); // Effacer d'abord la ligne

            // --- Colonne 1 (Index i) ---
            if (i < activeItems.Count) 
            {
                var item = activeItems[i];
                string text = item.Text;
                if (text.Length > COL_WIDTH_1) text = text.Substring(0, COL_WIDTH_1);

                lineObj = item.IsRed ? lineObj.Red() : lineObj.Yellow();
                lineObj.Write(text);
                
                // Remplir l'espace restant de la colonne 1 en VERT (couleur par défaut)
                int padding = COL_WIDTH_1 - text.Length;
                if (padding > 0) {
                    lineObj.Green().Write(new string(' ', padding));
                }
            }
            else
            {
                // Si la colonne 1 est vide, remplir l'espace complet en VERT
                lineObj.Green().Write(new string(' ', COL_WIDTH_1));
            }

            // --- Colonne 2 (Index i + 1) ---
            if (i + 1 < activeItems.Count) 
            {
                var item = activeItems[i + 1];
                string text = item.Text;
                if (text.Length > COL_WIDTH_2) text = text.Substring(0, COL_WIDTH_2);

                lineObj = item.IsRed ? lineObj.Red() : lineObj.Yellow();
                lineObj.Write(text);

                // Remplir l'espace restant de la colonne 2 en VERT
                int padding = COL_WIDTH_2 - text.Length;
                if (padding > 0) {
                    lineObj.Green().Write(new string(' ', padding));
                }
            }
            else
            {
                 // Si la colonne 2 est vide, remplir l'espace complet en VERT
                lineObj.Green().Write(new string(' ', COL_WIDTH_2));
            }
            
            // --- Colonne 3 (Index i + 2) ---
            if (i + 2 < activeItems.Count) 
            {
                var item = activeItems[i + 2];
                string text = item.Text;
                if (text.Length > COL_WIDTH_3) text = text.Substring(0, COL_WIDTH_3);

                lineObj = item.IsRed ? lineObj.Red() : lineObj.Yellow();
                lineObj.Write(text);

                // Remplir l'espace restant de la colonne 3 en VERT
                int padding = COL_WIDTH_3 - text.Length;
                if (padding > 0) {
                    lineObj.Green().Write(new string(' ', padding));
                }
            }
            else
            {
                // Si la colonne 3 est vide, remplir l'espace complet en VERT
                lineObj.Green().Write(new string(' ', COL_WIDTH_3));
            }

            currentLine++;
        }

        // Effacer les lignes restantes (de la ligne après le dernier affichage jusqu'à la ligne 10)
        for (int j = currentLine; j <= 10; j++)
        {
            output.Line(j).ClearRow();
        }
        
        mcdu.RefreshDisplay();
    }

    private void UpdateCombinedPCNDisplay(Compositor output)
    {
        string combinedLine = string.Format("{0}{1,10}", _pcnDispL, _pcnDispR);
        output.Line(12).Green().WriteLine(combinedLine);
    }

    private void UpdateCombinedPrepDestDisplay(Compositor output)
    {
        string prepDisplay = string.Format("P:{0}", _pcnPrep); 
        string destDisplay = string.Format("D:{0}", _pcnDest); 
        string combinedLine = string.Format("{0}{1,5}", prepDisplay, destDisplay);
        output.Line(13).Green().WriteLine(combinedLine);
    }
}