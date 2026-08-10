@echo off
setlocal
cd /d "%~dp0"
echo ============================================================
echo WARBOARD - DICE VISIBILITY + TRAY POLISH
echo ============================================================
echo.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0INSTALL_DICE_TRAY_POLISH.ps1"
set ERR=%ERRORLEVEL%
echo.
if not "%ERR%"=="0" (
    echo INSTALL FAILED - error code %ERR%
    echo Send ChatGPT a screenshot of this window.
) else (
    echo INSTALL COMPLETE.
)
echo.
pause
exit /b %ERR%
