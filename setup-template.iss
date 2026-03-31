; Inno Setup script for EventViewer Command Palette extension

#define MyAppName "EventViewer"
#define MyAppVersion "0.0.1"
#define MyAppPublisher "niels9001"
#define MyAppURL "https://github.com/niels9001/CmdPal-EventViewer"
#define MyAppCLSID "{7579c206-295a-438b-91ca-70ee7b2fce13}"

[Setup]
AppId={#MyAppCLSID}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
OutputBaseFilename={#MyAppName}_{#MyAppVersion}_{#SetupSetting("ArchitecturesAllowed")}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
OutputDir=Installer

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Registry]
; Register the COM server for Command Palette discovery
Root: HKCU; Subkey: "Software\Classes\CLSID\{#MyAppCLSID}"; ValueType: string; ValueName: ""; ValueData: "{#MyAppName}"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\CLSID\{#MyAppCLSID}\InprocServer32"; ValueType: string; ValueName: ""; ValueData: "{app}\{#MyAppName}.dll"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\CLSID\{#MyAppCLSID}\InprocServer32"; ValueType: string; ValueName: "ThreadingModel"; ValueData: "Both"; Flags: uninsdeletekey

[UninstallDelete]
Type: filesandordirs; Name: "{app}"
