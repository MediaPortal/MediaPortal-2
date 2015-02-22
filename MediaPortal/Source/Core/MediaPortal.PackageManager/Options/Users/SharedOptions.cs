using System;
using System.Collections.Generic;
using CommandLine;

namespace MediaPortal.PackageManager.Options.Users
{
  internal class SharedOptions
  {
    private Dictionary<string, string> _installPaths;

    [OptionArray('p', "Paths")]
    public string[] InstallPaths { get; set; }

    public IDictionary<string, string> GetInstallPaths()
    {
      if (_installPaths == null)
      {
        _installPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in InstallPaths)
        {
          int n = path.IndexOf(':');
          if (n > 0)
          {
            _installPaths.Add(path.Substring(0, n).Trim(), path.Substring(n + 1).Trim());
          }
        }
      }
      return _installPaths;
    }

    [Option("elevated", DefaultValue = false, Required = false, HelpText = "Only for internal use!")]
    public bool IsElevated { get; set; }
  }
}