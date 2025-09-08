# McduDCSBiosBridge

This console application bridges DCS World with the Winwing MCDU hardware, enabling real-time data exchange between the simulator and the physical device.

**Data Flow:** DCS <-> DCS-BIOS <-> This App <-> Winwing MCDU

## Quick Start

1. **Install DCS-BIOS** (see detailed instructions below)
2. **Download and extract** this application to your preferred folder
3. **Configure** by running the application once to generate `config.json`
4. **Update** the `dcsBiosJsonLocation` path in `config.json`
5. **Connect** your Winwing MCDU and run the application
6. **Launch DCS** and select your aircraft from the MCDU menu

## Requirements

- DCS World
- DCS-BIOS (v0.8.4 or later, nightly build required for CH-47F)
- Winwing CDU hardware (MCDU / PFP3N / PFP7 )
- .NET 8.0 runtime

## Supported Aircraft

| Aircraft | Support Level | Features |
|----------|---------------|----------|
| **A10C** | Full | Complete MCDU functionality, LED indicators, brightness control |
| **AH-64D** | Basic | UFD information, keyboard display |
| **FA-18C** | Basic | UFC fields display |
| **CH-47F** | Basic | Pilot or CoPilot CDU (requires DCS-BIOS nightly build) |

### LED Mappings (A10C)

| MCDU LED | DCS Indicator |
|----------|---------------|
| Fail | Master Caution |
| FM1 | Gun Ready |
| IND | NWS Indicator |
| FM2 | Cockpit Indicator |

## Installation

### DCS-BIOS Setup

1. **Download** the latest DCS-BIOS release:
   - Standard: https://github.com/DCS-Skunkworks/dcs-bios/releases
   - For CH-47F: Download nightly build

2. **Extract** the DCS-BIOS folder to your DCS saved games Scripts directory:
   ```
   %USERPROFILE%\Saved Games\DCS\Scripts\DCS-BIOS\
   ```

3. **Configure Export.lua** in your Scripts folder:
   ```lua
   dofile(lfs.writedir() .. [[Scripts\DCS-BIOS\BIOS.lua]])
   ```
   
   ⚠️ **Important:** If you already have an Export.lua file, add the line above instead of overwriting it.

### Application Setup

1. **Extract** the application files to your chosen directory
2. **Run** `McduDcsBiosBridge.exe` once to generate the configuration file
3. **Edit** `config.json` with your settings:

```json
{
  "ReceiveFromIpUdp": "239.255.50.10",
  "SendToIpUdp": "127.0.0.1",
  "ReceivePortUdp": 5010,
  "SendPortUdp": 7778,
  "DcsBiosJsonLocation": "C:\\Users\\YourName\\Saved Games\\DCS\\Scripts\\DCS-BIOS\\doc\\json"
}
```

**Path Examples:**
- Windows: `"C:\\Users\\YourName\\Saved Games\\DCS\\Scripts\\DCS-BIOS\\doc\\json"`
- Localized: `"C:\\Users\\YourName\\Parties enregistrées\\DCS\\Scripts\\DCS-BIOS\\doc\\json"` (French)

⚠️ **Important:** When updating the application, do not overwrite your existing `config.json` file.

## Usage

### Running the Application

```bash
# Basic usage
McduDcsBiosBridge.exe

# With options (A10C specific)
McduDcsBiosBridge.exe --bottom-aligned --display-cms
McduDcsBiosBridge.exe -ba -cms
```

### Command Line Options

| Option | Short | Description |
|--------|-------|-------------|
| `--bottom-aligned` | `-ba` | Align display to bottom (A10C only) |
| `--display-cms` | `-cms` | Show CMS on free screen space (A10C only) |
| `--aircraft` | `-a` | Set aircraft number |

### Controls

- **MCDU Keys:** Map in DCS using the default joystick buttons sent by the device
- **Menu Key:** Exit the application
- **Aircraft Selection:** Use line select keys on startup screen

## Troubleshooting

### Common Issues

**"Configuration file not found"**
- The application will create a default `config.json` file
- Update the `DcsBiosJsonLocation` path to point to your DCS-BIOS JSON files

**"Connection failed" or MCDU not responding**
- Ensure your Winwing MCDU is properly connected
- Try unplugging and reconnecting the device
- Check that no other applications are using the MCDU

**"DCS-BIOS folder does not exist"**
- Verify the `DcsBiosJsonLocation` path in `config.json`
- Ensure DCS-BIOS is properly installed
- Check that the JSON files exist in the specified directory

**"No data appearing on MCDU"**
- Start your aircraft in DCS (data appears after aircraft systems are powered)
- Check that DCS-BIOS is working (look for network traffic)
- Verify Export.lua is configured correctly

**Aircraft change not working**
- Restart the application when switching aircraft
- Each aircraft requires a separate application instance

### Brightness Issues

- **Mismatched brightness:** Use the aircraft's brightness controls first, then adjust MCDU
- **A10C:** MCDU brightness is linked to the console rotary control (right pedestal)

### Logs

All application activity is logged to `log.txt` in the same folder as the executable. Check this file for detailed error information.

## Known Limitations

- **Aircraft switching:** Requires application restart
- **Cursor behavior:** May appear erratic during waypoint entry (reflects DCS-BIOS data)
- **CH-47F support:** Requires DCS-BIOS nightly build (not available in official releases)
- **Brightness sync:** May not perfectly match aircraft state

## Development

This project is written in C# and targets .NET 8.0. It uses:
- **DCS-BIOS** for DCS communication
- **mcdu-dotnet** for MCDU hardware interface
- **NLog** for logging
- **System.CommandLine** for command-line parsing

## License

See `LICENSE.txt` and `thirdparty-licences.txt` for licensing information.

## Support

For issues and questions, please check the logs first and review the troubleshooting section above.
