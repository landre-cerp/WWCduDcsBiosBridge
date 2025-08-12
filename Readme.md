# McduDCSBiosBridge
This console app is the bridge between DCS and the Winwing MCDU 

DCS <-> Dcs Bios <-> This App <-> Winwing MCDU 

## Keyboard 
You need to map keys in DCS using the default Joystick BTN sent by the device. 

## Aircraft support. 


### A10C 
The original target is the A10C,Which is only aircraft that has a MCDU in DCS. ( ch47 cdu is not yes readable in DCSBios )

#### Leds 

| Led     | Dcs indicator |
|---------|---------------|
| Fail | Master caution   | 
| Fm1  | Gun Ready        |
| Ind  | NWS indicator    |
| Fm2  | Cockpit indicator|

Keyboard backlight is mappend on the A10 console Rotary ( lower right part of the Right Pedestal )
Use of Brt/Dim in the plane should change things on the Mcdu 
 

### Other Aircrafts

- AH64-D ( very basic support, some UFD informations and Keyboard left of the Pilot )
- FA18C ( very basic support, only the UFC fields  )

Fail led binded to the Master caution.

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

**IF YOU update and ALREADY HAVE A Config.json FILE, DO NOT OVERWRITE IT.
REUSEIT !** 

Unzip the application in the folder of your choice.
Open the confi.json and modify the dcsBiosJsonLocation 

```json
{
  "ReceiveFromIpUdp": "239.255.50.10",
  "SendToIpUdp": "127.0.0.1",
  "ReceivePortUdp": 5010,
  "SendPortUdp": 7778,
  "dcsBiosJsonLocation": "<< your path to DCS-BIOS JSON files >>"
}
```

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
- Launch the McduDcsBiosBridge.exe ( see command line options below to configure your shortcut ))
- Launch DCS
- Start your Aircraft (A10C or AH64-D)

## Command line options 
```
these are A10c specific options ( no effect on AH64-D )
-cms # Use free space on the CDU for CMS screen display
-ba # Prefer bottom alignment for the MCDU ( Free space on top for CMS ))
