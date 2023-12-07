using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Webradio.Stations.Helper;

internal class Standby
{
  private static IntPtr _currentPowerRequest;

  [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
  private struct PowerRequestContext
  {
    public uint Version;
    public uint Flags;
    [MarshalAs(UnmanagedType.LPWStr)] public string SimpleReasonString;
  }

  private enum PowerRequestType
  {
    PowerRequestDisplayRequired = 0, // Not to be used by drivers
    PowerRequestSystemRequired,
    PowerRequestAwayModeRequired, // Not to be used by drivers
    PowerRequestExecutionRequired // Not to be used by drivers
  }

  #region const

  private const int PowerRequestContextVersion = 0;
  private const int PowerRequestContextSimpleString = 0x1;

  #endregion

  #region DllImport

  [DllImport("kernel32.dll", SetLastError = true)]
  private static extern IntPtr PowerCreateRequest(ref PowerRequestContext context);

  [DllImport("kernel32.dll", SetLastError = true)]
  private static extern bool PowerSetRequest(IntPtr powerRequestHandle, PowerRequestType requestType);

  [DllImport("kernel32.dll", SetLastError = true)]
  private static extern bool PowerClearRequest(IntPtr powerRequestHandle, PowerRequestType requestType);

  #endregion

  #region public functions

  public static void Suppress()
  {
    // Clear current power request if there is any.
    if (_currentPowerRequest != IntPtr.Zero)
    {
      PowerClearRequest(_currentPowerRequest, PowerRequestType.PowerRequestSystemRequired);
      _currentPowerRequest = IntPtr.Zero;
    }

    // Create new power request.
    PowerRequestContext pContext;
    pContext.Flags = PowerRequestContextSimpleString;
    pContext.Version = PowerRequestContextVersion;
    pContext.SimpleReasonString = "Standby suppressed by PowerAvailabilityRequests.exe";

    _currentPowerRequest = PowerCreateRequest(ref pContext);

    if (_currentPowerRequest == IntPtr.Zero)
    {
      // Failed to create power request.
      var error = Marshal.GetLastWin32Error();

      if (error != 0)
        throw new Win32Exception(error);
    }

    var success = PowerSetRequest(_currentPowerRequest, PowerRequestType.PowerRequestSystemRequired);

    if (!success)
    {
      // Failed to set power request.
      _currentPowerRequest = IntPtr.Zero;
      var error = Marshal.GetLastWin32Error();

      if (error != 0)
        throw new Win32Exception(error);
    }
  }

  public static void Enable()
  {
    // Only try to clear power request if any power request is set.
    if (_currentPowerRequest != IntPtr.Zero)
    {
      var success = PowerClearRequest(_currentPowerRequest, PowerRequestType.PowerRequestSystemRequired);

      if (!success)
      {
        // Failed to clear power request.
        _currentPowerRequest = IntPtr.Zero;
        var error = Marshal.GetLastWin32Error();

        if (error != 0)
          throw new Win32Exception(error);
      }
      else
      {
        _currentPowerRequest = IntPtr.Zero;
      }
    }
  }

  #endregion
}
