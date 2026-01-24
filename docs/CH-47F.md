# CH-47F

**As of today CH-47F needs a special version of DCSBIOS** 

⚠️ 0.8.4 is the latest named release , AND IT DOES NOT CONTAIN CDU INFORMATION YET 
So you need to download the most recent version. 

## Single vs Multiple CDUs

The application automatically detects how many CDUs are connected and adjusts behavior accordingly:

### Single CDU Setup
If you only have **1 CDU connected**, the application will:
- Display a single **"Ch-47F"** option in the aircraft selection menu (no PLT/CPLT choice)
- Automatically switch the CDU display between pilot and co-pilot based on your seat position in DCS
- Monitor the DCS seat position to seamlessly update the CDU display

**YOU ABSOLUTELY NEED TO INSTALL DCS BIOS VERSION DCS-BIOS Nightly 2025-09-21 and Later**
as the seat position is not handled in previous versions.

### Multiple CDU Setup  
If you have **2 or more CDUs connected**, the application will:
- Display both **"Ch-47F (PLT)"** and **"Ch-47F (CPLT)"** options in the aircraft selection menu
- Require you to manually select the role for each CDU
- Keep each CDU fixed to its selected role (pilot or co-pilot) without automatic switching

No configuration is needed - the behavior is entirely automatic based on detection at startup.




## CDU Brightness 

The Dim / Brt are doing nothing in the aircraft -> should change the screen brightness , but do not work. 
So, i provided a way to dim the physical CDU by linking it to the Key backlight information 

If you check the "CH47F linked ...", everything is controlled by the cdu knobs. 

You start the aircraft. Fine, then the program receives the position of the knob -> 0 by default.
This set all brightness level to 0 ! and look like the program is not working.
Just turn the knob and it should react.

if you want it to behave like in DCS, leave the CH47 checkbox unticked.
Knob only controls Key backlight (and leds i think) 

And if you don't want light management at all, and leave it to SimAppPro for example, tick the Global option that says that 🙂

<img width="2103" height="1242" alt="image" src="https://github.com/user-attachments/assets/2ff01622-d4da-43ef-87ec-fac9aa7bdb22" />

