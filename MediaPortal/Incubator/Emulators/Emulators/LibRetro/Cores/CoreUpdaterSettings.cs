using MediaPortal.Common.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.LibRetro.Cores
{
  public class CoreUpdaterSettings
  {
    //These cores all use fibers via the libco library.
    //Fibers make .net fail hard!
    public static readonly string[] DEFAULT_UNSUPPORTED =
    {
      "mupen64plus_libretro.dll",
      "bnes_libretro.dll",
      "bsnes_accuracy_libretro.dll",
      "bsnes_balanced_libretro.dll",
      "bsnes_cplusplus98_libretro.dll",
      "bsnes_mercury_accuracy_libretro.dll",
      "bsnes_mercury_balanced_libretro.dll",
      "bsnes_mercury_performance_libretro.dll",
      "bsnes_performance_libretro.dll",
      "mednafen_snes_libretro.dll",
      "dosbox_libretro.dll",
      "mame2000_libretro.dll",
      "mame2010_libretro.dll",
      "mame2014_libretro.dll",
      "mess2014_libretro.dll",
      "ume2014_libretro.dll",
      "gpsp_libretro.dll"
    };

    public static readonly CustomCore[] DEFAULT_CUSTOM_CORES =
    {
      //Custom build of mupen64plus without libco
      new CustomCore
      {
        CoreName = "mupen64plus_singlethread_libretro.dll",
        CoreUrl = "https://github.com/brownard/mupen64plus-libretro/releases/download/v0.1-single_thread/mupen64plus_singlethread_libretro.dll",
        InfoUrl = "https://github.com/brownard/mupen64plus-libretro/releases/download/v0.1-single_thread/mupen64plus_singlethread_libretro.info"
      }
    };

    [Setting(SettingScope.Global, "http://buildbot.libretro.com")]
    public string BaseUrl { get; set; }

    [Setting(SettingScope.Global, "/nightly/windows/x86/latest/")]
    public string CoresUrl { get; set; }

    [Setting(SettingScope.Global, "/assets/frontend/info/")]
    public string CoreInfoUrl { get; set; }

    [Setting(SettingScope.Global, "http://brownard.github.io/Libretro/CustomCoresList.xml")]
    public string CustomCoresUrl { get; set; }
  }
}