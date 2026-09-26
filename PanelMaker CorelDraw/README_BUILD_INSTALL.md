# SMRI Panel Maker — Build and Update

SMRI Panel Maker is a Windows `.exe` that connects to an open CorelDRAW document. It requires .NET Framework 4.8 and does not need a separate GMS build or installer.

## Files needed for building

Keep these together:

```text
SMRI.PanelMaker.sln
SMRI.PanelMaker\
```

The `SMRI.PanelMaker` folder contains the actual source code and project file.

`SMRI.PanelMaker.sln` is a small Visual Studio solution file that points to:

```text
SMRI.PanelMaker\SMRI.PanelMaker.csproj
```

The `.sln` does not contain the program code. It simply makes the project easy to open and build in Visual Studio. You can build by opening the `.csproj` directly, but when sending the project to someone else, send both the `.sln` file and the complete `SMRI.PanelMaker` folder.

## First build

Do this on a Windows PC:

1. Install Visual Studio 2022.
2. During installation, select **.NET desktop development**.
3. Make sure the **.NET Framework 4.8 Developer Pack** is installed.
4. Open `SMRI.PanelMaker.sln` in Visual Studio.
5. At the top of Visual Studio, select **Release** and **Any CPU**.
6. Click **Build > Build Solution**.

The finished files will be created here:

```text
SMRI.PanelMaker\bin\Release\SMRI.PanelMaker.exe
SMRI.PanelMaker\bin\Release\SMRI.PanelMaker.exe.config
```

Copy both files to:

```text
C:\SMRI\PanelMaker\
```

You can also build from a Visual Studio Developer Command Prompt:

```bat
msbuild SMRI.PanelMaker.sln /p:Configuration=Release /p:Platform="Any CPU"
```

## Run Panel Maker

1. Start CorelDRAW normally.
2. Open your CorelDRAW document.
3. Select the artwork.
4. Run:

```text
C:\SMRI\PanelMaker\SMRI.PanelMaker.exe
```

Do not run CorelDRAW or Panel Maker as administrator unless both programs are running as administrator.

On first use, Panel Maker asks how many days the license should remain valid and then asks for an 8-digit activation code. Generate the code from the project folder with:

```sh
python3 Adobe/TOTP_gen.py
```

The code is time-sensitive, so enter it immediately. The activation lasts for the number of days entered and does not require internet access.

## Update the EXE

After changing the source code:

1. Open `SMRI.PanelMaker.sln`.
2. Select **Release** and **Any CPU**.
3. Click **Build > Rebuild Solution**.
4. Close the currently running Panel Maker.
5. Copy these newly built files:

```text
SMRI.PanelMaker\bin\Release\SMRI.PanelMaker.exe
SMRI.PanelMaker\bin\Release\SMRI.PanelMaker.exe.config
```

6. Replace the old files in:

```text
C:\SMRI\PanelMaker\
```

Existing activation and saved width presets remain on the computer after replacing the EXE.
