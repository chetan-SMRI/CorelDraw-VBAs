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

1. Start CorelDRAW 2024 or 2025.
2. Open the document you want to panel.
3. Select the artwork/image.
4. Run `SMRI.PanelMaker\bin\Release\SMRI.PanelMaker.exe`.
5. On first run, enter the license key.
6. Enter media widths and overlap when prompted.

Activation is saved to:

```text
C:\ProgramData\SMRI\PanelMaker\license.json
```

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
