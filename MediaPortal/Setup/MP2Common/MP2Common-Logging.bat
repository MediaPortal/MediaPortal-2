@echo off
REM =========================================================================
REM This file starts the MP2Common.msi and logs all messages to MP2Common.log
REM =========================================================================

echo.
echo MP2Common.msi started.
echo Logging to MP2Common.log...
REM For verbose logging uncomment the next line and comment the second next.
REM msiexec /i MP2Common.msi /l*v MP2Common.log
msiexec /i MP2Common.msi /l MP2Common.log
