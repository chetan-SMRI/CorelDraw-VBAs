# SMRI Exporter — Build

## Build the EXE

1. Open `SMRI.Exporter.sln` in Visual Studio 2022 on Windows.
2. Select **Release** and **Any CPU**.
3. Click **Build > Build Solution**.

The output is created here:

```text
SMRI.Exporter\bin\Release\SMRI.Exporter.exe
SMRI.Exporter\bin\Release\SMRI.Exporter.exe.config
```

Keep the EXE and config file together.

## Run

1. Open CorelDRAW normally.
2. Select the objects to export.
3. Run `SMRI.Exporter.exe` normally.

CorelDRAW and SMRI Exporter must use the same Windows permission level. Normally, neither should use **Run as administrator**.

On first use, enter the license duration in days and then the current 8-digit activation code. The license is stored separately from Panel Maker at:

```text
%LOCALAPPDATA%\SMRI\Exporter\license.dat
```
