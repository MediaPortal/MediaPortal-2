
@"%WINDIR%\Microsoft.NET\Framework\v4.0.30319\MSBUILD.exe" ..\..\Build\Transifex.targets /target:DownloadTransifexClient
@"%WINDIR%\Microsoft.NET\Framework\v4.0.30319\MSBUILD.exe" ..\..\Build\Transifex.targets /target:BuildTransifexHelper
..\TransifexHelper\bin\x86\Release\TransifexHelper.exe --verify -t "%cd%\..\.." > verify.log
