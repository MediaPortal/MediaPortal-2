@ECHO OFF

set ProgramDir=%ProgramFiles%
if exist "%ProgramFiles(x86)%" set ProgramDir=%ProgramFiles(x86)%

"%ProgramDir%\Microsoft Visual Studio 8\Common7\IDE\devenv.com" /rebuild Release ..\Tools\BuildReport\BuildReport.sln

xcopy /Y ..\Tools\BuildReport\css\*.* .\css\
xcopy /Y ..\Tools\BuildReport\images\*.* .\images\

"%ProgramDir%\Microsoft Visual Studio 8\Common7\IDE\devenv.com" ..\Source\MP-II-Client-VS2005.sln /Rebuild "Release|x86" > VS2005_Build_Report_Release_x86.txt

..\Tools\BuildReport\bin\Release\BuildReport.exe /VS2005 /Input=VS2005_Build_Report_Release_x86.txt /Output=VS2005_Build_Report_Release_x86.html /Solution=MP-II-Client-VS2005.sln /Title="MediaPortal II - Build Report"