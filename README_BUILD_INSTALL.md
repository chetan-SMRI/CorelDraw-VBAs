# SMRI Panel Maker build and install

This project is a simple .NET Framework 4.8 Windows EXE that connects to the currently running CorelDRAW instance through COM automation and modifies the active document. The same EXE can be used for CorelDRAW 2024 and 2025.

## Before building

The license backend is hard-coded to the production endpoints under:

```text
https://shrimayanand.com/api/method/coreldraw_utility.api
```

The app activates with `activate_license`, validates on startup with `validate_license`, uses `force_reauthentication` for corrupted local state or clock rollback, and can call `deactivate_license` when a logout/move flow is added.

## Build

Use Visual Studio 2022 on Windows with the .NET Framework 4.8 Developer Pack installed.

```bat
msbuild SMRI.PanelMaker.sln /p:Configuration=Release /p:Platform="Any CPU"
```

The EXE is created at:

```text
SMRI.PanelMaker\bin\Release\SMRI.PanelMaker.exe
```

## Test manually

Use these steps immediately after building in Visual Studio.

1. Create this folder:

```text
C:\SMRI\PanelMaker
```

2. Copy these two files from `SMRI.PanelMaker\bin\Release` into `C:\SMRI\PanelMaker`:

```text
SMRI.PanelMaker.exe
SMRI.PanelMaker.exe.config
```

3. Start CorelDRAW 2024 or 2025 normally. Do not use "Run as administrator".
4. Open the document you want to panel.
5. Select the artwork/image.
6. Double-click:

```text
C:\SMRI\PanelMaker\SMRI.PanelMaker.exe
```

7. On first run, enter the license key.
8. Choose a saved width preset, create/update a preset, or enter one-time custom widths.
9. Choose vertical or horizontal cuts, enter overlap, and choose whether to add outside bleeding/overlap markers.

Activation is saved to:

```text
C:\ProgramData\SMRI\PanelMaker\license.dat
```

The license file is encrypted and signed locally. If the file is deleted or corrupted, the app will require online reauthentication.

If the EXE says CorelDRAW is not running while Task Manager shows CorelDRAW, check Windows permission levels. CorelDRAW and `SMRI.PanelMaker.exe` must both run normally, or both run as administrator. The usual setup is: run CorelDRAW normally, then run SMRI Panel Maker normally from the CorelDRAW launcher macro or Start menu.

If the error continues, CorelDRAW's COM automation registration may be missing on that PC. Try opening CorelDRAW once as administrator, close it, then reopen CorelDRAW normally and run SMRI Panel Maker normally. The EXE also writes a diagnostic file here:

```text
C:\ProgramData\SMRI\PanelMaker\coreldraw-com-diagnostic.txt
```

If panel widths show values like `4572` for artwork that should be `180` inches, rebuild and copy the latest EXE. That means the old EXE was using CorelDRAW's millimeter unit value instead of inches.

## Add CorelDRAW launcher macro manually

Use this while testing before you build the installer. For customer machines, prefer copying the compiled `.gms` macro project. Do not depend on `Tools > Scripts > Script Editor > Import File`; that can be disabled by CorelDRAW/VBA security, missing VBA components, restricted permissions, or a locked macro project.

Important: a `.gms` file is not a renamed `.bas` text file. It must be a real CorelDRAW macro project saved by CorelDRAW/VBA. If the file is plain text, CorelDRAW will not show it in `Run Script`.

1. Make sure these files exist:

```text
C:\SMRI\PanelMaker\SMRI.PanelMaker.exe
C:\SMRI\PanelMaker\SMRI.PanelMaker.exe.config
```

2. Close CorelDRAW.
3. Copy this file from the project:

```text
Launcher\SMRI_PanelMaker_Launcher.gms
```

to the CorelDRAW user GMS folder for the installed version:

```text
%APPDATA%\Corel\CorelDRAW Graphics Suite 2024\Draw\GMS
%APPDATA%\Corel\CorelDRAW Graphics Suite 2025\Draw\GMS
```

Create the `GMS` folder if it does not exist.

4. Start CorelDRAW.
5. Open `Tools > Scripts > Run Script`.
6. Select and run:

```text
SMRI_RunPanelMaker
```

The `.bas` file is only source code for editing/rebuilding the launcher macro project. The `.gms` file is what should be installed. The macro simply launches:

```text
C:\SMRI\PanelMaker\SMRI.PanelMaker.exe
```

## Rebuild the launcher GMS file

Do this once on a development PC where CorelDRAW VBA editing is working. The generated `.gms` file can then be shipped to customers by the installer.

1. Close CorelDRAW.
2. Open the CorelDRAW user GMS folder:

