# LicenseOverlap CorelDRAW Add-on

This project converts the VBA macro in `coreldraw_overlap.gms` into a Visual Studio C#/.NET Framework add-on for CorelDRAW 2024.

## Files

- `LicenseOverlap.csproj` - Visual Studio .NET Framework 4.7.2 class library project.
- `OverlapTool.cs` - C# version of the panel/PowerClip logic from the VBA macro. Main entry point: `RunOverlapTool()`.
- `LicenseManager.cs` - first-run activation logic. Current offline key is `12345`. A commented `LicenseApiUrl` placeholder shows where to add your real server API later.
- `ControlUI.xaml` and `ControlUI.xaml.cs` - hidden WPF host that CorelDRAW loads to register the command data source.
- `DataSource/*.cs` - CorelDRAW UI data source classes. The menu/button calls `OverlapDataSource.MenuItemCommand()`, which calls `RunOverlapTool()`.
- `AppUI.xslt` - registers the add-on UI item and WPF host with CorelDRAW.
- `UserUI.xslt` - adds the command to CorelDRAW's standard toolbar and one context menu location.
- `config.xml` - CorelDRAW add-on resource config.
- `CorelDrw.addon` - marker file used by CorelDRAW add-on loading.

## Build

1. Open `LicenseOverlap.csproj` in Visual Studio on a Windows PC that has CorelDRAW 2024 installed.
2. In Solution Explorer, confirm the COM reference named `VGCore` resolves.
   - If it is broken, remove it and add it again from `References > Add Reference > COM > CorelDRAW 2024 Type Library` or `VGCore`.
3. Build `Release | Any CPU`.
4. Output DLL will be in `LicenseOverlap\bin\Release\LicenseOverlap.dll`.
5. The build also copies the DLL to `LicenseOverlap.CorelAddon`, which is the file name used by the CorelDRAW XSLT host entry.

## Install on a user's CorelDRAW PC

Copy these files into a folder named `LicenseOverlap` under CorelDRAW's add-ons folder:

```text
LicenseOverlap.CorelAddon
AppUI.xslt
UserUI.xslt
config.xml
CorelDrw.addon
```

Common install locations to try. The program-folder location is the normal add-on style; use the per-user location only if your CorelDRAW installation supports it:

```text
C:\Program Files\Corel\CorelDRAW Graphics Suite 2024\Programs64\Addons\LicenseOverlap\
C:\Program Files\Corel\CorelDRAW Graphics Suite 2024\Programs\Addons\LicenseOverlap\
%APPDATA%\Corel\CorelDRAW Graphics Suite 2024\Draw\Addons\LicenseOverlap\
```

Then restart CorelDRAW. The command caption is `SMRI Overlap Panels`. If the command does not appear, start CorelDRAW while holding `F8` to reset/rebuild the workspace UI. This can remove workspace customizations, so test it on your own machine first.

## First Run License

On first click, the add-on asks for a license key.

Current test key:

```text
12345
```

If valid, activation is saved here for the current Windows user:

```text
%LOCALAPPDATA%\SMRI\LicenseOverlap\activation.dat
```

To test activation again, delete that file.

## Adding Server Validation Later

Open `LicenseManager.cs`, paste your real URL into `LicenseApiUrl`, implement `ValidateWithServer()`, and call it from `EnsureActivated()` instead of `ValidateOfflineOnly()`.

## Protecting the Logic

This is safer than distributing VBA source, but .NET DLLs can still be decompiled. For a commercial release, build Release, strong-name sign the DLL, then obfuscate it with a .NET obfuscator before distribution.
