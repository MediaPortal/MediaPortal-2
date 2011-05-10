@ECHO OFF

"%WINDIR%\Microsoft.NET\Framework\v4.0.30319\MSBUILD.exe" ..\Tools\MergeMSI\MergeMSI.sln /target:Rebuild  /property:Configuration=Release

"%WINDIR%\Microsoft.NET\Framework\v4.0.30319\MSBUILD.exe" ..\Setup\MP2-Setup.sln /target:Rebuild  /property:Configuration=Release;Platform=x86 >> build_setup.log

..\Tools\MergeMSI\bin\Release\MergeMSI.exe --TargetFileName="..\Setup\MP2-Setup\bin\Release\" --TargetDir="MP2-Setup.msi" >> build_setup.log