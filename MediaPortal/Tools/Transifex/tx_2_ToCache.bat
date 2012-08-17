
@"%WINDIR%\Microsoft.NET\Framework\v4.0.30319\MSBUILD.exe" /target:Rebuild /property:Configuration=Release;Platform=x86 ..\TransifexHelper\TransifexHelper.sln > ToCache.log
..\TransifexHelper\bin\x86\Release\TransifexHelper.exe --ToCache -t "%cd%\..\.." >> ToCache.log
