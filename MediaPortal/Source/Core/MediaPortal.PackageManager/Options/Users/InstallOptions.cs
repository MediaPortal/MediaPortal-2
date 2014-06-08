using CommandLine;

namespace MediaPortal.PackageManager.Options.Users
{
  internal class InstallOptions : PackageOptions
  {
    [Option('v', "version", HelpText = "")]
    public string PackageVersion { get; set; }

    [Option('d', "deps", HelpText = "")]
    public bool AutomaticDependencyHandling { get; set; }
  }
}