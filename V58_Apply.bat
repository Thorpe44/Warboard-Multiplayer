@echo off
setlocal
cd /d "%~dp0"

echo ========================================
echo        WARBOARD MULTIPLAYER V58
echo ========================================
echo.
echo This patch:
echo   - fills the V55 secondary-card summaries
echo   - stops Standard11 faction rules showing the wrong army
echo   - changes the deployment status bar into a temporary toast
echo   - bumps the visible build identity to v58
echo.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0V58_Apply.ps1"
set EXITCODE=%ERRORLEVEL%

echo.
if not "%EXITCODE%"=="0" (
    echo V58 patch FAILED.
    echo Read the error above. If a backup was created, your original files are safe there.
) else (
    echo V58 patch applied successfully.
    echo Open Unity and allow scripts to recompile.
)
echo.
pause
exit /b %EXITCODE%
