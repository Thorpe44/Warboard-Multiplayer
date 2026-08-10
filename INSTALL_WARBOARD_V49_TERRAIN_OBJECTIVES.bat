@echo off
setlocal
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0INSTALL_WARBOARD_V49_TERRAIN_OBJECTIVES.ps1"
set EXITCODE=%ERRORLEVEL%
if not "%EXITCODE%"=="0" (
  echo.
  echo Installer failed with code %EXITCODE%.
  pause
  exit /b %EXITCODE%
)
echo.
echo V49 install complete.
pause
