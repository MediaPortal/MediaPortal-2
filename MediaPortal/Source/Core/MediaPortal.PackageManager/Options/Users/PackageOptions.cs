using CommandLine;

namespace MediaPortal.PackageManager.Options.Users
{
  internal class PackageOptions : SharedOptions
  {
    [Option('t', "type", HelpText = "")]
    public string PackageType { get; set; }

    [Option('n', "name", HelpText = "")]
    public string PackageName { get; set; }

    [Option('f', "force", Required = false, HelpText = "Just do it<tm>")]
    public bool SkipValidation { get; set; }
  }
}