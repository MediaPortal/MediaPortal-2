
echo Deleting binary folder > BuildInstaller.log
rmdir /s /q ..\bin

echo Updating language resources from transifex >> BuildInstaller.log
call TRANSIFEX_Update_Translation_Files.bat

echo Rebuilding Server >> BuildInstaller.log
call MSBUILD_Build_Report_Release_Server.bat

echo Rebuilding Client >> BuildInstaller.log
call MSBUILD_Build_Report_Release_Client.bat

echo Rebuilding ServiceMonitor >> BuildInstaller.log
call MSBUILD_Build_Report_Release_ServiceMonitor.bat

echo Rebuilding Setup >> BuildInstaller.log
call MSBUILD_Rebuild_Release_Setup.bat
