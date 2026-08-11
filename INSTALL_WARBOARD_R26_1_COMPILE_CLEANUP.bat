@echo off
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0INSTALL_WARBOARD_R26_1_COMPILE_CLEANUP.ps1"
if errorlevel 1 pause
