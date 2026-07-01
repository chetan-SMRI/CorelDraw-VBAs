# SMRI Panel Maker build and install

This project is a simple .NET Framework 4.8 Windows EXE that connects to the currently running CorelDRAW instance through COM automation and modifies the active document.

## Before building

1. Open `SMRI.PanelMaker\App.config`.
2. Replace `https://your-server.example.com/api/activate` with your real activation API URL.
3. The API must accept this JSON:

```json
{
  "licenseKey": "customer-key",
  "machineId": "sha256-machine-id"
}
```

4. The API should return this JSON:

```json
{
  "valid": true,
  "message": "Activated",
  "activationToken": "server-generated-token"
}
```

If `valid` is `false`, the EXE stops before touching CorelDRAW.

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

1. Start CorelDRAW 2024.
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

## CorelDRAW launcher macro

The installed macro exposes:

```vb
SMRI_RunPanelMaker
```

It runs:

```text
C:\SMRI\PanelMaker\SMRI.PanelMaker.exe
```
