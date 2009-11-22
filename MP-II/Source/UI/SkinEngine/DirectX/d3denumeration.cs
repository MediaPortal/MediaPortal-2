#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion


using System.Collections;
using System.Diagnostics;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using SlimDX;
using SlimDX.Direct3D9;

namespace MediaPortal.UI.SkinEngine.DirectX
{
  /// <summary>
  /// Enumeration of all possible D3D vertex processing types
  /// </summary>
  public enum VertexProcessingType
  {
    Software,
    Mixed,
    Hardware,
    PureHardware
  }


  /// <summary>
  /// Info about a display adapter
  /// </summary>
  public class GraphicsAdapterInfo
  {
    public int AdapterOrdinal;
    public AdapterDetails AdapterDetails;
    public ArrayList DisplayModeList = new ArrayList(); // List of D3DDISPLAYMODEs
    public ArrayList DeviceInfoList = new ArrayList(); // List of D3DDeviceInfos

    public override string ToString()
    {
      return AdapterDetails.Description;
    }
  }


  /// <summary>
  /// Info about a D3D device, including a list of DeviceCombos (see below) 
  /// that work with the device
  /// </summary>
  public class GraphicsDeviceInfo
  {
    public int AdapterOrdinal;
    public DeviceType DevType;
    public Capabilities Caps;
    public ArrayList DeviceComboList = new ArrayList(); // List of D3DDeviceCombos

    public override string ToString()
    {
      return DevType.ToString();
    }
  }


  /// <summary>
  /// Info about a depth/stencil buffer format that is incompatible with a
  /// multisample type
  /// </summary>
  public class DepthStencilMultiSampleConflict
  {
    public Format DepthStencilFormat;
    public MultisampleType MultisampleType;
  }


  /// <summary>
  /// A combination of adapter format, back buffer format, and windowed/fullscreen 
  /// that is compatible with a particular D3D device (and the app)
  /// </summary>
  public class DeviceCombo
  {
    public int AdapterOrdinal;
    public DeviceType DevType;
    public Format AdapterFormat;
    public Format BackBufferFormat;
    public bool IsWindowed;
    public ArrayList DepthStencilFormatList = new ArrayList(); // List of D3DFORMATs
    public ArrayList MultisampleTypeList = new ArrayList(); // List of D3DMULTISAMPLE_TYPEs
    public ArrayList MultiSampleQualityList = new ArrayList(); // List of ints (maxQuality per multisample type)
    public ArrayList DepthStencilMultiSampleConflictList = new ArrayList(); // List of DepthStencilMultiSampleConflicts
    public ArrayList VertexProcessingTypeList = new ArrayList(); // List of VertexProcessingTypes
    public ArrayList PresentIntervalList = new ArrayList(); // List of D3DPRESENT_INTERVALs
  }


  /// <summary>
  /// Used to sort Displaymodes
  /// </summary>
  internal class DisplayModeComparer : IComparer
  {
    /// <summary>
    /// Compare two display modes
    /// </summary>
    public int Compare(object x, object y)
    {
      DisplayMode dx = (DisplayMode)x;
      DisplayMode dy = (DisplayMode)y;

      if (dx.Width > dy.Width)
      {
        return 1;
      }
      if (dx.Width < dy.Width)
      {
        return -1;
      }
      if (dx.Height > dy.Height)
      {
        return 1;
      }
      if (dx.Height < dy.Height)
      {
        return -1;
      }
      if (dx.Format > dy.Format)
      {
        return 1;
      }
      if (dx.Format < dy.Format)
      {
        return -1;
      }
      if (dx.RefreshRate > dy.RefreshRate)
      {
        return 1;
      }
      if (dx.RefreshRate < dy.RefreshRate)
      {
        return -1;
      }
      return 0;
    }
  }


  /// <summary>
  /// Enumerates available adapters, devices, modes, etc.
  /// </summary>
  public class D3DEnumeration
  {
    /// <summary>
    /// The confirm device delegate which is used to determine if a device 
    /// meets the needs of the simulation
    /// </summary>
    public delegate bool ConfirmDeviceCallbackType(Capabilities caps,
                                                   VertexProcessingType vertexProcessingType, Format adapterFormat,
                                                   Format backBufferFormat);

    public ConfirmDeviceCallbackType ConfirmDeviceCallback;
    public ArrayList AdapterInfoList = new ArrayList(); // List of D3DAdapterInfos

