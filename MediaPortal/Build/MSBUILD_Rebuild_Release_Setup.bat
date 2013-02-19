
xcopy /I /Y .\BuildReport\_BuildReport_Files .\_BuildReport_Files

set xml=Build_Report_Release_Setup.xml
set html=Build_Report_Release_Setup.html

set logger=/l:XmlFileLogger,"BuildReport\MSBuild.ExtensionPack.Loggers.dll";logfile=%xml%
"%WINDIR%\Microsoft.NET\Framework\v4.0.30319\MSBUILD.exe" ..\Setup\MP2-Setup.sln %logger% /target:Rebuild  /property:Configuration=Release;Platform=x86

BuildReport\msxsl %xml% _BuildReport_Files\BuildReport.xslt -o %html%
