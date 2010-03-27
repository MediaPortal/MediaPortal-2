@ECHO OFF


"%WINDIR%\Microsoft.NET\Framework\v3.5\MSBUILD.exe" ..\Tools\BuildReport\BuildReport.sln /target:Rebuild  /property:Configuration=Release


xcopy /Y ..\Tools\BuildReport\css\*.* .\css\
xcopy /Y ..\Tools\BuildReport\images\*.* .\images\

"%WINDIR%\Microsoft.NET\Framework\v3.5\MSBUILD.exe" ..\Source\MP2-Client.sln /target:Rebuild  /property:Configuration=Release;Platform=x86 > MSBUILD_Build_Report_Release_x86.txt

..\Tools\BuildReport\bin\Release\BuildReport.exe /Input=MSBUILD_Build_Report_Release_x86.txt /Output=MSBUILD_Build_Report_Release_x86.html /Solution=MP2-Client.sln /Title="MediaPortal 2 - Build Report"