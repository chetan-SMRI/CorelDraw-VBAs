#define MyAppName "SMRI Panel Maker"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "SMRI"
#define MyAppExeName "SMRI.PanelMaker.exe"

[Setup]
AppId={{C2DF2C51-83A3-460F-A188-9C0D08C4AC44}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName=C:\SMRI\PanelMaker
DisableDirPage=yes
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=Output
OutputBaseFilename=SMRI.PanelMaker.Setup
Compression=lzma
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=admin
#ifdef SignInstaller
SignTool=signtool /fd SHA256 /td SHA256 /tr http://timestamp.digicert.com /d "{#MyAppName}" $f
SignedUninstaller=yes
SignToolRetryCount=3
SignToolRetryDelay=5000
#endif

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Dirs]
Name: "{commonappdata}\SMRI\PanelMaker"; Permissions: users-modify
Name: "{userappdata}\Corel\CorelDRAW Graphics Suite 2024\Draw\GMS"
Name: "{userappdata}\Corel\CorelDRAW Graphics Suite 2025\Draw\GMS"

[Files]
#ifdef SignInstaller
Source: "..\SMRI.PanelMaker\bin\Release\SMRI.PanelMaker.exe"; DestDir: "{app}"; Flags: ignoreversion signonce
#else
Source: "..\SMRI.PanelMaker\bin\Release\SMRI.PanelMaker.exe"; DestDir: "{app}"; Flags: ignoreversion
#endif
Source: "..\SMRI.PanelMaker\bin\Release\SMRI.PanelMaker.exe.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\Launcher\SMRI_PanelMaker_Launcher.bas"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\Launcher\SMRI_PanelMaker_Launcher.gms"; DestDir: "{userappdata}\Corel\CorelDRAW Graphics Suite 2024\Draw\GMS"; Flags: ignoreversion
Source: "..\Launcher\SMRI_PanelMaker_Launcher.gms"; DestDir: "{userappdata}\Corel\CorelDRAW Graphics Suite 2025\Draw\GMS"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
