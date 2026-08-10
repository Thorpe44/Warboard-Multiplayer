@echo off
setlocal
cd /d "%~dp0"
echo ============================================================
echo WARBOARD MULTIPLAYER - UNITY 6000.5 COMPILE FIX
echo ============================================================
echo.
echo CLOSE UNITY BEFORE RUNNING THIS.
echo.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0FIX_UNITY6000_5_MULTIPLAYER.ps1"
set ERR=%ERRORLEVEL%
echo.
if not "%ERR%"=="0" (
    echo FIX FAILED - error code %ERR%
    echo Send ChatGPT a screenshot of this window.
) else (
    echo FIX COMPLETE.
    echo Re-open Unity now.
)
echo.
pause
exit /b %ERR%
