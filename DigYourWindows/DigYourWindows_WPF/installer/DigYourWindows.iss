#ifndef AppName
  #define AppName "DigYourWindows"
#endif

#ifndef AppVersion
  #define AppVersion "0.0.0"
#endif

#ifndef AppPublisher
  #define AppPublisher "LessUp"
#endif

#ifndef AppExeName
  #define AppExeName "DigYourWindows.UI.exe"
#endif

#ifndef PublishDir
  #define PublishDir ""
#endif

#ifndef OutputDir
  #define OutputDir ""
#endif

#ifndef OutputBaseFilename
  #define OutputBaseFilename "DigYourWindows_Setup"
#endif

#if PublishDir == ""
  #error PublishDir is not defined. Use ISCC /DPublishDir=...
#endif

[Setup]
AppId={{3B3E9C70-9B9F-4B70-BD0D-2A2A0CB6F1F3}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\{#AppName}
DisableProgramGroupPage=yes
ArchitecturesInstallIn64BitMode=x64
WizardStyle=modern
Compression=lzma
SolidCompression=yes
OutputDir={#OutputDir}
OutputBaseFilename={#OutputBaseFilename}

[Tasks]
Name: "desktopicon"; Description: "创建桌面快捷方式"; GroupDescription: "附加任务"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "启动 {#AppName}"; Flags: nowait postinstall skipifsilent
