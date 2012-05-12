@echo off

rem initialize the tx folder - this is a once off, ALREADY DONE
rem tx init

rem set project file format to ANDROID - once off again??
rem  this is raising an error currently, it's a know problem and a fix release is planned
rem tx set --type ANDROID

rem this needs to be done with new files
rem   later  --type ANDROID should be added but there is a known issue with tx.exe
rem as a workaround manually add       type = ANDROID      to the resources in tx.config
rem tx set --execute --auto-local --source-lang en -r MP2.Incubator_BDHandler "Incubator\BDHandler\Language\strings_<lang>.xml"
rem tx set --execute --auto-local --source-lang en -r MP2.Incubator_SlimTvClient "Incubator\SlimTvClient\Language\strings_<lang>.xml"


rem todo: work out a workflow what happens when files are moved, folders renamed
rem       to prevent errors, there should at first be a script to validate the project against the folder structure


rem pushing the templates to Transifex
rem tx push -s


rem pulling translations from Transifex
tx pull -f -a

pause