
echo Deleting binary folder > BuildInstaller.log
rmdir /s /q ..\bin

echo Updating language resources from transifex >> BuildInstaller.log
cd ..\Tools\Transifex\
call tx_2_ToCache.bat
call tx_4_Pull_Translations.bat
call tx_5_FromCache.bat

cd ..\..\Build
echo Rebuilding Server >> BuildInstaller.log
call MSBUILD_Build_Report_Release_Server.bat

echo Rebuilding Client >> BuildInstaller.log
call MSBUILD_Build_Report_Release_Client.bat

echo Rebuilding Setup >> BuildInstaller.log
call MSBUILD_Rebuild_Release_Setup.bat
