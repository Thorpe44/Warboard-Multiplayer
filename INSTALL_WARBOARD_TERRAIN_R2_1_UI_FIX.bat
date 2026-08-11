@echo off
setlocal
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0INSTALL_WARBOARD_TERRAIN_R2_1_UI_FIX.ps1"
if errorlevel 1 (
  echo.
  echo R2.1 fix reported an error.
  pause
  exit /b 1
)
endlocal
