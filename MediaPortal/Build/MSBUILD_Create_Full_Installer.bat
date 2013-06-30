
xcopy /I /Y .\BuildReport\_BuildReport_Files .\_BuildReport_Files

set xml=Build_Report_MediaPortal_2.xml
set html=Build_Report_MediaPortal_2.html

set logger=/l:XmlFileLogger,"BuildReport\MSBuild.ExtensionPack.Loggers.dll";logfile=%xml%
"%WINDIR%\Microsoft.NET\Framework\v4.0.30319\MSBUILD.exe" Build.proj %logger%

BuildReport\msxsl %xml% _BuildReport_Files\BuildReport.xslt -o %html%
