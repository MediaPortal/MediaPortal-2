#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Runtime.InteropServices;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.UI.SkinEngine.SkinManagement;

namespace MediaPortal.Plugins.RefreshRateChanger
{
  public class TemporaryRefreshRateChanger : RefreshRateChanger
  {
    private readonly double _originalRate;
    private string _renderMode;

    public TemporaryRefreshRateChanger(uint displayIndex, bool forceVsync = false)
      : base(displayIndex)
    {
      _originalRate = GetRefreshRate();
      if (forceVsync)
      {
        _renderMode = SkinContext.RenderStrategy.Name;
        while (!SkinContext.RenderStrategy.Name.Contains("VSync"))
          SkinContext.NextRenderStrategy();
      }
    }

    public override void Dispose()
    {
      if (_rateChanged)
        SetRefreshRate(_originalRate);
      if (!string.IsNullOrEmpty(_renderMode))
        while (SkinContext.RenderStrategy.Name != _renderMode)
          SkinContext.NextRenderStrategy();
      base.Dispose();
    }
  }

  public class RefreshRateChanger : IDisposable
  {
    protected bool _rateChanged;
    private readonly uint _displayIndex;
    private bool _initialized;
    private int _offset;
    private uint _numPathArrayElements;
    private uint _numModeArrayElements;
    private IntPtr _pPathArray = IntPtr.Zero;
    private IntPtr _pModeArray = IntPtr.Zero;

    public RefreshRateChanger(uint displayIndex)
    {
      _displayIndex = displayIndex;
      InitBuffers();
    }

    #region Consts

    // ReSharper disable InconsistentNaming
    private const uint SIZE_OF_DISPLAYCONFIG_PATH_INFO = 72;
    private const uint SIZE_OF_DISPLAYCONFIG_MODE_INFO = 64;

    private const uint QDC_ALL_PATHS = 1;
    private const uint DISPLAYCONFIG_MODE_INFO_TYPE_TARGET = 2;
    private const uint DISPLAYCONFIG_SCANLINE_ORDERING_PROGRESSIVE = 1;

    private const uint SDC_USE_SUPPLIED_DISPLAY_CONFIG = 0x00000020;
    private const uint SDC_VALIDATE = 0x00000040;
    private const uint SDC_APPLY = 0x00000080;
    private const uint SDC_ALLOW_CHANGES = 0x00000400;
    // ReSharper enable InconsistentNaming

    #endregion

    #region DLL imports

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

    #region public members

    public double GetRefreshRate()
    {
      if (!_initialized)
        return 0.0;

      // Calculate refresh rate
      UInt32 numerator = (UInt32)Marshal.ReadInt32(_pModeArray, _offset + 32);
      UInt32 denominator = (UInt32)Marshal.ReadInt32(_pModeArray, _offset + 36);
      double refreshRate = numerator / (double)denominator;
      ServiceRegistration.Get<ILogger>().Debug("RefreshRateChanger.GetRefreshRate: QueryDisplayConfig returned {0}/{1}", numerator, denominator);
      return refreshRate;
    }

    public bool SetRefreshRate(double refreshRate)
    {
      if (!_initialized)
        return false;

      // Set proper numerator and denominator for refresh rate
      UInt32 newRefreshRate = (uint)(refreshRate * 1000);
      UInt32 numerator;
      UInt32 denominator;
      UInt32 scanLineOrdering;
      switch (newRefreshRate)
      {
        case 23976:
          numerator = 24000;
          denominator = 1001;
          scanLineOrdering = DISPLAYCONFIG_SCANLINE_ORDERING_PROGRESSIVE;
          break;
        case 24000:
          numerator = 24000;
          denominator = 1000;
          scanLineOrdering = DISPLAYCONFIG_SCANLINE_ORDERING_PROGRESSIVE;
          break;
        case 25000:
          numerator = 25000;
          denominator = 1000;
          scanLineOrdering = DISPLAYCONFIG_SCANLINE_ORDERING_PROGRESSIVE;
          break;
        case 29970:
          numerator = 30000;
          denominator = 1001;
          scanLineOrdering = DISPLAYCONFIG_SCANLINE_ORDERING_PROGRESSIVE;
          break;
        case 30000:
          numerator = 30000;
          denominator = 1000;
          scanLineOrdering = DISPLAYCONFIG_SCANLINE_ORDERING_PROGRESSIVE;
          break;
        case 50000:
          numerator = 50000;
          denominator = 1000;
          scanLineOrdering = DISPLAYCONFIG_SCANLINE_ORDERING_PROGRESSIVE;
          break;
        case 59940:
          numerator = 60000;
          denominator = 1001;
          scanLineOrdering = DISPLAYCONFIG_SCANLINE_ORDERING_PROGRESSIVE;
          break;
        case 60000:
          numerator = 60000;
          denominator = 1000;
          scanLineOrdering = DISPLAYCONFIG_SCANLINE_ORDERING_PROGRESSIVE;
          break;
        default:
          numerator = newRefreshRate / 1000;
          denominator = 1;
          scanLineOrdering = DISPLAYCONFIG_SCANLINE_ORDERING_PROGRESSIVE;
          break;
      }

      // Set refresh rate parameters in display config
      Marshal.WriteInt32(_pModeArray, _offset + 32, (int)numerator);
      Marshal.WriteInt32(_pModeArray, _offset + 36, (int)denominator);
      Marshal.WriteInt32(_pModeArray, _offset + 56, (int)scanLineOrdering);

      // Validate new refresh rate
      UInt32 flags = SDC_VALIDATE | SDC_USE_SUPPLIED_DISPLAY_CONFIG;
      long result = SetDisplayConfig(_numPathArrayElements, _pPathArray, _numModeArrayElements, _pModeArray, flags);
      // Adding SDC_ALLOW_CHANGES to flags if validation failed
      if (result != 0)
      {
        ServiceRegistration.Get<ILogger>().Debug("RefreshRateChanger.SetDisplayConfig(...): SDC_VALIDATE of {0}/{1} failed", numerator, denominator);
        flags = SDC_APPLY | SDC_USE_SUPPLIED_DISPLAY_CONFIG | SDC_ALLOW_CHANGES;
      }
      else
      {
        ServiceRegistration.Get<ILogger>().Debug("RefreshRateChanger.SetDisplayConfig(...): SDC_VALIDATE of {0}/{1} succesful", numerator, denominator);
        flags = SDC_APPLY | SDC_USE_SUPPLIED_DISPLAY_CONFIG;
      }

      // Configuring display
      result = SetDisplayConfig(_numPathArrayElements, _pPathArray, _numModeArrayElements, _pModeArray, flags);

      if (result != 0)
      {
        ServiceRegistration.Get<ILogger>().Warn("RefreshRateChanger.SetDisplayConfig(...): SDC_APPLY returned {0}", result);
        return false;
      }
      ServiceRegistration.Get<ILogger>().Debug("RefreshRateChanger.SetDisplayConfig(...): Successfully switched to {0}/{1}", numerator, denominator);
      Windows7DwmFix.FixDwm();
      _rateChanged = true;
      return true;
    }

    #endregion

    #region Private members

    private void InitBuffers()
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

    private int GetModeInfoOffsetForDisplayId(uint displayIndex, IntPtr pModeArray, uint uNumModeArrayElements)
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

    public virtual void Dispose()
    {
      // Free memory and return refresh rate
      if (_pPathArray != IntPtr.Zero)
        Marshal.FreeHGlobal(_pPathArray);
      if (_pModeArray != IntPtr.Zero)
        Marshal.FreeHGlobal(_pModeArray);
    }
  }
}
