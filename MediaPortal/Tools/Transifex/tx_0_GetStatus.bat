
@"%WINDIR%\Microsoft.NET\Framework\v4.0.30319\MSBUILD.exe" ..\..\Build\Transifex.targets /target:DownloadTransifexClient
@tx status

pause