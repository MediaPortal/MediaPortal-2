using CommandLine;

namespace MediaPortal.PackageManager.Options.Users
{
  internal class SharedOptions
  {
    [Option('p', "path", Required = false, HelpText = "Override auto-detection of MP2 plugin folder.")]
    public string PluginRootPath { get; set; }
  }
}