# Contributing Guidelines

Thank you for your interest in contributing to this C# project! 🎉  
We welcome all contributions, whether it’s bug reports, feature requests, code improvements, or documentation updates.

---

## 🚀 How to Contribute

### Prerequisites
- Ensure you have Git installed on your machine.
- Visual Studio
- .NET 8 SDK
- DCS-BIOS installed in your DCS World Saved Games folder. ( check project README for details)
https://github.com/DCS-Skunkworks/dcs-bios (nightly build required for CH-47F)

### Recommended Tools
Bort https://github.com/DCS-Skunkworks/Bort or any other Dcsbios Reference tool to help you find the right addresses and values.
This is where you get "CDU_BRT" you use in 
```csharp
_CDU_BRT = DCSBIOSControlLocator.GetUIntDCSBIOSOutput("CDU_BRT");
```

### 1. Fork & Clone
- Fork this repository.
- Clone your fork locally
- ```bash
  git clone --recurse-submodules <your-forked-repo-url>
  ```

### 2. Create a Branch
**If you use Visual Studio, select the McduDcsBiosBridge repository**
- Create a new branch for your feature or bug fix:
  ```bash
  git checkout -b my-feature-branch
  ```
  Replace `my-feature-branch` with a descriptive name.
- I'm not strict on branch names. Here are some examples:
  - `fix-issue-123`
  - `add-new-feature`
  - `improve-documentation`

or commitize style like:
  - `feat/add-new-feature`
  - `fix/issue-123`
  - `docs/update-readme`

### 3. Make Changes
- Make your changes in the appropriate files.
- Follow the existing coding style and conventions.
- For C# code, target `.NET 8`

### Add a new Aircraft
- Find the Aircraft number in dcs-bios_modules.txt (For Example: F-14 = 16))
- Add a menu entry in DeviceContext.cs and handle the ReadMenu

### 4. Test Your Changes
- There's no automated test suite (as most of the tests are using the physical device), please ensure to:
- Review your code for any errors or typos.

### 5. Commit & Push
- Commit your changes with a clear message:

  ```bash
  git commit -m "Description of my changes"
  ```
- Push your changes to your forked repository:
  ```bash
  git push origin my-feature-branch
  ```

### 6. Create a Pull Request
- Go to the original repository where you want to merge your changes.
- Click on "New Pull Request".
- Select your branch with the changes and create the pull request.
- Provide a clear description of the changes and why they are needed.

---

## 📝 Guidelines

- Please ensure your code adheres to the existing code style and conventions. (any improvements are welcome)
- Write clear, concise commit messages.
- Keep your changes focused on one issue or feature.

---

## ❓ FAQ

**Q: Keeps complaining that it can't find Aircraft 50 (CH47F)**
A: Add the CH-47F to the supported aircraft list in dcs-bios_modules.txt after the last entry (49)
It's because the CH-47F is not yet handled in the DCSFPCommon library.
```
OH-58D|49|OH-58D Kiowa Warrior
CH-47F|50|CH-47F Chinook
```

**Q: What if I found a bug?**  
A: Please check if the bug is already reported. If not, feel free to open a new issue with steps to reproduce the bug.

**Q: Can I contribute to the documentation?**  
A: Absolutely! We welcome documentation improvements. You can follow the same process: fork, clone, branch, and contribute.

**Q: How do I know if my changes are acceptable?**  
A: Follow the coding standards of the project, test your changes, and ensure your changes are meaningful and well-explained.

---

Thank you for considering contributing to our project! Your help is greatly appreciated. If you have any questions, feel free to ask in the discussions or issues section.

**Frontpanel Integration:**
- Check if `frontpanelHub.HasFrontpanels` before handling frontpanel-specific updates
- Use `frontpanelState` to update frontpanel display values (speed, heading, altitude, vertical speed)
- Set `refresh_frontpanel` flag when frontpanel data changes
- For FCU/EFIS devices, cast to `FcuEfisState` for additional properties like barometric pressure
- For PAP3 devices, cast to `Pap3State` for device-specific features
- For PDC-3N devices, only brightness control is available (no display or LED features)
- The frontpanel display updates automatically via the timer in `AircraftListener` base class
- **Brightness:** When setting brightness via `FrontpanelHub.SetBrightness()`, convert DCS-BIOS values to the 0-255 byte range:
  ```csharp
  var rawBrightness = _CONSOLE_BRT!.GetUIntValue(e.Data);
  var brightness = (byte)(rawBrightness * 255 / _CONSOLE_BRT.MaxValue);
  frontpanelHub.SetBrightness(brightness, brightness, brightness);
  ```
  **Do not** convert to percentage (0-100) first, as this limits maximum brightness to only 39% of actual capability.
- See `A10C_Listener.cs` for a complete frontpanel integration example with altitude drum conversion

**Supported Frontpanel Devices:**
- **FCU/EFIS**: Full display, LED control, brightness control
- **PAP3**: Full display, LED control, brightness control  
- **PDC-3N**: Brightness control only (no display or LEDs)
