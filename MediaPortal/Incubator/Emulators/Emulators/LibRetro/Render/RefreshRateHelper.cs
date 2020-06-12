using MediaPortal.Common;
using MediaPortal.Common.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Emulators.LibRetro.Render
{
  class RefreshRateHelper : IDisposable
  {
    #region DLL Imports
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern long GetDisplayConfigBufferSizes([In] uint flags, [Out] out uint numPathArrayElements,
                                                           [Out] out uint numModeArrayElements);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern long QueryDisplayConfig([In] uint flags, ref uint numPathArrayElements, IntPtr pathArray,
                                                  ref uint numModeArrayElements, IntPtr modeArray,
                                                  IntPtr currentTopologyId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern long SetDisplayConfig(uint numPathArrayElements, IntPtr pathArray, uint numModeArrayElements,
                                                IntPtr modeArray, uint flags);
    #endregion

    #region Consts
    protected const uint SIZE_OF_DISPLAYCONFIG_PATH_INFO = 72;
    protected const uint SIZE_OF_DISPLAYCONFIG_MODE_INFO = 64;

    protected const uint QDC_ALL_PATHS = 1;
    protected const uint DISPLAYCONFIG_MODE_INFO_TYPE_TARGET = 2;
    protected const uint DISPLAYCONFIG_SCANLINE_ORDERING_PROGRESSIVE = 1;

    protected const uint SDC_USE_SUPPLIED_DISPLAY_CONFIG = 0x00000020;
    protected const uint SDC_VALIDATE = 0x00000040;
    protected const uint SDC_APPLY = 0x00000080;
    protected const uint SDC_ALLOW_CHANGES = 0x00000400;
    #endregion

    #region Static Methods
    public static uint GetDisplayIndex(Form form)
    {
      return (uint)Array.IndexOf(Screen.AllScreens, Screen.FromControl(form));
    }

    public static double GetRefreshRate(Form form)
    {
      uint displayIndex = GetDisplayIndex(form);
      using (var helper = new RefreshRateHelper(displayIndex))
        return helper.GetRefreshRate();
    }
    #endregion

    #region Protected Members
    protected readonly uint _displayIndex;
    protected bool _initialized;
    protected int _offset;
    protected uint _numPathArrayElements;
    protected uint _numModeArrayElements;
    protected IntPtr _pPathArray = IntPtr.Zero;
    protected IntPtr _pModeArray = IntPtr.Zero;
    #endregion

    #region Ctor
    public RefreshRateHelper(uint displayIndex)
    {
      _displayIndex = displayIndex;
      InitBuffers();
    }
    #endregion

    #region Public Methods
    public double GetRefreshRate()
    {
      if (!_initialized)
        return 0;
      // Calculate refresh rate
      UInt32 numerator = (UInt32)Marshal.ReadInt32(_pModeArray, _offset + 32);
      UInt32 denominator = (UInt32)Marshal.ReadInt32(_pModeArray, _offset + 36);
      double refreshRate = numerator / (double)denominator;
      ServiceRegistration.Get<ILogger>().Debug("LibRetroRefreshRateHelper.GetRefreshRate: QueryDisplayConfig returned {0}/{1}", numerator, denominator);
      return refreshRate;
    }
    #endregion

    #region Protected Methods
    protected void InitBuffers()
    {
      // Get size of buffers for QueryDisplayConfig
      long result = GetDisplayConfigBufferSizes(QDC_ALL_PATHS, out _numPathArrayElements, out _numModeArrayElements);
      if (result != 0)
      {
        ServiceRegistration.Get<ILogger>().Error("RefreshRateChanger: GetDisplayConfigBufferSizes(...) returned {0}", result);
        return;
      }

      _pPathArray = Marshal.AllocHGlobal((Int32)(_numPathArrayElements * SIZE_OF_DISPLAYCONFIG_PATH_INFO));
      _pModeArray = Marshal.AllocHGlobal((Int32)(_numModeArrayElements * SIZE_OF_DISPLAYCONFIG_MODE_INFO));

      // Get display configuration
      result = QueryDisplayConfig(QDC_ALL_PATHS, ref _numPathArrayElements, _pPathArray, ref _numModeArrayElements, _pModeArray, IntPtr.Zero);
      if (result != 0)
      {
        ServiceRegistration.Get<ILogger>().Error("RefreshRateChanger: QueryDisplayConfig(...) returned {0}", result);
        return;
      }

      _offset = GetModeInfoOffsetForDisplayId(_displayIndex, _pModeArray, _numModeArrayElements);
      if (_offset == -1)
      {
        ServiceRegistration.Get<ILogger>().Error("RefreshRateChanger: Couldn't find a target mode info for display {0}", _displayIndex);
        return;
      }
      _initialized = true;
    }

    protected int GetModeInfoOffsetForDisplayId(uint displayIndex, IntPtr pModeArray, uint uNumModeArrayElements)
    {
      // There are always two mode infos per display (target and source)
      int offset = (int)(displayIndex * SIZE_OF_DISPLAYCONFIG_MODE_INFO * 2);

      // Out of bounds sanity check
      if (offset + SIZE_OF_DISPLAYCONFIG_MODE_INFO >= uNumModeArrayElements * SIZE_OF_DISPLAYCONFIG_MODE_INFO)
        return -1;

      // Check which one of the two mode infos for the display is the target
      int modeInfoType = Marshal.ReadInt32(pModeArray, offset);
      if (modeInfoType == DISPLAYCONFIG_MODE_INFO_TYPE_TARGET)
        return offset;

      offset += (int)SIZE_OF_DISPLAYCONFIG_MODE_INFO;

      modeInfoType = Marshal.ReadInt32(pModeArray, offset);
      if (modeInfoType == DISPLAYCONFIG_MODE_INFO_TYPE_TARGET)
        return offset;
      return -1;
    }

    #endregion

    #region IDisposable
    public virtual void Dispose()
    {
      // Free memory and return refresh rate
      if (_pPathArray != IntPtr.Zero)
        Marshal.FreeHGlobal(_pPathArray);
      if (_pModeArray != IntPtr.Zero)
        Marshal.FreeHGlobal(_pModeArray);
    }
    #endregion
  }
}