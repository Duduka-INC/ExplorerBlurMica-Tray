; Inno Setup 7 script for ExplorerBlurMica Tray. Build with installer\build.ps1.
;
; Setup runs elevated, so it also downloads the official ExplorerBlurMica release and registers it
; through the app's own elevated command: one UAC prompt covers everything. If the download fails,
; the app is still installed and ExplorerBlurMica can be installed from the app later.

#ifndef AppVersion
  #define AppVersion "0.1.0"
#endif
#ifndef CliDir
  #define CliDir "..\artifacts\publish\cli"
#endif
#ifndef AppDir
  #define AppDir "..\artifacts\publish\app"
#endif
#ifndef OutputDir
  #define OutputDir "..\artifacts"
#endif

; Pinned release of ExplorerBlurMica that Setup installs; the app updates it later.
#define EbmVersion "2.1.0"
#define EbmUrl "https://github.com/Maplespe/ExplorerBlurMica/releases/download/2.1.0/Release_x64.zip"
#define EbmSha256 "71f678c2356a11a18f9b1e3935d64e035c4f3db71ecf76ff3d27878694f358d5"

; The tray app, and the command-line tool Setup uses for the steps it hands to the core.
#define AppExe "ExplorerBlurMicaTray.exe"
#define CliExe "ebmt.exe"
; Must match Program.MutexName in the app: Setup asks to close the app before replacing it.
#define AppMutexName "ExplorerBlurMicaTray.Running"

