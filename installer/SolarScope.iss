; SolarScope installer (Inno Setup 6). Built by installer/build.ps1, do not compile directly.
; Per-user install: no admin rights needed and solarscope.settings.json stays editable.

#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif
#define SourceDir "..\dist\app"

[Setup]
AppId={{6E0F3C2A-5B7D-4C1E-9A8F-2D4B6C8E0A13}
AppName=SolarScope
AppVersion={#AppVersion}
AppPublisher=Tim SolarScope
DefaultDirName={localappdata}\Programs\SolarScope
DefaultGroupName=SolarScope
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.19041
OutputDir=..\dist
OutputBaseFilename=SolarScope-Setup
SetupIconFile=..\src\SolarScope.Desktop\Assets\solarscope.ico
UninstallDisplayIcon={app}\SolarScope.Desktop.exe
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\SolarScope"; Filename: "{app}\SolarScope.Desktop.exe"
Name: "{group}\Uninstall SolarScope"; Filename: "{uninstallexe}"
Name: "{autodesktop}\SolarScope"; Filename: "{app}\SolarScope.Desktop.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\SolarScope.Desktop.exe"; Description: "{cm:LaunchProgram,SolarScope}"; Flags: nowait postinstall skipifsilent
