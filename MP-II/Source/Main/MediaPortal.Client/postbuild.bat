REM %1 = Solution directory
REM %2 = Target directory

echo Solution directory = %1, target directory = %2
xcopy %1\Base\Client\*.* %2 /E /Y /D
copy %1\Ui\Players\DShowHelper\release\DShowHelper.dll %2
