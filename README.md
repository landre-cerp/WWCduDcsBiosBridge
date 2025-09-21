
[![Release][release-shield]][release-url]
[![Discord][discord-shield]][discord-invite-url]
![Build Status][build-shield]
![License](https://img.shields.io/github/license/landre-cerp/WWCduDcsBiosBridge)

# WWCduDCSBiosBridge

This console application bridges DCS World with the Winwing MCDU hardware, enabling real-time data exchange between the simulator and the physical device.

**Data Flow:** DCS <-> DCS-BIOS <-> This App <-> Winwing MCDU

## Quick Start

1. **Install DCS-BIOS** (see detailed instructions below)
2. **Download and extract** this application to your preferred folder
3. **Configure** by running the application once to generate `config.json`
4. **Update** the `dcsBiosJsonLocation` path in `config.json`
5. **Connect** your Winwing MCDU and run the application
6. **Launch DCS** and select your aircraft from the MCDU menu
7. optional create a shortcut to add commandline options (see below)

## Requirements

- DCS World
- DCS-BIOS (v0.8.4 or later, nightly build required for CH-47F)
- Winwing CDU hardware (MCDU / PFP3N / PFP7)
- .NET 8.0 runtime

## Supported Aircraft

| Aircraft | Support Level | Features |
|----------|---------------|----------|
| **A10C** | Full | Complete MCDU functionality, LED indicators, brightness control |
| **AH-64D** | Basic | UFD information, keyboard display |
| **FA-18C** | Basic | UFC fields display |
| **CH-47F** | Basic | Pilot or CoPilot CDU (requires DCS-BIOS nightly build) |
| **F15E** | Basic | UFC Lines 1-6 by smreki |

### LED Mappings (A10C)

| MCDU LED | DCS Indicator |
|----------|---------------|
| Fail | Master Caution |
| FM1 | Gun Ready |
| IND | NWS Indicator |
| FM2 | Cockpit Indicator |

### LED Mappings other aircraft
| MCDU LED | DCS Indicator |
|----------|---------------|
| CDU LED | DCS Indicator |
| Fail | Master Caution |

## Installation

### DCS-BIOS Setup

1. **Download** the latest DCS-BIOS release:
   - Standard: https://github.com/DCS-Skunkworks/dcs-bios/releases
   - For CH-47F: Download nightly build (or later than 0.8.4)

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
2. **Run** `WWCduDcsBiosBridge.exe`
if no config.json is found, it will create a default one and show you a dialog box to edit it.

<img width="441" height="368" alt="image" src="https://github.com/user-attachments/assets/dca3d830-970d-4741-aeb5-7358658f82f0" />

⚠️ **Important:** When updating the application, do not overwrite your existing `config.json` file.

## Usage

### Controls

- **MCDU Keys:** Map them in DCS.
- **Aircraft Selection:** Use line select keys on startup screen

## Troubleshooting

### Common Issues


**"Connection failed" or MCDU not responding**
- Ensure your Winwing MCDU is properly connected
- Try unplugging and reconnecting the device
- Check that no other applications are using the MCDU

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
- In case of flickering with SimAppPro running, check the

<img width="50%" alt="image" src="https://github.com/user-attachments/assets/1cc6f86f-8fc8-457e-a9fb-11191fcd966d" />

### Logs

All application activity is logged to `log.txt` in the same folder as the executable. Check this file for detailed error information.

Report issues [here](https://github.com/landre-cerp/WWCduDcsBiosBridge/issues), or reach out on Discord [![Discord][discord-shield]][discord-invite-url].

## Known Limitations

- **Aircraft switching:** Requires application restart
- **Cursor behavior:** May appear erratic during waypoint entry (reflects DCS-BIOS data)
- **CH-47F support:** Requires DCS-BIOS nightly build (later than 0.8.4 )
- **Brightness sync:** May not perfectly match aircraft state

## Development

This project is written in C# and targets .NET 8.0. It uses:
- **DCS-BIOS** for DCS communication
- **mcdu-dotnet** for MCDU hardware interface
- **NLog** for logging
- **System.CommandLine** for command-line parsing

## Contributing
see `docs/CONTRIBUTING.md` for contribution guidelines.

## License

See `LICENSE.txt` and `thirdparty-licences.txt` for licensing information.

## Support

For issues and questions, please check the logs first and review the troubleshooting section above.

[release-url]: https://github.com/landre-cerp/WWCduDcsBiosBridge/releases
[release-shield]:  https://img.shields.io/github/release/landre-cerp/WWCduDcsBiosBridge.svg
[discord-shield]: https://img.shields.io/discord/231115945047883778
[discord-invite-url]: https://discord.gg/Td2cGvMhVC
[dcs-forum-discussion]: https://forum.dcs.world/topic/368056-winwing-mcdu-can-it-be-used-in-dcs-for-other-aircraft/page/4/
[build-shield]: https://img.shields.io/github/actions/workflow/status/landre-cerp/WWCduDcsBiosBridge/build-on-tag.yml
