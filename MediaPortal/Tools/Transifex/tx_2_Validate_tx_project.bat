@echo off

echo.
echo todo: create a workflow what happens when files are moved, folders renamed
echo       to prevent errors, there should at first be a script to validate the project against the folder structure
echo.
echo Result should be:
echo   error messages for strings_en.xml files
echo       - which are in tx project
echo       - but not in folder structure anymore
echo
echo   if error show messages and exit.
echo   A dev has to fix the tx project first (renaming or deleting entries),
echo     before script can be continued.
pause

echo   expecting no error in first check.
echo   Now look for files which are not in tx project but in folder structure.
echo.
echo   for those files the following code (2 examples) has to be executed:
echo      tx set --execute --auto-local --source-lang en -r MP2.Incubator_BDHandler "Incubator\BDHandler\strings_<lang>.xml"
echo      tx set --execute --auto-local --source-lang en -r MP2.Incubator_SlimTvClient "Incubator\SlimTvClient\strings_<lang>.xml"
echo.

pause