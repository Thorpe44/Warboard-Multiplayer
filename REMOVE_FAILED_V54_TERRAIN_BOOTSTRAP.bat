@echo off
setlocal
cd /d "%~dp0"
echo.
echo Removing failed Warboard V54 terrain bootstrap...
echo.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0REMOVE_FAILED_V54_TERRAIN_BOOTSTRAP.ps1"
if errorlevel 1 (
    echo.
    echo Removal reported an error.
    pause
    exit /b 1
)
endlocal
