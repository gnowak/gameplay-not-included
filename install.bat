@echo off
:: Launcher for Gameplay Not Included Installer
title Gameplay Not Included Mod Installer

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0install.ps1"

echo.
echo Press any key to exit...
pause >nul
