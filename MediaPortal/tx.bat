@echo off

rem initialize the tx folder - this is a once off, ALREADY DONE
rem tx init

rem set project file format to ANDROID - once off again, ALREADY DONE
tx set -t ANDROID

rem this needs to be done with new files
rem tx set --execute --auto-local --source-lang en -r MP2.Incubator_BDHandler "Incubator\BDHandler\Language\strings_<lang>.xml"
rem tx set --execute --auto-local --source-lang en -r MP2.Incubator_SlimTvClient "Incubator\SlimTvClient\Language\strings_<lang>.xml"


rem todo: work out a workflow what happens when files are moved, folders renamed
rem       to prevent errors, there should at first be a script to validate the project against the folder structure


rem pushing the templates to the server
rem tx push -s


pause