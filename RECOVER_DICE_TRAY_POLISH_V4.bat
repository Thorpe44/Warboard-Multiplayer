@echo off
setlocal
cd /d "%~dp0"
echo ============================================================
echo WARBOARD - DICE POLISH RECOVERY V4
echo ============================================================
echo.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0RECOVER_DICE_TRAY_POLISH_V4.ps1"
set ERR=%ERRORLEVEL%
echo.
if not "%ERR%"=="0" (
    echo RECOVERY FAILED - error code %ERR%
    echo Send ChatGPT a screenshot of this window.
) else (
    echo RECOVERY COMPLETE.
)
echo.
pause
exit /b %ERR%
