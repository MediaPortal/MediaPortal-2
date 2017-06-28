@echo off
REM =========================================================================
REM This file starts the MP2Client.msi and logs all messages to MP2Client.log
REM =========================================================================

echo.
echo MP2Client.msi started.
echo Logging to MP2Client.log...
REM For verbose logging uncomment the next line and comment the second next.
REM msiexec /i MP2-Setup.msi /l*v MP2-Setup.log
msiexec /i MP2Client.msi /l MP2Client.log