```text
%APPDATA%\Corel\CorelDRAW Graphics Suite 2024\Draw\GMS
%APPDATA%\Corel\CorelDRAW Graphics Suite 2025\Draw\GMS
```

3. Start CorelDRAW.
4. Open `Tools > Scripts > Script Editor`.
5. Create or open a macro project named:

```text
SMRI_PanelMaker_Launcher
```

6. Add a module and paste the code from:

```text
Launcher\SMRI_PanelMaker_Launcher.bas
```

7. Save the macro project from the VBA editor.
8. Close CorelDRAW.
9. Copy the newly created real `.gms` file from the CorelDRAW `GMS` folder back into this project:

```text
Launcher\SMRI_PanelMaker_Launcher.gms
```

The real `.gms` should be a binary/compound macro project and will usually be much larger than the `.bas` source file. If it opens in Notepad as readable text, it is not a valid `.gms`.

## Create a CorelDRAW toolbar button

For commercial use, the button should point to the launcher macro, not directly to the EXE. The macro keeps the CorelDRAW workflow simple and launches the installed EXE from `C:\SMRI\PanelMaker`.

First make sure the launcher macro is installed and visible in CorelDRAW:

1. Copy `Launcher\SMRI_PanelMaker_Launcher.gms` to the user's CorelDRAW `GMS` folder, or run the installer.
2. Restart CorelDRAW.
3. Confirm that this macro appears in the Scripts list:

```text
SMRI_RunPanelMaker
```

Then create the button:

1. In CorelDRAW, go to `Tools > Options > Customization`.
2. Open `Command Bars`.
3. Click `New`.
4. Name the toolbar:

```text
SMRI
```

5. Enable the checkbox next to the new `SMRI` toolbar so it is visible.
6. In the same Customization window, open `Commands`.
7. In the command category dropdown, choose `Macros`.
8. Find the `SMRI_RunPanelMaker` macro.
9. Drag it onto the `SMRI` toolbar.
10. Optional: select the macro command, open its appearance/icon settings, and set the caption to:

```text
Panel Maker
```

After this, the user only clicks the `Panel Maker` toolbar button.

For a customer installer, the EXE should still install to:

```text
C:\SMRI\PanelMaker\SMRI.PanelMaker.exe
```

The macro button remains stable because it always launches that path.

## Build installer

Install Inno Setup, then compile:

```bat
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" Installer\SMRI.PanelMaker.iss
```

The installer output is:

```text
Installer\Output\SMRI.PanelMaker.Setup.exe
```

The installer copies:

- `SMRI.PanelMaker.exe` to `C:\SMRI\PanelMaker`
- `SMRI.PanelMaker.exe.config` to `C:\SMRI\PanelMaker`
- `Launcher\SMRI_PanelMaker_Launcher.gms` to `%APPDATA%\Corel\CorelDRAW Graphics Suite 2024\Draw\GMS`
- `Launcher\SMRI_PanelMaker_Launcher.gms` to `%APPDATA%\Corel\CorelDRAW Graphics Suite 2025\Draw\GMS`

## Sign the installer for release

Unsigned installers show Windows "Unknown publisher" warnings. This cannot be fixed only by changing Inno Setup text; the EXE must be signed with a real code-signing certificate.

For release builds:

1. Buy a code-signing certificate for the publisher name you want Windows to show, for example `SMRI`.
2. Install Windows SDK so `signtool.exe` is available.
3. In Inno Setup, open `Tools > Configure Sign Tools...`.
4. Add a sign tool named:

```text
signtool
```

5. Set its command to one of these examples.

For a certificate installed in the Windows certificate store:

```bat
"C:\Program Files (x86)\Windows Kits\10\bin\x64\signtool.exe" sign /a $p
```

For a `.pfx` certificate file:

```bat
"C:\Program Files (x86)\Windows Kits\10\bin\x64\signtool.exe" sign /f "C:\Path\To\certificate.pfx" /p YOUR_PFX_PASSWORD $p
```

6. Build the signed release installer:

```bat
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" /DSignInstaller Installer\SMRI.PanelMaker.iss
```

The Inno script signs:

- `SMRI.PanelMaker.exe`
- `SMRI.PanelMaker.Setup.exe`
- the generated uninstaller

After signing, verify the installer:

```bat
signtool verify /pa /v Installer\Output\SMRI.PanelMaker.Setup.exe
```

Note: a standard OV certificate changes Windows from "Unknown publisher" to your publisher name, but Microsoft SmartScreen reputation may still take some downloads/runs to build. An EV code-signing certificate usually establishes reputation faster.

## CorelDRAW launcher macro

The installed macro exposes:

```vb
SMRI_RunPanelMaker
```

It runs:

```text
C:\SMRI\PanelMaker\SMRI.PanelMaker.exe
```
