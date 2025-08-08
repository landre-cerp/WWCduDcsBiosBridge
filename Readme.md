# McduDCSBiosBridge
This console app is the bridge between DCS and the Winwing MCDU 

DCS <-> Dcs Bios <-> This App <-> Winwing MCDU 

## A10C support. 
At the moment this is the only DCS Aircraft supported 

### Keyboard 
You need to map keys in DCS using the default Joystick BTN sent by the device. 

### Leds 

| Led     | Dcs indicator |
|---------|---------------|
| Fail | Master caution   | 
| Fm1  | Gun Ready        |
| Ind  | NWS indicator    |
| Fm2  | Cockpit indicator|

Keyboard backlight is mappend on the A10 console Rotary ( lower right part of the Right Pedestal )
Use of Brt/Dim in the plane should change things on the Mcdu 

### Display 

Some A10C chars do not exists on the default Font of the Winwing. 
I've made some mapping that "Looks good", but it's a matter of taste :) . 

My goal is to, one day, be able to use a font that matches the one in the Aircraft. 

### Know issues 
- Brightness of things can mismatch the plane situation 
	- You can use Dim to make it 0 on both airplane / Mcdu , then crank it up 
- Cursor can be erratic. 
	- If you try to enter Senaki as a next FP point, typing SEN should prefill Senaki and you should observe a dancing cursor. It's what i receive from DCSBios ! 

# DCSBios Installation

You can get the Latest Release here

https://github.com/DCS-Skunkworks/dcs-bios/releases

Download .zip file (latest at the moment is v0.8.4)

https://github.com/DCS-Skunkworks/dcs-bios/releases/download/v0.8.4/DCS-BIOS_v0.8.4.zip

Open zip file, drag&drop DCS-BIOS Folder in the DCS saved game / Script folder. 
** If you already have an existing Export.Lua DOT NOT OVERWRITE IT with the one in the zip **
** otherwise you can also drag&drop the export.lua file 

```
Saved game/
├── script/
│   ├── export.lua
│   └── DCS-BIOS/
```

In the script folder, modify the export.lua to add this line. 

```lua
dofile(lfs.writedir() .. [[Scripts\DCS-BIOS\BIOS.lua]])
```

If you open the .lua on the zip file this is what you should find 

# Application installation 

Unzip the application in the folder of your choice.
Open the confi.json and modify the dcsBiosJsonLocation 

Default should be in %USERPROFILE%\Saved Games\DCS\Scripts\ ...  
(name depends on localisation, for example Fr is "Parties enregistrées" ).
Mine is on d:/saved games/DCS/Scripts/ ... 

```lua
"dcsBiosJsonLocation": "D:\\Saved Games\\DCS\\Scripts\\DCS-BIOS\\doc\\json"
```
This should match the above folders 
IP and ports in the config.json are the default one of dcsbiosconfig.lua 

# Fly ! 

- Plug your Winwing(tm) MCDU
- Launch the McduDcsBiosBridge.exe
- Launch DCS
- Start yout A10.
