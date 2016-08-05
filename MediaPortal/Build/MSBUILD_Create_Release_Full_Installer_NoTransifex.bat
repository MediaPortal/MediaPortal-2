REM This file can be used to prevent downloading latest translations from Transifex.
REM Transifex also does not allow anonymous translation download currently.
REM   see: https://github.com/transifex/transifex/issues/1
@"%WINDIR%\Microsoft.NET\Framework\v4.0.30319\MSBUILD.exe" RestorePackages.targets /target:RestoreBuildPackages

set PathToBuildReport=.\..\Packages\BuildReport.1.0.0
xcopy /I /Y %PathToBuildReport%\_BuildReport_Files .\_BuildReport_Files

set xml=Build_Report_MediaPortal_2.xml
set html=Build_Report_MediaPortal_2.html

set logger=/l:XmlFileLogger,"%PathToBuildReport%\MSBuild.ExtensionPack.Loggers.dll";logfile=%xml%
"%WINDIR%\Microsoft.NET\Framework\v4.0.30319\MSBUILD.exe" /m Build.proj %logger% /p:DownloadTranslations=false;Configuration=Release

%PathToBuildReport%\msxsl %xml% _BuildReport_Files\BuildReport.xslt -o %html%
