REM %1 = Solution Directory
REM %2 = $(ConfigurationName) Debug/Release

REM Check for Microsoft Antispyware .BAT bug
if exist .\kernel32.dll exit 1

echo %2
del /Q models\*.* 
del /Q skin\default\Scripts\*.* 
del /Q skin\default\Scripts\Precompiled\*.* 
xcopy %1\MediaPortal.Base\*.* . /E /Y /D
copy %1\vmr9Helper\release\vmr9helper.dll .
