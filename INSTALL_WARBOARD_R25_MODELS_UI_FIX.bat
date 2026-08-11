@echo off
setlocal
cd /d "%~dp0"
echo.
echo Starting Warboard R25 model/UI fix...
echo.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0INSTALL_WARBOARD_R25_MODELS_UI_FIX.ps1"
if errorlevel 1 (
    echo.
    echo R25 installer reported an error.
    pause
    exit /b 1
)
endlocal
