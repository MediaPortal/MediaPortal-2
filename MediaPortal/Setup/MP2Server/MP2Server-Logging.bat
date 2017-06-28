@echo off
REM =========================================================================
REM This file starts the MP2Server.msi and logs all messages to MP2Server.log
REM =========================================================================

echo.
echo MP2Server.msi started.
echo Logging to MP2Server.log...
REM For verbose logging uncomment the next line and comment the second next.
REM msiexec /i MP2-Server.msi /l*v MP2-Server.log
msiexec /i MP2Server.msi /l MP2Server.log
