@echo off
setlocal EnableExtensions

set "SCRIPT_DIR=%~dp0"
set "EXE_PATH=%SCRIPT_DIR%UrlPrompt.exe"

if not exist "%EXE_PATH%" (
  echo [ERROR] Not found: "%EXE_PATH%"
  echo Put install.bat in the same folder as UrlPrompt.exe and run again.
  exit /b 1
)

for %%I in ("%SCRIPT_DIR%") do set "APP_DIR=%%~fI"
if not "%APP_DIR:~-1%"=="\" set "APP_DIR=%APP_DIR%\"

set "KEY_BASE=HKCU\Software\UrlPrompt"
set "KEY_CLASSES_HTTP=HKCU\Software\Classes\UrlPrompt.http"
set "KEY_CLASSES_HTTPS=HKCU\Software\Classes\UrlPrompt.https"
set "KEY_APPPATHS=HKCU\Software\Microsoft\Windows\CurrentVersion\App Paths\UrlPrompt.exe"
set "KEY_REGAPPS=HKCU\Software\RegisteredApplications"

reg add "%KEY_REGAPPS%" /v UrlPrompt /t REG_SZ /d "Software\UrlPrompt\Capabilities" /f >nul

reg add "%KEY_BASE%\Capabilities" /v ApplicationName /t REG_SZ /d "UrlPrompt" /f >nul
reg add "%KEY_BASE%\Capabilities" /v ApplicationDescription /t REG_SZ /d "Prompt before opening http/https links and then forward to a real browser." /f >nul
reg add "%KEY_BASE%\Capabilities\UrlAssociations" /v http /t REG_SZ /d "UrlPrompt.http" /f >nul
reg add "%KEY_BASE%\Capabilities\UrlAssociations" /v https /t REG_SZ /d "UrlPrompt.https" /f >nul

reg add "%KEY_CLASSES_HTTP%" /ve /t REG_SZ /d "UrlPrompt HTTP" /f >nul
reg add "%KEY_CLASSES_HTTP%" /v "URL Protocol" /t REG_SZ /d "" /f >nul
reg add "%KEY_CLASSES_HTTP%\DefaultIcon" /ve /t REG_SZ /d "\"%EXE_PATH%\",0" /f >nul
reg add "%KEY_CLASSES_HTTP%\shell" /ve /t REG_SZ /d "open" /f >nul
reg add "%KEY_CLASSES_HTTP%\shell\open" /ve /t REG_SZ /d "Open" /f >nul
reg add "%KEY_CLASSES_HTTP%\shell\open\command" /ve /t REG_SZ /d "\"%EXE_PATH%\" \"%%1\"" /f >nul

reg add "%KEY_CLASSES_HTTPS%" /ve /t REG_SZ /d "UrlPrompt HTTPS" /f >nul
reg add "%KEY_CLASSES_HTTPS%" /v "URL Protocol" /t REG_SZ /d "" /f >nul
reg add "%KEY_CLASSES_HTTPS%\DefaultIcon" /ve /t REG_SZ /d "\"%EXE_PATH%\",0" /f >nul
reg add "%KEY_CLASSES_HTTPS%\shell" /ve /t REG_SZ /d "open" /f >nul
reg add "%KEY_CLASSES_HTTPS%\shell\open" /ve /t REG_SZ /d "Open" /f >nul
reg add "%KEY_CLASSES_HTTPS%\shell\open\command" /ve /t REG_SZ /d "\"%EXE_PATH%\" \"%%1\"" /f >nul

reg add "%KEY_APPPATHS%" /ve /t REG_SZ /d "%EXE_PATH%" /f >nul
reg add "%KEY_APPPATHS%" /v Path /t REG_SZ /d "%APP_DIR%" /f >nul

echo [OK] UrlPrompt protocol registration completed.
echo EXE: %EXE_PATH%
exit /b 0
