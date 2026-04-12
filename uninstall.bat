@echo off
setlocal EnableExtensions

reg delete "HKCU\Software\RegisteredApplications" /v UrlPrompt /f >nul 2>nul
reg delete "HKCU\Software\UrlPrompt" /f >nul 2>nul
reg delete "HKCU\Software\Classes\UrlPrompt.http" /f >nul 2>nul
reg delete "HKCU\Software\Classes\UrlPrompt.https" /f >nul 2>nul
reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\App Paths\UrlPrompt.exe" /f >nul 2>nul

echo [OK] UrlPrompt registry entries removed (HKCU).
exit /b 0