    // The following variables can be used to limit what modes, formats, 
    // etc. are enumerated.  Set them to the values you want before calling
    // Enumerate().
    public int AppMinFullscreenWidth = 640;
    public int AppMinFullscreenHeight = 480;
    public int AppMinColorChannelBits = 5; // min color bits per channel in adapter format
    public int AppMinAlphaChannelBits = 0; // min alpha bits per pixel in back buffer format
    public int AppMinDepthBits = 15;
    public int AppMinStencilBits = 0;
    public bool AppUsesDepthBuffer = true;
    public bool AppUsesMixedVP = false; // whether app can take advantage of mixed vp mode


    /// <summary>
    /// Enumerates available D3D adapters, devices, modes, etc.
    /// </summary>
    public void Enumerate()
    {
      foreach (AdapterInformation ai in MPDirect3D.Direct3D.Adapters)
      {
        ArrayList adapterFormatList = new ArrayList();
        GraphicsAdapterInfo adapterInfo = new GraphicsAdapterInfo();
        adapterInfo.AdapterOrdinal = ai.Adapter;
        adapterInfo.AdapterDetails = ai.Details;

        // Get list of all display modes on this adapter.  
        // Also build a temporary list of all display adapter formats.
        foreach (DisplayMode displayMode in ai.GetDisplayModes(Format.X8R8G8B8))
        {
          if (displayMode.Width < AppMinFullscreenWidth)
          {
            continue;
          }
          if (displayMode.Height < AppMinFullscreenHeight)
          {
            continue;
          }
          if (D3DUtil.GetColorChannelBits(displayMode.Format) < AppMinColorChannelBits)
          {
            continue;
          }
          adapterInfo.DisplayModeList.Add(displayMode);
          if (!adapterFormatList.Contains(displayMode.Format))
          {
            adapterFormatList.Add(displayMode.Format);
          }
        }

        // Sort displaymode list
        DisplayModeComparer dmc = new DisplayModeComparer();
        adapterInfo.DisplayModeList.Sort(dmc);

        // Get info for each device on this adapter
        EnumerateDevices(adapterInfo, adapterFormatList);

        // If at least one device on this adapter is available and compatible
        // with the app, add the adapterInfo to the list
        if (adapterInfo.DeviceInfoList.Count == 0)
        {
          continue;
        }
        AdapterInfoList.Add(adapterInfo);
      }
    }


    /// <summary>
    /// Enumerates D3D devices for a particular adapter
    /// </summary>
    [DebuggerStepThrough]
    protected void EnumerateDevices(GraphicsAdapterInfo adapterInfo, ArrayList adapterFormatList)
    {
      DeviceType[] devTypeArray = new DeviceType[] { DeviceType.Hardware, DeviceType.Software, DeviceType.Reference };

      foreach (DeviceType devType in devTypeArray)
      {
        GraphicsDeviceInfo deviceInfo = new GraphicsDeviceInfo();
        deviceInfo.AdapterOrdinal = adapterInfo.AdapterOrdinal;
        deviceInfo.DevType = devType;

        try
        {
          deviceInfo.Caps = MPDirect3D.Direct3D.GetDeviceCaps(adapterInfo.AdapterOrdinal, devType);
        }
        catch
        {
          continue;
        }

        // Get info for each devicecombo on this device
        EnumerateDeviceCombos(deviceInfo, adapterFormatList);

        // If at least one devicecombo for this device is found, 
        // add the deviceInfo to the list
        if (deviceInfo.DeviceComboList.Count == 0)
        {
          continue;
        }
        adapterInfo.DeviceInfoList.Add(deviceInfo);
      }
    }


