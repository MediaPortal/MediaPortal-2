using System.Reflection;
#if NET5_0_OR_GREATER
using System.Runtime.Versioning;
#endif

// This file provides common information about assembly company, copyright and version number. It should be referenced
// in any MP2 project (as link). The existing AssemblyInfo.cs files must be edited, the duplicated attributes need to 
// be deleted.
[assembly: AssemblyCompany("Team MediaPortal")]
[assembly: AssemblyProduct("MediaPortal 2")]
[assembly: AssemblyCopyright("Copyright © Team MediaPortal 2007 - 2023")]
// Note: Following two line will be modified by AssemblyInfoHelper in build process:
// The version number will have format: 2.YY.MM.{BuildNumber}
[assembly: AssemblyVersion("2.5.2305.14606")]
[assembly: AssemblyFileVersion("2.5.2305.14606")]
// Note: This line will be filled by AssemblyInfoHelper in build process.
[assembly: AssemblyInformationalVersion("origin/EXP-Net6-70a151")]

#if NET5_0_OR_GREATER
// To avoid warnings about platform specific code on multi-platform versions of .net
// an assembly can specify the supported platforms, in our case only Windows for now.
// If an assembly auto-generates its AssemblyInfo then the platform can be specified in
// the TFM, e.g. net6.0-windows. However most MP2 projects have a manually generated
// AssemblyInfo file, so that this version file can be included in the AssemblyInfo at build
// time. The platform TFM is ignored for manually generated AssemblyInfos so the attribute
// needs to be explicitly included. It is done here to keep it centralized and easily
// modified across assemblies.
[assembly: SupportedOSPlatform("windows")]
#endif
