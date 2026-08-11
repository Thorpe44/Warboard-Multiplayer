@echo off
setlocal
cd /d "%~dp0"
echo Starting Warboard R28.1 unified model resolver install...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0INSTALL_WARBOARD_R28_1_UNIFIED_MODEL_RESOLVER.ps1"
set EXITCODE=%ERRORLEVEL%
echo.
if not "%EXITCODE%"=="0" (
    echo R28.1 reported an error. Installer files were kept for inspection.
) else (
    echo R28.1 finished successfully.
)
pause
exit /b %EXITCODE%
