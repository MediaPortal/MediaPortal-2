@ECHO OFF

IF EXIST ..\MediaPortal.Tools\BuildReport\bin\Release\BuildReport.exe GOTO BUILT

"%ProgramFiles%\Microsoft Visual Studio 8\Common7\IDE\devenv.com" /rebuild Release ..\MediaPortal.Tools\BuildReport\BuildReport.sln

:BUILT

xcopy /Y ..\MediaPortal.Tools\BuildReport\css\*.* .\css\
xcopy /Y ..\MediaPortal.Tools\BuildReport\images\*.* .\images\

"%ProgramFiles%\Microsoft Visual Studio 8\Common7\IDE\devenv.com" /rebuild Release /projectconfig x86 ..\MediaPortal.sln > VS2005_Build_Report_Release_x86.txt

..\MediaPortal.Tools\BuildReport\bin\Release\BuildReport.exe /Input=VS2005_Build_Report_Release_x86.txt /Output=VS2005_Build_Report_Release_x86.html /Solution=MediaPortal.sln /Title="MediaPortal II - Build Report"