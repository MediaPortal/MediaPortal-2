REM This file can be used to prevent downloading latest translations from Transifex.
REM Transifex also does not allow anonymous translation download currently.
REM   see: https://github.com/transifex/transifex/issues/1
call MSBUILD_Rebuild_Base.bat Build_Report_MediaPortal_2 "/p:DownloadTranslations=false;Configuration=Release"