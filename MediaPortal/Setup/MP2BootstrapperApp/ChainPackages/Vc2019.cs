using System;
using System.IO;

namespace MP2BootstrapperApp.ChainPackages
{
  public class Vc2019 : IPackage
  {
    private readonly IPackageChecker _packageChecker;

    public Vc2019(IPackageChecker packageChecker)
    {
      _packageChecker = packageChecker;
    }

    public Version GetInstalledVersion()
    {
      const string mfc140Dll = "mfc140.dll";

      // Explicitly check the x86 system path for the x86 version of the dll. The installer is currently always run
      // as x86 so this is not technically necessary as the OS will automatically redirect to the x86 path, but in
      // case we change to x64 in future this will ensure we are looking in the x86 directory.
      string vc2019Path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.SystemX86), mfc140Dll);

      if (!_packageChecker.Exists(vc2019Path))
      {
        return new Version();
      }
      int majorVersion = _packageChecker.GetFileMajorVersion(vc2019Path);
      int minorVersion = _packageChecker.GetFileMinorVersion(vc2019Path);
      int buildVersion = _packageChecker.GetFileBuildVersion(vc2019Path);
      int revision = _packageChecker.GetFilePrivateVersion(vc2019Path);
      
      return new Version(majorVersion, minorVersion, buildVersion, revision);
    }
  }
}
