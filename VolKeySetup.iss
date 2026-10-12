; ==========================================================
; Inno Setup script for VolKey
; Компилируется в Inno Setup Compiler (https://jrsoftware.org/isinfo.php)
; ==========================================================

#define MyAppName "VolKey"
; Версия берётся из собранного exe (свойство Version в VolKey.csproj).
#define MyAppVersion GetVersionNumbersString(AddBackslash(SourcePath) + "publish\VolKey.exe")
#define MyAppPublisher "Xyberov"
#define MyAppExeName "VolKey.exe"

[Setup]
AppId={{DC0AAD60-5A52-4764-99E7-DC48B7CD099F}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=Output
OutputBaseFilename=VolKeySetup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
SetupIconFile=Assets\VolKey.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}
VersionInfoVersion={#MyAppVersion}
; Совпадает с мьютексом в Program.cs: установщик попросит закрыть запущенный VolKey.
AppMutex=VolKey.SingleInstance
CloseApplications=yes

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[Tasks]
Name: "desktopicon"; Description: "Создать ярлык на рабочем столе"; GroupDescription: "Дополнительные ярлыки:"; Flags: checkedonce

[Files]
; Сначала соберите exe: dotnet publish -c Release -p:PublishProfile=Installer
Source: "publish\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Запустить {#MyAppName}"; Flags: nowait postinstall skipifsilent

[Registry]
; Автозапуск включает сама программа, а удаляет запись — деинсталлятор.
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueName: "VolKey"; ValueType: none; Flags: uninsdeletevalue

[UninstallDelete]
Type: filesandordirs; Name: "{app}"
Type: filesandordirs; Name: "{localappdata}\VolKey"
