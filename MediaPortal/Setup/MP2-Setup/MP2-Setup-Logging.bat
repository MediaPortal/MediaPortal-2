@echo off
REM =========================================================================
REM This file starts the MP2-Setup.msi and logs all messages to MP2-Setup.log
REM =========================================================================

echo.
echo MP2-Setup.msi started.
echo Logging to MP2-Setup.log...
REM For verbose logging uncomment the next line and comment the second next.
REM msiexec /i MP2-Setup.msi /l*v MP2-Setup.log
msiexec /i MP2-Setup.msi /l MP2-Setup.log
