
xcopy /I /Y .\BuildReport\_BuildReport_Files .\_BuildReport_Files

set xml=Build_Report_Release_Server.xml
set html=Build_Report_Release_Server.html

set logger=/l:XmlFileLogger,"BuildReport\MSBuild.ExtensionPack.Loggers.dll";logfile=%xml%
"%WINDIR%\Microsoft.NET\Framework\v4.0.30319\MSBUILD.exe" Build.proj %logger% /property:OneStepOnly=true;BuildServer=true

BuildReport\msxsl %xml% _BuildReport_Files\BuildReport.xslt -o %html%
