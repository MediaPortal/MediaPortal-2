using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FreeImageAPI;
using MediaPortal.Utilities.SystemAPI;

namespace FreeImageLib
{
  public class FreeImageInit
  {
    static FreeImageInit()
    {
      string absolutePlatformDir;
      if (!NativeMethods.SetPlatformSearchDirectories(out absolutePlatformDir))
        throw new Exception("Error adding dll probe path");
    }

    /// <summary>
    /// Helper function to check availability of FreeImage libraries. It makes sure to set platform search paths (x86/x64) in static constructor.
    /// </summary>
    /// <returns></returns>
    public static bool IsAvailable()
    {
      return FreeImage.IsAvailable();
    }
  }
}