    /// <summary>
    /// Enumerates DeviceCombos for a particular device
    /// </summary>
    protected void EnumerateDeviceCombos(GraphicsDeviceInfo deviceInfo, ArrayList adapterFormatList)
    {
      Format[] backBufferFormatArray = new Format[]
        {
          Format.A8R8G8B8, Format.X8R8G8B8, Format.A2R10G10B10,
          Format.R5G6B5, Format.A1R5G5B5, Format.X1R5G5B5,
        };
      bool[] isWindowedArray = new bool[] { false, true };

      // See which adapter formats are supported by this device
      foreach (Format adapterFormat in adapterFormatList)
      {
        foreach (Format backBufferFormat in backBufferFormatArray)
        {
          if (D3DUtil.GetAlphaChannelBits(backBufferFormat) < AppMinAlphaChannelBits)
          {
            continue;
          }
          foreach (bool isWindowed in isWindowedArray)
          {

            if (!MPDirect3D.Direct3D.CheckDeviceType(deviceInfo.AdapterOrdinal,
                                                     deviceInfo.DevType,
                                                     adapterFormat,
                                                     backBufferFormat,
                                                     isWindowed))
              continue;



            // At this point, we have an adapter/device/adapterformat/backbufferformat/iswindowed
            // DeviceCombo that is supported by the system.  We still need to confirm that it's 
            // compatible with the app, and find one or more suitable depth/stencil buffer format,
            // multisample type, vertex processing type, and present interval.
            DeviceCombo deviceCombo = new DeviceCombo();
            deviceCombo.AdapterOrdinal = deviceInfo.AdapterOrdinal;
            deviceCombo.DevType = deviceInfo.DevType;
            deviceCombo.AdapterFormat = adapterFormat;
            deviceCombo.BackBufferFormat = backBufferFormat;
            deviceCombo.IsWindowed = isWindowed;
            if (AppUsesDepthBuffer)
            {
              BuildDepthStencilFormatList(deviceCombo);
              if (deviceCombo.DepthStencilFormatList.Count == 0)
              {
                continue;
              }
            }
            BuildMultisampleTypeList(deviceCombo);
            if (deviceCombo.MultisampleTypeList.Count == 0)
            {
              continue;
            }
            BuildDepthStencilMultiSampleConflictList(deviceCombo);
            BuildVertexProcessingTypeList(deviceInfo, deviceCombo);
            if (deviceCombo.VertexProcessingTypeList.Count == 0)
            {
              continue;
            }
            BuildPresentIntervalList(deviceInfo, deviceCombo);
            if (deviceCombo.PresentIntervalList.Count == 0)
            {
              continue;
            }

            deviceInfo.DeviceComboList.Add(deviceCombo);
          }
        }
      }
    }


    /// <summary>
    /// Adds all depth/stencil formats that are compatible with the device and app to
    /// the given deviceCombo
    /// </summary>
    public void BuildDepthStencilFormatList(DeviceCombo deviceCombo)
    {
      Format[] depthStencilFormatArray =
        {
          Format.D16,
          Format.D15S1,
          Format.D24X8,
          Format.D24S8,
          Format.D24X4S4,
          Format.D32,
        };

      foreach (Format depthStencilFmt in depthStencilFormatArray)
      {
        if (D3DUtil.GetDepthBits(depthStencilFmt) < AppMinDepthBits)
        {
          continue;
        }
        if (D3DUtil.GetStencilBits(depthStencilFmt) < AppMinStencilBits)
        {
          continue;
        }
        if (MPDirect3D.Direct3D.CheckDeviceFormat(deviceCombo.AdapterOrdinal, deviceCombo.DevType, deviceCombo.AdapterFormat,
                                      Usage.DepthStencil, ResourceType.Surface, depthStencilFmt))
        {
          if (MPDirect3D.Direct3D.CheckDepthStencilMatch(deviceCombo.AdapterOrdinal, deviceCombo.DevType,
                                             deviceCombo.AdapterFormat, deviceCombo.BackBufferFormat, depthStencilFmt))
          {
            deviceCombo.DepthStencilFormatList.Add(depthStencilFmt);
          }
        }
      }
    }


    /// <summary>
    /// Adds all multisample types that are compatible with the device and app to
    /// the given deviceCombo
    /// </summary>
    public void BuildMultisampleTypeList(DeviceCombo deviceCombo)
    {
      MultisampleType[] msTypeArray = {
                                        MultisampleType.None,
                                        MultisampleType.NonMaskable,
                                        MultisampleType.TwoSamples,
                                        MultisampleType.ThreeSamples,
                                        MultisampleType.FourSamples,
                                        MultisampleType.FiveSamples,
                                        MultisampleType.SixSamples,
                                        MultisampleType.SevenSamples,
                                        MultisampleType.EightSamples,
                                        MultisampleType.NineSamples,
                                        MultisampleType.TenSamples,
                                        MultisampleType.ElevenSamples,
                                        MultisampleType.TwelveSamples,
                                        MultisampleType.ThirteenSamples,
                                        MultisampleType.FourteenSamples,
                                        MultisampleType.FifteenSamples,
                                        MultisampleType.SixteenSamples,
                                      };

      foreach (MultisampleType msType in msTypeArray)
      {
        Result result;
        int qualityLevels = 0;
        if (MPDirect3D.Direct3D.CheckDeviceMultisampleType(deviceCombo.AdapterOrdinal, deviceCombo.DevType,
                                               deviceCombo.BackBufferFormat, deviceCombo.IsWindowed, msType,
                                               out qualityLevels, out result))
        {
          deviceCombo.MultisampleTypeList.Add(msType);
          deviceCombo.MultiSampleQualityList.Add(qualityLevels);
        }
      }
    }


