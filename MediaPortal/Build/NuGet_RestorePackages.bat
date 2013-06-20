
@"%WINDIR%\Microsoft.NET\Framework\v4.0.30319\MSBUILD.exe" /target:Rebuild /property:Configuration=Release ..\Tools\NuGetHelper\NuGetHelper.sln > NuGetHelper.log
..\Tools\NuGetHelper\bin\Release\NuGetHelper.exe >> NuGetHelper.log
