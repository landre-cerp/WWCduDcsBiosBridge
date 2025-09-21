# CH-47F

**As of today CH-47F needs a special version of DCSBIOS** 

⚠️ 0.8.4 is the latest named release , AND IT DOES NOT CONTAIN CDU INFORMATION YET 
So you need to download a most recent version. 

## Cdu Brightness 

The Dim / Brt are doing nothing in the aircraft -> should change the screen brightness , but do not work. 
So, i provided a way to dim the physical CDU by linking it to the Key backlight information 

If you check the "CH47F linked ...", everything is controlled by the cdu knobs. 

You start the aircraft. Fine, then the program receives the position of the knob -> 0 by default.
This set all brightness level to 0 ! and look like the program is not working.
Just turn the knob and it should react.

if you want it to behave like in DCS, leave the CH47 checkbox unticked.
Knob only controls Key backlight (and leds i think) 

And i you don't want light management at all, and leave it to SimAppPro for example, tick the Global option that says that 🙂

<img width="2103" height="1242" alt="image" src="https://github.com/user-attachments/assets/2ff01622-d4da-43ef-87ec-fac9aa7bdb22" />
