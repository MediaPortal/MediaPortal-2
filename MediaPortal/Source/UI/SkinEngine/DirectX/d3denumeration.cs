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
using System.Collections.Generic;
using System.Diagnostics;
using SharpDX;
using SharpDX.Direct3D9;

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
  /// Info about a display adapter.
  /// </summary>
  public class GraphicsAdapterInfo
  {
    public int AdapterOrdinal;
    public AdapterDetails AdapterDetails;
    public ICollection<DisplayMode> DisplayModes = new List<DisplayMode>(); // Collection of D3DDISPLAYMODEs
    public ICollection<GraphicsDeviceInfo> DeviceInfos = new List<GraphicsDeviceInfo>(); // Collection of D3DDeviceInfos

    public override string ToString()
    {
      return AdapterDetails.Description;
    }
  }

  /// <summary>
  /// Info about a D3D device, including a list of <see cref="DeviceCombo"/>s
  /// that work with the device.
  /// </summary>
  public class GraphicsDeviceInfo
  {
    public int AdapterOrdinal;
    public DeviceType DevType;
    public Capabilities Caps;
    public ICollection<DeviceCombo> DeviceCombos = new List<DeviceCombo>();

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

    public override int GetHashCode()
    {
      return DepthStencilFormat.GetHashCode() + MultisampleType.GetHashCode();
    }

    public override bool Equals(object obj)
    {
      if (!(obj is DepthStencilMultiSampleConflict))
        return false;
      DepthStencilMultiSampleConflict other =  (DepthStencilMultiSampleConflict) obj;
      return other.DepthStencilFormat == DepthStencilFormat && other.MultisampleType == MultisampleType;
    }

    public override string ToString()
    {
      return DepthStencilFormat + "; " + MultisampleType;
    }
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
    public ICollection<Format> DepthStencilFormats = new List<Format>(); // Collection of D3DFORMATs
    public IDictionary<MultisampleType, int> MultisampleTypes = new Dictionary<MultisampleType, int>(); // Collection of D3DMULTISAMPLE_TYPEs mapped to their max quality (device manufacturer dependent)
    public ICollection<DepthStencilMultiSampleConflict> DepthStencilMultiSampleConflicts = new List<DepthStencilMultiSampleConflict>();
    public ICollection<VertexProcessingType> VertexProcessingTypes = new List<VertexProcessingType>();
    public ICollection<PresentInterval> PresentIntervals = new List<PresentInterval>(); // Collection of D3DPRESENT_INTERVALs
  }

  /// <summary>
  /// Used to sort display modes.
  /// </summary>
  internal class DisplayModeComparer : IComparer<DisplayMode>
  {
    /// <summary>
    /// Compare two display modes.
    /// </summary>
    public int Compare(DisplayMode dx, DisplayMode dy)
    {
      if (dx.Width > dy.Width)
        return 1;
      if (dx.Width < dy.Width)
        return -1;
      if (dx.Height > dy.Height)
        return 1;
      if (dx.Height < dy.Height)
        return -1;
      if (dx.Format > dy.Format)
        return 1;
      if (dx.Format < dy.Format)
        return -1;
      if (dx.RefreshRate > dy.RefreshRate)
        return 1;
      if (dx.RefreshRate < dy.RefreshRate)
        return -1;
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
    public delegate bool ConfirmDeviceCallbackType(Capabilities caps, VertexProcessingType vertexProcessingType, 
        Format adapterFormat, Format backBufferFormat);

    public ConfirmDeviceCallbackType ConfirmDeviceCallback;
    public ICollection<GraphicsAdapterInfo> AdapterInfoList = new List<GraphicsAdapterInfo>(); // List of D3DAdapterInfos

    // The following variables can be used to limit what modes, formats, 
    // etc. are enumerated. Set them to the values you want before calling
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
        ICollection<Format> adapterFormatList = new List<Format>();
        GraphicsAdapterInfo adapterInfo = new GraphicsAdapterInfo
          {
              AdapterOrdinal = ai.Adapter,
              AdapterDetails = ai.Details
          };
        
        List<DisplayMode> displayModes = new List<DisplayMode>();
        // Get list of all display modes on this adapter.  
        // Also build a temporary list of all display adapter formats.
        foreach (DisplayMode displayMode in ai.GetDisplayModes(Format.X8R8G8B8))
        {
          if (displayMode.Width < AppMinFullscreenWidth)
            continue;
          if (displayMode.Height < AppMinFullscreenHeight)
            continue;
          if (D3DUtil.GetColorChannelBits(displayMode.Format) < AppMinColorChannelBits)
            continue;
          displayModes.Add(displayMode);
          if (!adapterFormatList.Contains(displayMode.Format))
            adapterFormatList.Add(displayMode.Format);
        }

        // Sort displaymode list
        DisplayModeComparer dmc = new DisplayModeComparer();
        displayModes.Sort(dmc);
        adapterInfo.DisplayModes = displayModes;

        // Get info for each device on this adapter
        EnumerateDevices(adapterInfo, adapterFormatList);

        // If at least one device on this adapter is available and compatible
        // with the app, add the adapterInfo to the list
        if (adapterInfo.DeviceInfos.Count == 0)
          continue;
        AdapterInfoList.Add(adapterInfo);
      }
    }

    /// <summary>
    /// Enumerates D3D devices for a particular adapter
    /// </summary>
    [DebuggerStepThrough]
    protected void EnumerateDevices(GraphicsAdapterInfo adapterInfo, ICollection<Format> adapterFormatList)
    {
      DeviceType[] devTypeArray = new DeviceType[] { DeviceType.Hardware, DeviceType.Software, DeviceType.Reference };

      foreach (DeviceType devType in devTypeArray)
      {
        GraphicsDeviceInfo deviceInfo = new GraphicsDeviceInfo
          {
              AdapterOrdinal = adapterInfo.AdapterOrdinal,
              DevType = devType
          };
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
        if (deviceInfo.DeviceCombos.Count == 0)
          continue;
        adapterInfo.DeviceInfos.Add(deviceInfo);
      }
    }

    /// <summary>
    /// Enumerates DeviceCombos for a particular device
    /// </summary>
    protected void EnumerateDeviceCombos(GraphicsDeviceInfo deviceInfo, ICollection<Format> adapterFormatList)
    {
      Format[] backBufferFormats = new Format[]
        {
          Format.A8R8G8B8, Format.X8R8G8B8, Format.A2R10G10B10,
          Format.R5G6B5, Format.A1R5G5B5, Format.X1R5G5B5,
        };
      bool[] bools = new bool[] { false, true };

      // See which adapter formats are supported by this device
      foreach (Format adapterFormat in adapterFormatList)
      {
        foreach (Format backBufferFormat in backBufferFormats)
        {
          if (D3DUtil.GetAlphaChannelBits(backBufferFormat) < AppMinAlphaChannelBits)
            continue;
          foreach (bool isWindowed in bools)
          {
            if (!MPDirect3D.Direct3D.CheckDeviceType(deviceInfo.AdapterOrdinal, deviceInfo.DevType,
                adapterFormat, backBufferFormat, isWindowed))
              continue;

            // At this point, we have an adapter/device/adapterformat/backbufferformat/iswindowed
            // DeviceCombo that is supported by the system.  We still need to confirm that it's 
            // compatible with the app, and find one or more suitable depth/stencil buffer format,
            // multisample type, vertex processing type, and present interval.
            DeviceCombo deviceCombo = new DeviceCombo
              {
                  AdapterOrdinal = deviceInfo.AdapterOrdinal,
                  DevType = deviceInfo.DevType,
                  AdapterFormat = adapterFormat,
                  BackBufferFormat = backBufferFormat,
                  IsWindowed = isWindowed
              };
            if (AppUsesDepthBuffer)
            {
              BuildDepthStencilFormatList(deviceCombo);
              if (deviceCombo.DepthStencilFormats.Count == 0)
                continue;
            }
            BuildMultisampleTypeList(deviceCombo);
            if (deviceCombo.MultisampleTypes.Count == 0)
              continue;
            BuildDepthStencilMultiSampleConflictList(deviceCombo);
            BuildVertexProcessingTypeList(deviceInfo, deviceCombo);
            if (deviceCombo.VertexProcessingTypes.Count == 0)
              continue;
            BuildPresentIntervalList(deviceInfo, deviceCombo);
            if (deviceCombo.PresentIntervals.Count == 0)
              continue;

            deviceInfo.DeviceCombos.Add(deviceCombo);
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
          continue;
        if (D3DUtil.GetStencilBits(depthStencilFmt) < AppMinStencilBits)
          continue;
        if (MPDirect3D.Direct3D.CheckDeviceFormat(deviceCombo.AdapterOrdinal, deviceCombo.DevType, deviceCombo.AdapterFormat,
            Usage.DepthStencil, ResourceType.Surface, depthStencilFmt))
          if (MPDirect3D.Direct3D.CheckDepthStencilMatch(deviceCombo.AdapterOrdinal, deviceCombo.DevType,
              deviceCombo.AdapterFormat, deviceCombo.BackBufferFormat, depthStencilFmt))
            deviceCombo.DepthStencilFormats.Add(depthStencilFmt);
      }
    }

    /// <summary>
    /// Adds all multisample types that are compatible with the device and app to
    /// the given deviceCombo
    /// </summary>
    public void BuildMultisampleTypeList(DeviceCombo deviceCombo)
    {
      foreach (MultisampleType msType in Enum.GetValues(typeof(MultisampleType)))
      {
        Result result;
        int qualityLevels;
        if (MPDirect3D.Direct3D.CheckDeviceMultisampleType(deviceCombo.AdapterOrdinal, deviceCombo.DevType,
            deviceCombo.BackBufferFormat, deviceCombo.IsWindowed, msType, out qualityLevels, out result))
          deviceCombo.MultisampleTypes.Add(msType, qualityLevels);
      }
    }

    /// <summary>
    /// Finds any depthstencil formats that are incompatible with multisample types and
    /// builds a list of them.
    /// </summary>
    public void BuildDepthStencilMultiSampleConflictList(DeviceCombo deviceCombo)
    {
      foreach (Format dsFmt in deviceCombo.DepthStencilFormats)
        foreach (MultisampleType mst in deviceCombo.MultisampleTypes.Keys)
          if (!MPDirect3D.Direct3D.CheckDeviceMultisampleType(deviceCombo.AdapterOrdinal,
              deviceCombo.DevType, dsFmt, deviceCombo.IsWindowed, mst))
          {
            deviceCombo.DepthStencilMultiSampleConflicts.Add(
                new DepthStencilMultiSampleConflict {DepthStencilFormat = dsFmt, MultisampleType = mst});
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
          if (ConfirmDeviceCallback == null ||
              ConfirmDeviceCallback(deviceInfo.Caps, VertexProcessingType.PureHardware,
                  deviceCombo.AdapterFormat, deviceCombo.BackBufferFormat))
            deviceCombo.VertexProcessingTypes.Add(VertexProcessingType.PureHardware);
        if (ConfirmDeviceCallback == null ||
            ConfirmDeviceCallback(deviceInfo.Caps, VertexProcessingType.Hardware,
                deviceCombo.AdapterFormat, deviceCombo.BackBufferFormat))
          deviceCombo.VertexProcessingTypes.Add(VertexProcessingType.Hardware);
        if (AppUsesMixedVP && (ConfirmDeviceCallback == null ||
            ConfirmDeviceCallback(deviceInfo.Caps, VertexProcessingType.Mixed,
                deviceCombo.AdapterFormat, deviceCombo.BackBufferFormat)))
          deviceCombo.VertexProcessingTypes.Add(VertexProcessingType.Mixed);
      }
      if (ConfirmDeviceCallback == null ||
          ConfirmDeviceCallback(deviceInfo.Caps, VertexProcessingType.Software,
              deviceCombo.AdapterFormat, deviceCombo.BackBufferFormat))
        deviceCombo.VertexProcessingTypes.Add(VertexProcessingType.Software);
    }

    /// <summary>
    /// Adds all present intervals that are compatible with the device and app to
    /// the given deviceCombo
    /// </summary>
    public void BuildPresentIntervalList(GraphicsDeviceInfo deviceInfo, DeviceCombo deviceCombo)
    {
      foreach (PresentInterval pi in Enum.GetValues(typeof(PresentInterval)))
      {
        if (deviceCombo.IsWindowed)
          if (pi == PresentInterval.Two ||
              pi == PresentInterval.Three ||
              pi == PresentInterval.Four)
            // These intervals are not supported in windowed mode.
            continue;

        // Note that PresentInterval.Default is zero, so you
        // can't do a caps check for it -- it is always available.
        if (pi == PresentInterval.Default || (deviceInfo.Caps.PresentationIntervals & pi) != 0)
          deviceCombo.PresentIntervals.Add(pi);
      }
    }
  }
}
