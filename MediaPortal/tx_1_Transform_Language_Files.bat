@echo off

echo.
echo - find all template language files. These are:
echo      - English language files:   strings_en.xml
echo      - in each of dozens language directories:   Language\strings_en.xml
echo      - the language dir is in a sudirectory, i.e.:   Incubator\BDHandler
echo                                                      Incubator\SlimTvClient
echo.
echo - transform all those files using
echo      - the xslt: Transform-MP2 to Android.xslt
echo      - replace the intput file
echo.

pause