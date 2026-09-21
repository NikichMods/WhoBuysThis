@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Build-And-Install.ps1"
if errorlevel 1 (
  echo.
  echo Build/install failed. See the message above.
)
pause
