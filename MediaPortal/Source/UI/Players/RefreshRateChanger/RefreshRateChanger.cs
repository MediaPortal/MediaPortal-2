#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
  /// <summary>
  /// This class takes care of wrapping "Connecting and Configuring Displays(CCD) Win32 API"
  /// Author Erti-Chris Eelmaa || easter199 at hotmail dot com
  /// </summary>
  public class CCDWrapper
  {
    [StructLayout(LayoutKind.Sequential)]
    public struct LUID
    {
      public uint LowPart;
      public uint HighPart;
    }

    public enum DisplayConfigVideoOutputTechnology : uint
    {
      Other = 4294967295, // -1
      Hd15 = 0,
      Svideo = 1,
      CompositeVideo = 2,
      ComponentVideo = 3,
      Dvi = 4,
      Hdmi = 5,
      Lvds = 6,
      DJpn = 8,
      Sdi = 9,
      DisplayportExternal = 10,
      DisplayportEmbedded = 11,
      UdiExternal = 12,
      UdiEmbedded = 13,
      Sdtvdongle = 14,
      Internal = 0x80000000,
      ForceUint32 = 0xFFFFFFFF
    }

    #region SdcFlags enum

    [Flags]
    public enum SdcFlags : uint
    {
      Zero = 0,

      TopologyInternal = 0x00000001,
      TopologyClone = 0x00000002,
      TopologyExtend = 0x00000004,
      TopologyExternal = 0x00000008,
      TopologySupplied = 0x00000010,

      UseSuppliedDisplayConfig = 0x00000020,
      Validate = 0x00000040,
      Apply = 0x00000080,
      NoOptimization = 0x00000100,
      SaveToDatabase = 0x00000200,
      AllowChanges = 0x00000400,
      PathPersistIfRequired = 0x00000800,
      ForceModeEnumeration = 0x00001000,
      AllowPathOrderChanges = 0x00002000,

      UseDatabaseCurrent = TopologyInternal | TopologyClone | TopologyExtend | TopologyExternal
    }

    [Flags]
    public enum DisplayConfigFlags : uint
    {
      Zero = 0x0,
      PathActive = 0x00000001
    }

    public enum DisplayConfigSourceStatus
    {
      Zero = 0x0,
      InUse = 0x00000001
    }

    [Flags]
    public enum DisplayConfigTargetStatus : uint
    {
      Zero = 0x0,
      InUse = 0x00000001,
      Forcible = 0x00000002,
      ForcedAvailabilityBoot = 0x00000004,
      ForcedAvailabilityPath = 0x00000008,
      ForcedAvailabilitySystem = 0x00000010,
    }

    public enum DisplayConfigRotation : uint
    {
      Zero = 0x0,
      Identity = 1,
      Rotate90 = 2,
      Rotate180 = 3,
      Rotate270 = 4,
      ForceUint32 = 0xFFFFFFFF
    }

    public enum DisplayConfigPixelFormat : uint
    {
      Zero = 0x0,
      Pixelformat8Bpp = 1,
      Pixelformat16Bpp = 2,
      Pixelformat24Bpp = 3,
      Pixelformat32Bpp = 4,
      PixelformatNongdi = 5,
      PixelformatForceUint32 = 0xffffffff
    }

    public enum DisplayConfigScaling : uint
    {
      Zero = 0x0,
      Identity = 1,
      Centered = 2,
      Stretched = 3,
      Aspectratiocenteredmax = 4,
      Custom = 5,
      Preferred = 128,
      ForceUint32 = 0xFFFFFFFF
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DisplayConfigRational
    {
      public uint Numerator;
      public uint Denominator;
    }

    public enum DisplayConfigScanLineOrdering : uint
    {
      Unspecified = 0,
      Progressive = 1,
      Interlaced = 2,
      InterlacedUpperfieldfirst = Interlaced,
      InterlacedLowerfieldfirst = 3,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DisplayConfigPathInfo
    {
      public DisplayConfigPathSourceInfo SourceInfo;
      public DisplayConfigPathTargetInfo TargetInfo;
      public uint Flags;
    }

    public enum DisplayConfigModeInfoType : uint
    {
      Zero = 0,
      Source = 1,
      Target = 2,
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct DisplayConfigModeInfo
    {
      [FieldOffset(0)]
      public DisplayConfigModeInfoType InfoType;

      [FieldOffset(4)]
      public uint Id;

      [FieldOffset(8)]
      public LUID AdapterId;

      [FieldOffset(16)]
      public DisplayConfigTargetMode TargetMode;

      [FieldOffset(16)]
      public DisplayConfigSourceMode SourceMode;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DisplayConfig2DRegion
    {
      public uint cx;
      public uint cy;
    }

    public enum D3DmdtVideoSignalStandard : uint
    {
      Uninitialized = 0,
      VesaDmt = 1,
      VesaGtf = 2,
      VesaCvt = 3,
      Ibm = 4,
      Apple = 5,
      NtscM = 6,
      NtscJ = 7,
      Ntsc443 = 8,
      PalB = 9,
      PalB1 = 10,
      PalG = 11,
      PalH = 12,
      PalI = 13,
      PalD = 14,
      PalN = 15,
      PalNc = 16,
      SecamB = 17,
      SecamD = 18,
      SecamG = 19,
      SecamH = 20,
      SecamK = 21,
      SecamK1 = 22,
      SecamL = 23,
      SecamL1 = 24,
      Eia861 = 25,
      Eia861A = 26,
      Eia861B = 27,
      PalK = 28,
      PalK1 = 29,
      PalL = 30,
      PalM = 31,
      Other = 255
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DisplayConfigVideoSignalInfo
    {
      public long PixelRate;
      public DisplayConfigRational HorizontalSyncFreq;
      public DisplayConfigRational VerticalSyncFreq;
      public DisplayConfig2DRegion ActiveSize;
      public DisplayConfig2DRegion TotalSize;

      public D3DmdtVideoSignalStandard VideoStandard;
      public DisplayConfigScanLineOrdering ScanLineOrdering;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DisplayConfigTargetMode
    {
      public DisplayConfigVideoSignalInfo TargetVideoSignalInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PointL
    {
      public int X;
      public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DisplayConfigSourceMode
    {
      public uint Width;
      public uint Height;
      public DisplayConfigPixelFormat PixelFormat;
      public PointL Position;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DisplayConfigPathSourceInfo
    {
      public LUID AdapterId;
      public uint Id;
      public uint ModeInfoIdx;

      public DisplayConfigSourceStatus StatusFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DisplayConfigPathTargetInfo
    {
      public LUID AdapterId;
      public uint Id;
      public uint ModeInfoIdx;
      public DisplayConfigVideoOutputTechnology OutputTechnology;
      public DisplayConfigRotation Rotation;
      public DisplayConfigScaling Scaling;
      public DisplayConfigRational RefreshRate;
      public DisplayConfigScanLineOrdering ScanLineOrdering;

      public bool TargetAvailable;
      public DisplayConfigTargetStatus StatusFlags;
    }

    [Flags]
    public enum QueryDisplayFlags : uint
    {
      Zero = 0x0,
      AllPaths = 0x00000001,
      OnlyActivePaths = 0x00000002,
      DatabaseCurrent = 0x00000004
    }

    [Flags]
    public enum DisplayConfigTopologyId : uint
    {
      Zero = 0x0,
      Internal = 0x00000001,
      Clone = 0x00000002,
      Extend = 0x00000004,
      External = 0x00000008,
      ForceUint32 = 0xFFFFFFFF
    }

    #endregion

    [DllImport("User32.dll")]
    public static extern int SetDisplayConfig(
      uint numPathArrayElements,
      [In] DisplayConfigPathInfo[] pathArray,
      uint numModeInfoArrayElements,
      [In] DisplayConfigModeInfo[] modeInfoArray,
      SdcFlags flags
    );

    [DllImport("User32.dll")]
    public static extern int QueryDisplayConfig(
      QueryDisplayFlags flags,
      ref uint numPathArrayElements,
      [Out] DisplayConfigPathInfo[] pathInfoArray,
      ref uint modeInfoArrayElements,
      [Out] DisplayConfigModeInfo[] modeInfoArray,
      IntPtr z
    );

    [DllImport("User32.dll")]
    public static extern int GetDisplayConfigBufferSizes(QueryDisplayFlags flags, out uint numPathArrayElements, out uint numModeInfoArrayElements);
  }

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
    private uint _numPathArrayElements;
    private uint _numModeInfoArrayElements;
    private CCDWrapper.DisplayConfigPathInfo[] _pathInfoArray;
    private CCDWrapper.DisplayConfigModeInfo[] _modeInfoArray;

    public RefreshRateChanger(uint displayIndex)
    {
      _displayIndex = displayIndex;
      InitBuffers();
    }

    #region public members

    public double GetRefreshRate()
    {
      if (!_initialized)
        return 0.0;

      // Calculate refresh rate
      var modeIndex = _displayIndex * 2; // Array always contains "Source" and "Target" per display
      var numerator = _modeInfoArray[modeIndex].TargetMode.TargetVideoSignalInfo.VerticalSyncFreq.Numerator;
      var denominator = _modeInfoArray[modeIndex].TargetMode.TargetVideoSignalInfo.VerticalSyncFreq.Denominator;
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
      CCDWrapper.DisplayConfigScanLineOrdering scanLineOrdering;
      switch (newRefreshRate)
      {
        case 23976:
          numerator = 24000;
          denominator = 1001;
          scanLineOrdering = CCDWrapper.DisplayConfigScanLineOrdering.Progressive;
          break;
        case 24000:
          numerator = 24000;
          denominator = 1000;
          scanLineOrdering = CCDWrapper.DisplayConfigScanLineOrdering.Progressive;
          break;
        case 25000:
          numerator = 25000;
          denominator = 1000;
          scanLineOrdering = CCDWrapper.DisplayConfigScanLineOrdering.Progressive;
          break;
        case 29970:
          numerator = 30000;
          denominator = 1001;
          scanLineOrdering = CCDWrapper.DisplayConfigScanLineOrdering.Progressive;
          break;
        case 30000:
          numerator = 30000;
          denominator = 1000;
          scanLineOrdering = CCDWrapper.DisplayConfigScanLineOrdering.Progressive;
          break;
        case 50000:
          numerator = 50000;
          denominator = 1000;
          scanLineOrdering = CCDWrapper.DisplayConfigScanLineOrdering.Progressive;
          break;
        case 59940:
          numerator = 60000;
          denominator = 1001;
          scanLineOrdering = CCDWrapper.DisplayConfigScanLineOrdering.Progressive;
          break;
        case 60000:
          numerator = 60000;
          denominator = 1000;
          scanLineOrdering = CCDWrapper.DisplayConfigScanLineOrdering.Progressive;
          break;
        default:
          numerator = newRefreshRate / 1000;
          denominator = 1;
          scanLineOrdering = CCDWrapper.DisplayConfigScanLineOrdering.Progressive;
          break;
      }

      // Set refresh rate parameters in display config
      var modeIndex = _displayIndex * 2; // Array always contains "Source" and "Target" per display
      _modeInfoArray[modeIndex].TargetMode.TargetVideoSignalInfo.VerticalSyncFreq.Numerator = numerator;
      _modeInfoArray[modeIndex].TargetMode.TargetVideoSignalInfo.VerticalSyncFreq.Denominator = denominator;
      _modeInfoArray[modeIndex].TargetMode.TargetVideoSignalInfo.ScanLineOrdering = scanLineOrdering;

      // Validate new refresh rate
      CCDWrapper.SdcFlags flags = CCDWrapper.SdcFlags.Validate | CCDWrapper.SdcFlags.UseSuppliedDisplayConfig;
      long result = CCDWrapper.SetDisplayConfig(_numPathArrayElements, _pathInfoArray, _numModeInfoArrayElements, _modeInfoArray, flags);
      // Adding SDC_ALLOW_CHANGES to flags if validation failed
      if (result != 0)
      {
        ServiceRegistration.Get<ILogger>().Debug("RefreshRateChanger.SetDisplayConfig(...): SDC_VALIDATE of {0}/{1} failed", numerator, denominator);
        flags = CCDWrapper.SdcFlags.Apply | CCDWrapper.SdcFlags.UseSuppliedDisplayConfig | CCDWrapper.SdcFlags.AllowChanges;
      }
      else
      {
        ServiceRegistration.Get<ILogger>().Debug("RefreshRateChanger.SetDisplayConfig(...): SDC_VALIDATE of {0}/{1} succesful", numerator, denominator);
        flags = CCDWrapper.SdcFlags.Apply | CCDWrapper.SdcFlags.UseSuppliedDisplayConfig;
      }

      // Configuring display
      result = CCDWrapper.SetDisplayConfig(_numPathArrayElements, _pathInfoArray, _numModeInfoArrayElements, _modeInfoArray, flags);

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
      // query active paths from the current computer.
      int result = CCDWrapper.GetDisplayConfigBufferSizes(CCDWrapper.QueryDisplayFlags.OnlyActivePaths, out _numPathArrayElements, out _numModeInfoArrayElements);
      if (result != 0)
      {
        ServiceRegistration.Get<ILogger>().Error("RefreshRateChanger: GetDisplayConfigBufferSizes(...) returned {0}", result);
        return;
      }

      _pathInfoArray = new CCDWrapper.DisplayConfigPathInfo[_numPathArrayElements];
      _modeInfoArray = new CCDWrapper.DisplayConfigModeInfo[_numModeInfoArrayElements];

      // Get display configuration
      result = CCDWrapper.QueryDisplayConfig(CCDWrapper.QueryDisplayFlags.OnlyActivePaths, ref _numPathArrayElements, _pathInfoArray, ref _numModeInfoArrayElements, _modeInfoArray, IntPtr.Zero);
      if (result != 0)
      {
        ServiceRegistration.Get<ILogger>().Error("RefreshRateChanger: QueryDisplayConfig(...) returned {0}", result);
        return;
      }

      _initialized = true;
    }

    #endregion

    public virtual void Dispose()
    {
    }
  }
}
