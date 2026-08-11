@echo off
setlocal
powershell -ExecutionPolicy Bypass -File "%~dp0INSTALL_WARBOARD_R26_UI_READABILITY_ORK_DATA_FIX.ps1"
exit /b %errorlevel%
