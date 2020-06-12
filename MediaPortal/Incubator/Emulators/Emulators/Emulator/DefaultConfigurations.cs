using Emulators.Common.Emulators;
using MediaPortal.Common.ResourceAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Emulators.Common.Games;

namespace Emulators.Emulator
{
  public static class DefaultConfigurations
  {
    class DefaultConfiguration : EmulatorConfiguration
    {
      public string PathRegex { get; set; }
    }

    static List<DefaultConfiguration> _configurations;
    static EmulatorConfiguration _nativeConfiguration;

    static DefaultConfigurations()
    {
      CreateDefaultConfigurations();
    }

    public static EmulatorConfiguration NativeConfiguration
    {
      get { return _nativeConfiguration; }
    }

    public static bool TryMatch(string path, out EmulatorConfiguration defaultConfiguration)
    {
      string fileName = DosPathHelper.GetFileName(path);
      defaultConfiguration = _configurations.FirstOrDefault(c => IsMatch(c, fileName));
      return defaultConfiguration != null;
    }

    static bool IsMatch(DefaultConfiguration configuration, string fileName)
    {
      if (configuration.Path != null && configuration.Path.Equals(fileName, StringComparison.InvariantCultureIgnoreCase))
        return true;
      return configuration.PathRegex != null && Regex.IsMatch(fileName, configuration.PathRegex);
    }

    static void CreateDefaultConfigurations()
    {
      _nativeConfiguration = new EmulatorConfiguration()
      {
        Name = "PC",
        Platforms = new HashSet<string> { GameInfo.PLATFORM_PC },
        FileExtensions = new HashSet<string> { ".exe", ".bat", ".lnk" }
      };

      _configurations = new List<DefaultConfiguration>
      {

      new DefaultConfiguration()
      {
        Name = "MAME",
        Path = "mame.exe",
        Platforms = new HashSet<string> { GameInfo.PLATFORM_ARCADE },
        FileExtensions = new HashSet<string> { ".zip" },
        Arguments = string.Format("-rp {0} {1}", EmulatorConfiguration.WILDCARD_GAME_DIRECTORY, EmulatorConfiguration.WILDCARD_GAME_PATH_NO_EXT)
      },

      new DefaultConfiguration()
      {
        Name = "Project 64",
        Path = "project64.exe",
        Platforms = new HashSet<string> { GameInfo.PLATFORM_NINTENDO_64 },
        UseQuotes = false,
        FileExtensions = new HashSet<string> { ".n64", ".z64", ".v64", ".rom" }
      },

      new DefaultConfiguration()
      {
        Name = "WinUAE",
        Path = "winuae.exe",
        Platforms = new HashSet<string> { GameInfo.PLATFORM_AMIGA },
        FileExtensions = new HashSet<string> { ".uae" }
      },

      new DefaultConfiguration()
      {
        Name = "Atari800",
        Path = "atari800win.exe",
        Platforms = new HashSet<string> { GameInfo.PLATFORM_ATARI_5200 },
        FileExtensions = new HashSet<string> { ".atr" }
      },

      new DefaultConfiguration()
      {
        Name = "NullDC",
        Path = "nulldc.exe",
        Platforms = new HashSet<string> { GameInfo.PLATFORM_SEGA_DREAMCAST },
        FileExtensions = new HashSet<string> { ".cdi" }
      },

      new DefaultConfiguration()
      {
        Name = "VisualBoyAdvance",
        Path = "vba.exe",
        Platforms = new HashSet<string> { GameInfo.PLATFORM_NINTENDO_GAME_BOY, GameInfo.PLATFORM_NINTENDO_GAME_BOY_ADVANCE, GameInfo.PLATFORM_NINTENDO_GAME_BOY_COLOR },
        FileExtensions = new HashSet<string> { ".gba", ".gbc", ".sgb", ".gb" }
      },

      new DefaultConfiguration()
      {
        Name = "FCE Ultra",
        Path = "fceu.exe",
        Platforms = new HashSet<string> { GameInfo.PLATFORM_NINTENDO_NES },
        FileExtensions = new HashSet<string> { ".nes" }
      },

      new DefaultConfiguration()
      {
        Name = "ePSXe",
        Path = "epsxe.exe",
        Platforms = new HashSet<string> { GameInfo.PLATFORM_SONY_PLAYSTATION },
        FileExtensions = new HashSet<string> { ".bin", ".iso", ".img" },
        Arguments = "-nogui -loadbin",
        ExitsOnEscapeKey = true
      },

      new DefaultConfiguration()
      {
        Name = "PCSX2",
        PathRegex = "pcsx2.*?\\.exe",
        Platforms = new HashSet<string> { GameInfo.PLATFORM_SONY_PLAYSTATION_2 },
        FileExtensions = new HashSet<string> { ".bin", ".iso", ".img" },
        Arguments = "--nogui"
      },

      new DefaultConfiguration()
      {
        Name = "Kega Fusion",
        Path = "fusion.exe",
        Platforms = new HashSet<string> { GameInfo.PLATFORM_SEGA_32X, GameInfo.PLATFORM_SEGA_CD, GameInfo.PLATFORM_SEGA_GENESIS, GameInfo.PLATFORM_SEGA_MASTER_SYSTEM, GameInfo.PLATFORM_SEGA_MEGA_DRIVE, GameInfo.PLATFORM_SEGA_GAME_GEAR },
        FileExtensions = new HashSet<string> { ".bin", ".smd", ".md" }
      },

      new DefaultConfiguration()
      {
        Name = "Snes9x",
        Path = "snes9xw.exe",
        Platforms = new HashSet<string> { GameInfo.PLATFORM_NINTENDO_SNES },
        FileExtensions = new HashSet<string> { ".smc", ".sfc" }
      },

      new DefaultConfiguration()
      {
        Name = "Snes9x",
        Path = "snes9x.exe",
        Platforms = new HashSet<string> { GameInfo.PLATFORM_NINTENDO_SNES },
        FileExtensions = new HashSet<string> { ".smc", ".fig", ".bin", ".sfc" }
      },

      new DefaultConfiguration()
      {
        Name = "ZSNES",
        Path = "zsnesw.exe",
        Platforms = new HashSet<string> { GameInfo.PLATFORM_NINTENDO_SNES },
        FileExtensions = new HashSet<string> { ".smc", ".fig", ".sfc" }
      },

      new DefaultConfiguration()
      {
        Name = "Dolphin",
        Path = "dolphin.exe",
        Platforms = new HashSet<string> { GameInfo.PLATFORM_NINTENDO_WII, GameInfo.PLATFORM_NINTENDO_GAMECUBE },
        FileExtensions = new HashSet<string> { ".iso", ".elf", ".dol", ".gcm", ".wbfs", ".ciso", ".gcz", ".wad" },
        Arguments = "-b -e"
      }

      };
    }
  }
}
