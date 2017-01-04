call MSBUILD_Rebuild_Base.bat Build_Report_Release_Client "/property:OneStepOnly=true;BuildClient=true;BuildOnlineVideos=true;Configuration=Release"
rem Tools version 4 is not able to build the VS2013 C++ project :-(
rem "%WINDIR%\Microsoft.NET\Framework\v4.0.30319\MSBUILD.exe" /m Build.proj %logger% /property:OneStepOnly=true;BuildClient=true
"C:\Program Files (x86)\MSBuild\12.0\bin\MSBUILD.exe" /m Build.proj %logger% /property:OneStepOnly=true;BuildClient=true
