@ECHO OFF


"%WINDIR%\Microsoft.NET\Framework\v3.5\MSBUILD.exe" ..\MediaPortal.Tools\BuildReport\BuildReport.sln /target:Rebuild  /property:Configuration=Release


xcopy /Y ..\MediaPortal.Tools\BuildReport\css\*.* .\css\
xcopy /Y ..\MediaPortal.Tools\BuildReport\images\*.* .\images\

"%WINDIR%\Microsoft.NET\Framework\v3.5\MSBUILD.exe" ..\MediaPortal-VS2008.sln /target:Rebuild  /property:Configuration=Release;Platform=x86 > MSBUILD_Build_Report_Release_x86.txt

..\MediaPortal.Tools\BuildReport\bin\Release\BuildReport.exe /Input=MSBUILD_Build_Report_Release_x86.txt /Output=MSBUILD_Build_Report_Release_x86.html /Solution=MediaPortal-VS2008.sln /Title="MediaPortal II - Build Report"