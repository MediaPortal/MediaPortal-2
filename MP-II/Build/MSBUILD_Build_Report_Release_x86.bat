@ECHO OFF

"%WINDIR%\Microsoft.NET\Framework\v2.0.50727\MSBUILD.exe" ..\MediaPortal.sln /target:Rebuild  /property:Configuration=Release;Platform=x86 > MSBUILD_Build_Report_Release_x86.txt

..\MediaPortal.Tools\BuildReport\bin\Release\BuildReport.exe /Input=MSBUILD_Build_Report_Release_x86.txt /Output=MSBUILD_Build_Report_Release_x86.html /Solution=MediaPortal.sln /Title="MediaPortal II - Build Report"