using MediaPortal.Utilities.SystemAPI;
using System;

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
      // No longer available in netstandard library, but call is needed to set platform path
      return true;
    }
  }
}
