@echo off
@"%WINDIR%\Microsoft.NET\Framework\v4.0.30319\MSBUILD.exe" /target:Rebuild /property:Configuration=Release;Platform=x86 ..\TransifexHelper\TransifexHelper.sln > verify.log
..\TransifexHelper\bin\x86\Release\TransifexHelper.exe --FromCache -t "%cd%\..\.." >> push.log
