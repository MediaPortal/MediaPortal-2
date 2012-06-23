@echo off

set ProgramDir=%ProgramFiles%
if exist "%ProgramFiles(x86)%" set ProgramDir=%ProgramFiles(x86)%

"%ProgramDir%\Microsoft Visual Studio 10.0\Common7\IDE\devenv.com" /rebuild Release ..\Tools\TransifexHelper\TransifexHelper.sln > Transifex.log

..\Tools\TransifexHelper\bin\x86\Release\TransifexHelper.exe --FromCache -t "%cd%\.." >> Transifex.log