@echo off
setlocal EnableExtensions

reg delete "HKCU\Software\RegisteredApplications" /v UrlRouter /f >nul 2>nul
reg delete "HKCU\Software\UrlRouter" /f >nul 2>nul
reg delete "HKCU\Software\Classes\UrlRouter.http" /f >nul 2>nul
reg delete "HKCU\Software\Classes\UrlRouter.https" /f >nul 2>nul
reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\App Paths\UrlRouter.exe" /f >nul 2>nul

echo [OK] URL Router registry entries removed (HKCU).
exit /b 0
