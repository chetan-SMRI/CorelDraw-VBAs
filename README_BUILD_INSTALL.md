# SMRI Panel Maker build and install

This project is a simple .NET Framework 4.8 Windows EXE that connects to the currently running CorelDRAW instance through COM automation and modifies the active document. The same EXE can be used for CorelDRAW 2024 and 2025.

## Before building

1. Open `SMRI.PanelMaker\App.config`.
2. The license API URL is configured as:

```text
https://shrimayanand.com/api/method/cdr-activator?machineId=123&licenseKey=chetan@123
```

The EXE replaces the sample query values at runtime with the real generated `machineId` and entered `licenseKey`, then sends a POST request.

3. On invalid license, the API can return:

```json
{}
```

4. On valid license, the API should return:

```json
{
  "message": {
    "valid": true,
    "message": "Activated",
    "activationToken": "abcdefghijklf"
  }
}
```

If the nested `message.valid` is not `true`, the EXE stops before touching CorelDRAW.

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
C:\ProgramData\SMRI\PanelMaker\license.json
```

If the EXE says CorelDRAW is not running while Task Manager shows CorelDRAW, check Windows permission levels. CorelDRAW and `SMRI.PanelMaker.exe` must both run normally, or both run as administrator. The usual setup is: run CorelDRAW normally, then run SMRI Panel Maker normally from the CorelDRAW launcher macro or Start menu.

If the error continues, CorelDRAW's COM automation registration may be missing on that PC. Try opening CorelDRAW once as administrator, close it, then reopen CorelDRAW normally and run SMRI Panel Maker normally. The EXE also writes a diagnostic file here:

```text
C:\ProgramData\SMRI\PanelMaker\coreldraw-com-diagnostic.txt
```

If panel widths show values like `4572` for artwork that should be `180` inches, rebuild and copy the latest EXE. That means the old EXE was using CorelDRAW's millimeter unit value instead of inches.

## Add CorelDRAW launcher macro manually

Use this while testing before you build the installer.

1. Make sure these files exist:

```text
C:\SMRI\PanelMaker\SMRI.PanelMaker.exe
C:\SMRI\PanelMaker\SMRI.PanelMaker.exe.config
```

2. Open CorelDRAW.
3. Open `Tools > Scripts > Script Editor`.
4. In the Script Editor, choose `File > Import File`.
5. Import this file from the project:

```text
Launcher\SMRI_PanelMaker_Launcher.bas
```

6. Save the macro project when CorelDRAW asks.
7. Back in CorelDRAW, open `Tools > Scripts > Run Script`.
8. Select and run:

```text
SMRI_RunPanelMaker
```

The macro simply launches:

```text
C:\SMRI\PanelMaker\SMRI.PanelMaker.exe
```

## Create a CorelDRAW toolbar button

For commercial use, the button should point to the launcher macro, not directly to the EXE. The macro keeps the CorelDRAW workflow simple and launches the installed EXE from `C:\SMRI\PanelMaker`.

First make sure the launcher macro is installed and visible in CorelDRAW:

1. Copy or import `Launcher\SMRI_PanelMaker_Launcher.bas`.
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

## CorelDRAW launcher macro

The installed macro exposes:

```vb
SMRI_RunPanelMaker
```

It runs:

```text
C:\SMRI\PanelMaker\SMRI.PanelMaker.exe
```
