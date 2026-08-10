@echo off
setlocal
cd /d "%~dp0"
echo ============================================================
echo WARBOARD - SHARED MULTIPLAYER DICE V2
echo ============================================================
echo.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0INSTALL_SHARED_MULTIPLAYER_DICE_V2.ps1"
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