    /// <summary>
    /// Finds any depthstencil formats that are incompatible with multisample types and
    /// builds a list of them.
    /// </summary>
    public void BuildDepthStencilMultiSampleConflictList(DeviceCombo deviceCombo)
    {
      DepthStencilMultiSampleConflict DSMSConflict;

      foreach (Format dsFmt in deviceCombo.DepthStencilFormatList)
      {
        foreach (MultisampleType msType in deviceCombo.MultisampleTypeList)
        {
          if (!MPDirect3D.Direct3D.CheckDeviceMultisampleType(deviceCombo.AdapterOrdinal,
                                                              deviceCombo.DevType, 
                                                              (Format)dsFmt, 
                                                              deviceCombo.IsWindowed, 
                                                              msType))
          {
            DSMSConflict = new DepthStencilMultiSampleConflict();
            DSMSConflict.DepthStencilFormat = dsFmt;
            DSMSConflict.MultisampleType = msType;
            deviceCombo.DepthStencilMultiSampleConflictList.Add(DSMSConflict);
          }
        }
      }
    }


    /// <summary>
    /// Adds all vertex processing types that are compatible with the device and app to
    /// the given deviceCombo
    /// </summary>
    public void BuildVertexProcessingTypeList(GraphicsDeviceInfo deviceInfo, DeviceCombo deviceCombo)
    {
      if ((deviceInfo.Caps.DeviceCaps & DeviceCaps.HWTransformAndLight) != 0)
      {
        if ((deviceInfo.Caps.DeviceCaps & DeviceCaps.PureDevice) != 0)
        {
          if (ConfirmDeviceCallback == null ||
              ConfirmDeviceCallback(deviceInfo.Caps, VertexProcessingType.PureHardware,
                                    deviceCombo.AdapterFormat, deviceCombo.BackBufferFormat))
          {
            deviceCombo.VertexProcessingTypeList.Add(VertexProcessingType.PureHardware);
          }
        }
        if (ConfirmDeviceCallback == null ||
            ConfirmDeviceCallback(deviceInfo.Caps, VertexProcessingType.Hardware,
                                  deviceCombo.AdapterFormat, deviceCombo.BackBufferFormat))
        {
          deviceCombo.VertexProcessingTypeList.Add(VertexProcessingType.Hardware);
        }
        if (AppUsesMixedVP && (ConfirmDeviceCallback == null ||
                               ConfirmDeviceCallback(deviceInfo.Caps, VertexProcessingType.Mixed,
                                                     deviceCombo.AdapterFormat, deviceCombo.BackBufferFormat)))
        {
          deviceCombo.VertexProcessingTypeList.Add(VertexProcessingType.Mixed);
        }
      }
      if (ConfirmDeviceCallback == null ||
          ConfirmDeviceCallback(deviceInfo.Caps, VertexProcessingType.Software,
                                deviceCombo.AdapterFormat, deviceCombo.BackBufferFormat))
      {
        deviceCombo.VertexProcessingTypeList.Add(VertexProcessingType.Software);
      }
    }


    /// <summary>
    /// Adds all present intervals that are compatible with the device and app to
    /// the given deviceCombo
    /// </summary>
    public void BuildPresentIntervalList(GraphicsDeviceInfo deviceInfo, DeviceCombo deviceCombo)
    {
      PresentInterval[] piArray = {
                                    PresentInterval.Immediate,
                                    PresentInterval.Default,
                                    PresentInterval.One,
                                    PresentInterval.Two,
                                    PresentInterval.Three,
                                    PresentInterval.Four,
                                  };

      foreach (PresentInterval pi in piArray)
      {
        if (deviceCombo.IsWindowed)
        {
          if (pi == PresentInterval.Two ||
              pi == PresentInterval.Three ||
              pi == PresentInterval.Four)
          {
            // These intervals are not supported in windowed mode.
            continue;
          }
        }
        // Note that PresentInterval.Default is zero, so you
        // can't do a caps check for it -- it is always available.

        if (pi == PresentInterval.Default ||
            (deviceInfo.Caps.PresentationIntervals & pi) != (PresentInterval)0)
        {
          deviceCombo.PresentIntervalList.Add(pi);
        }
      }
    }
  }
} ;
