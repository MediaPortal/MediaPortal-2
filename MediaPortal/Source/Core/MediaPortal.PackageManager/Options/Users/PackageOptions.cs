using CommandLine;
using MediaPortal.Common.PluginManager.Packages.DataContracts.Enumerations;

namespace MediaPortal.PackageManager.Options.Users
{
  internal class PackageOptions : SharedOptions
  {
    [Option('t', "type", HelpText = "")]
    public PackageType PackageType { get; set; }

    [Option('n', "name", HelpText = "")]
    public string PackageName { get; set; }

    [Option('f', "force", Required = false, HelpText = "Just do it<tm>")]
    public bool SkipValidation { get; set; }
  }
}