
echo Deleting binary folder
rmdir /s /q ..\bin

echo Restore NuGet packages
call NuGet_RestorePackages.bat

echo Updating language resources from transifex
call TRANSIFEX_Update_Translation_Files.bat

echo Rebuilding Server
call MSBUILD_Rebuild_Release_Server.bat

echo Rebuilding Client
call MSBUILD_Rebuild_Release_Client.bat

echo Rebuilding ServiceMonitor
call MSBUILD_Rebuild_Release_ServiceMonitor.bat

echo Rebuilding Setup
call MSBUILD_Rebuild_Release_Setup.bat
