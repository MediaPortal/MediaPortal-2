@echo off

set ProgramDir=%ProgramFiles%
if exist "%ProgramFiles(x86)%" set ProgramDir=%ProgramFiles(x86)%

"%ProgramDir%\Microsoft Visual Studio 10.0\Common7\IDE\devenv.com" /rebuild Release ..\TransifexHelper\TransifexHelper.sln > verify.log

..\TransifexHelper\bin\Release\TransifexHelper_MP2.exe --verify -t "%cd%\..\.." >> verify.log
