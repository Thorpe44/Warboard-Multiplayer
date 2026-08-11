@echo off
setlocal
cd /d "%~dp0"
echo.
echo Starting Warboard R2.2 UI + Necron model fix...
echo.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0INSTALL_WARBOARD_R2_2_UI_NECRON_FIX.ps1"
if errorlevel 1 (
    echo.
    echo R2.2 installer reported an error.
    pause
    exit /b 1
)
endlocal
