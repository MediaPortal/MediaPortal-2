@ECHO OFF

rem "%WINDIR%\Microsoft.NET\Framework\v4.0.30319\MSBUILD.exe" ..\Tools\MergeMSI\MergeMSI.sln /target:Rebuild  /property:Configuration=Release > Build_Setup.log


"%WINDIR%\Microsoft.NET\Framework\v4.0.30319\MSBUILD.exe" ..\Setup\MP2-Setup.sln /target:Rebuild  /property:Configuration=Release;Platform=x86 > Build_Setup.log


rem ..\Tools\MergeMSI\bin\Release\MergeMSI.exe --TargetDir=..\Setup\MP2-Setup\bin\Release\ --TargetFileName=MP2-Setup.msi >> Build_Setup.log