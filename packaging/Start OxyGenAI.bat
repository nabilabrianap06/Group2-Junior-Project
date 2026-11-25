@echo off
title OxyGenAI
cd /d "%~dp0"
if not exist "oxygenai.settings.json" (
  echo First start: running setup...
  powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0setup.ps1"
  if not exist "oxygenai.settings.json" goto :fail
)
echo Starting OxyGenAI. Keep this window open while you use the app; close it to stop the app.
OxyGenAI.exe
if errorlevel 1 goto :fail
goto :eof
:fail
echo.
echo OxyGenAI stopped with an error. Read the message above; see USER_GUIDE section "Troubleshooting".
pause
