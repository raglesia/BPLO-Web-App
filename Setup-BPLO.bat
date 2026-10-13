@echo off
setlocal
cd /d "%~dp0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0operations\setup\Setup-BPLO.ps1" %*
set "setupResult=%errorlevel%"
echo.
if not "%setupResult%"=="0" echo Setup stopped. Read the message above before retrying.
pause
exit /b %setupResult%
