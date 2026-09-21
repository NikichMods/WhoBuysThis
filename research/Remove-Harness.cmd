@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Build-And-Install.ps1" -Uninstall
pause
