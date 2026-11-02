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

The export form includes an optional customer name. When supplied, each exported
object gets a temporary top caption such as `Matt Vinyl 23x36 | Subham Enterprises`,
using the filename prefix and the object's dimensions. The temporary CorelDRAW text
is removed after that file is exported. The customer name is also used as the filename
suffix. Leaving the field blank exports exactly as before, without adding a caption
or suffix.

CorelDRAW and SMRI Exporter must use the same Windows permission level. Normally, neither should use **Run as administrator**.

Raster export uses late-bound CorelDRAW automation so the EXE is not tied to the
exact `IVGDocument` interface ID of the CorelDRAW version used to build it.

On first use, enter the license duration in days and then the current 8-digit activation code. The license is stored separately from Panel Maker at:

```text
%LOCALAPPDATA%\SMRI\Exporter\license.dat
```