[Setup]
AppId={{A5099D13-FE90-4FCE-9557-083982A77B71}
AppName=ExplorerBlurMica Tray
AppVersion={#AppVersion}
AppVerName=ExplorerBlurMica Tray {#AppVersion}
AppPublisher=Duduka-INC
AppPublisherURL=https://github.com/Duduka-INC/ExplorerBlurMica-Tray
AppSupportURL=https://github.com/Duduka-INC/ExplorerBlurMica-Tray/issues
VersionInfoVersion={#AppVersion}
; Must match AppPaths.EbmRoot in the core: ExplorerBlurMica goes to {app}\ebm.
DefaultDirName={autopf}\ExplorerBlurMica-Tray
DisableDirPage=yes
DisableProgramGroupPage=yes
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
SetupArchitecture=x64
MinVersion=10.0.17763
WizardStyle=modern dynamic
UninstallDisplayName=ExplorerBlurMica Tray
UninstallDisplayIcon={app}\{#AppExe}
AppMutex={#AppMutexName}
CloseApplications=yes
OutputDir={#OutputDir}
OutputBaseFilename=ExplorerBlurMica-Tray-Setup-{#AppVersion}
Compression=lzma2/max
SolidCompression=yes
SetupLogging=yes

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"
Name: "ru"; MessagesFile: "compiler:Languages\Russian.isl"

[Messages]
en.WelcomeLabel2=This will install [name/ver] on your computer.%n%nThe File Explorer effects are made by ExplorerBlurMica by Maplespe. Setup downloads its official release from GitHub.
ru.WelcomeLabel2=Программа установит [name/ver] на ваш компьютер.%n%nЭффекты в Проводнике делает ExplorerBlurMica от Maplespe. Программа установки скачает его официальный релиз с GitHub.

[CustomMessages]
en.StatusRegistering=Registering ExplorerBlurMica...
ru.StatusRegistering=Регистрация ExplorerBlurMica...
en.RunRestartExplorer=Restart File Explorer to turn the effect on (the taskbar disappears for a moment)
ru.RunRestartExplorer=Перезапустить Проводник, чтобы включить эффект (панель задач на секунду пропадёт)
en.TaskAutostart=Start ExplorerBlurMica Tray with Windows (for all users)
ru.TaskAutostart=Запускать ExplorerBlurMica Tray вместе с Windows (для всех пользователей)
en.RunApp=Start ExplorerBlurMica Tray
ru.RunApp=Запустить ExplorerBlurMica Tray
en.EbmDownloadFailed=ExplorerBlurMica could not be downloaded:%n%1%n%nSetup will finish without it. You can install it later from the app.
ru.EbmDownloadFailed=Не удалось скачать ExplorerBlurMica:%n%1%n%nУстановка завершится без него. Его можно будет установить позже из приложения.
en.EbmInstallFailed=ExplorerBlurMica could not be registered (code %1). You can install it later from the app.
ru.EbmInstallFailed=Не удалось зарегистрировать ExplorerBlurMica (код %1). Его можно будет установить позже из приложения.
en.UninstallRestartNote=File Explorer windows keep the effect until Explorer restarts. Sign out or restart the computer to remove it completely; the remaining files are deleted on the next restart.
ru.UninstallRestartNote=Окна Проводника сохранят эффект, пока Проводник не перезапустится. Выйдите из системы или перезагрузите компьютер, чтобы убрать его полностью; оставшиеся файлы удалятся при следующей перезагрузке.

[Tasks]
Name: "autostart"; Description: "{cm:TaskAutostart}"

[Files]
Source: "{#AppDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#CliDir}\{#CliExe}"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\LICENSE"; DestDir: "{app}"; DestName: "LICENSE.txt"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\ExplorerBlurMica Tray"; Filename: "{app}\{#AppExe}"

[Registry]
; ExplorerBlurMica is machine-wide, so the tray app starts for every account.
Root: HKLM; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "ExplorerBlurMica Tray"; \
  ValueData: """{app}\{#AppExe}"""; Flags: uninsdeletevalue; Tasks: autostart
; The app speaks the language picked in Setup (read by Strings in the app).
Root: HKLM; Subkey: "Software\ExplorerBlurMica-Tray"; ValueType: string; ValueName: "Language"; \
  ValueData: "{language}"; Flags: uninsdeletekey

[Run]
; ExplorerBlurMica hooks Explorer when the process starts, so a fresh registration needs a restart,
; not just reopened windows. Runs as the original user so the shell does not come back elevated,
; and finishes before the app starts so its tray icon lands on the new taskbar.
Filename: "{app}\{#CliExe}"; Parameters: "restart-explorer"; Description: "{cm:RunRestartExplorer}"; \
  Flags: postinstall runasoriginaluser runhidden waituntilterminated skipifsilent; Check: EbmWasInstalled
Filename: "{app}\{#AppExe}"; Description: "{cm:RunApp}"; Flags: postinstall runasoriginaluser nowait skipifsilent

[UninstallRun]
Filename: "{sys}\taskkill.exe"; Parameters: "/F /IM {#AppExe}"; Flags: runhidden waituntilterminated; RunOnceId: "StopApp"
Filename: "{app}\{#CliExe}"; Parameters: "--ebmt-elevated uninstall"; Flags: runhidden waituntilterminated; RunOnceId: "UnregisterEbm"

[Code]
var
  DownloadPage: TDownloadWizardPage;
  EbmZipReady: Boolean;
  EbmInstalled: Boolean;

procedure InitializeWizard;
begin
  DownloadPage := CreateDownloadPage(SetupMessage(msgWizardPreparing), SetupMessage(msgPreparingDesc), nil);
  DownloadPage.ShowBaseNameInsteadOfUrl := True;
end;

// Downloads ExplorerBlurMica before installing; the app does nothing without it. A failed
// download (no network) still installs the app, which can fetch ExplorerBlurMica later.
function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  if CurPageID <> wpReady then
    Exit;

  DownloadPage.Clear;
  DownloadPage.Add('{#EbmUrl}', 'Release_x64.zip', '{#EbmSha256}');
  DownloadPage.Show;
  try
    try
      DownloadPage.Download;
      EbmZipReady := True;
    except
      if DownloadPage.AbortedByUser then
        Result := False
      else
        SuppressibleMsgBox(FmtMessage(CustomMessage('EbmDownloadFailed'), [GetExceptionMessage]), mbError, MB_OK, IDOK);
    end;
  finally
    DownloadPage.Hide;
  end;
end;

// Registers ExplorerBlurMica with the app's elevated command, which checks the archive again.
procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
begin
  if (CurStep <> ssPostInstall) or not EbmZipReady then
    Exit;

  WizardForm.StatusLabel.Caption := CustomMessage('StatusRegistering');
  if Exec(ExpandConstant('{app}\{#CliExe}'),
      ExpandConstant('--ebmt-elevated install --zip "{tmp}\Release_x64.zip" --sha256 {#EbmSha256} --version {#EbmVersion}'),
      '', SW_HIDE, ewWaitUntilTerminated, ResultCode) and (ResultCode = 0) then
    EbmInstalled := True
  else
    SuppressibleMsgBox(FmtMessage(CustomMessage('EbmInstallFailed'), [IntToStr(ResultCode)]), mbError, MB_OK, IDOK);
end;

function EbmWasInstalled: Boolean;
begin
  Result := EbmInstalled;
end;

// Deletes a folder tree; anything Explorer still holds open is deleted on the next restart.
// Returns True when something was left for the restart.
function DeleteOrScheduleTree(const Path: String): Boolean;
var
  FindRec: TFindRec;
  Full: String;
begin
  Result := False;
  if FindFirst(Path + '\*', FindRec) then
  begin
    try
      repeat
        if (FindRec.Name <> '.') and (FindRec.Name <> '..') then
        begin
          Full := Path + '\' + FindRec.Name;
          if (FindRec.Attributes and FILE_ATTRIBUTE_DIRECTORY) <> 0 then
          begin
            if DeleteOrScheduleTree(Full) then
              Result := True;
          end
          else if not DeleteFile(Full) then
          begin
            RestartReplace(Full, '');
            Result := True;
          end;
        end;
      until not FindNext(FindRec);
    finally
      FindClose(FindRec);
    end;
  end;
  if DirExists(Path) and not RemoveDir(Path) then
  begin
    RestartReplace(Path, '');
    Result := True;
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep <> usPostUninstall then
    Exit;
  // [UninstallRun] has already unregistered ExplorerBlurMica; its files are not Setup's own.
  if DeleteOrScheduleTree(ExpandConstant('{app}')) and not UninstallSilent then
    MsgBox(CustomMessage('UninstallRestartNote'), mbInformation, MB_OK);
end;
