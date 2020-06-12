using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Common.Emulators
{
  public class EmulatorConfiguration
  {
    public const string WILDCARD_GAME_PATH = "$game$";
    public const string WILDCARD_GAME_PATH_NO_EXT = "$gamenoext$";
    public const string WILDCARD_GAME_DIRECTORY = "$gamedir$";

    public EmulatorConfiguration()
    {
      Platforms = new HashSet<string>();
      FileExtensions = new HashSet<string>();
      UseQuotes = true;
    }

    public Guid Id { get; set; }
    public string LocalSystemId { get; set; }
    public string Name { get; set; }
    public HashSet<string> Platforms { get; set; }
    public HashSet<string> FileExtensions { get; set; }
    public string Path { get; set; }
    public string WorkingDirectory { get; set; }
    public string Arguments { get; set; }
    public bool UseQuotes { get; set; }
    public bool IsNative { get; set; }
    public bool IsLibRetro { get; set; }
    public bool ExitsOnEscapeKey { get; set; }
  }
}