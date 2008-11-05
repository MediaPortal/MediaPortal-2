REM %1 = Solution directory
REM %2 = Target directory

echo Solution directory = %1, target directory = %2
xcopy %1\MediaPortal.Base\*.* %2 /E /Y /D
copy %1\vmr9Helper\release\vmr9helper.dll %2
