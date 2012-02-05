#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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

//#define PROFILE_PERFORMANCE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Settings;
using MediaPortal.UI.SkinEngine.Settings;
using MediaPortal.Utilities;
using SlimDX.Direct3D9;

namespace MediaPortal.UI.SkinEngine.DirectX
{
  internal class D3DSetup
  {
    #region Classes

    /// <summary>
    /// An exception for when the ReferenceDevice is null
    /// </summary>
    public class NullReferenceDeviceException : ApplicationException
    {
      public override string Message
      {
        get
        {
          return "Warning: Nothing will be rendered.\n" +
              "The reference rendering device was selected, but your\n" +
              "computer only has a reduced-functionality reference device\n" +
              "installed. Please check if your graphics card and\n" +
              "drivers meet the minimum system requirements.\n";
        }
      }
    }

    #endregion

    /// <summary>
    /// Messages that can be used when displaying an error.
    /// </summary>
    public enum ApplicationMessage
    {
      None,
      ApplicationMustExit,
      WarnSwitchToRef
    }

    private System.Windows.Forms.Control _ourRenderTarget; // The window we will render too

    // We need to keep track of our enumeration settings
    protected D3DEnumeration _enumerationSettings = new D3DEnumeration();

    protected D3DSettings _graphicsSettings = new D3DSettings();
    private PresentParameters _presentParams = new PresentParameters();
    protected string _deviceStats; // String to hold D3D device stats
    private Form _window;
    bool _usingPerfHud = false;

    protected System.Windows.Forms.Control RenderTarget
    {
      get { return _ourRenderTarget; }
      set { _ourRenderTarget = value; }
    }

    public IList<DisplayMode> GetDisplayModes()
    {
      return _graphicsSettings.FullscreenDisplayModes.Where(mode => mode.Width != 0 || mode.Height != 0 || mode.RefreshRate != 0).ToList();
    }

    public string DesktopDisplayMode
    {
      get
      {
        DisplayMode mode = _graphicsSettings.FullscreenDisplayModes[_graphicsSettings.DesktopDisplayMode];
        return string.Format("{0}x{1}@{2}", mode.Width, mode.Height, mode.RefreshRate);
      }
    }

    public int DesktopHeight
    {
      get
      {
        DisplayMode mode = _graphicsSettings.FullscreenDisplayModes[_graphicsSettings.DesktopDisplayMode];
        return mode.Height;
      }
    }

    public int DesktopWidth
    {
      get
      {
        DisplayMode mode = _graphicsSettings.FullscreenDisplayModes[_graphicsSettings.DesktopDisplayMode];
        return mode.Width;
      }
    }

    public PresentParameters PresentParameters
    {
      get { return _presentParams; }
      set { _presentParams = value; }
    }

    public bool Windowed
    {
      get { return _graphicsSettings.IsWindowed; }
      set { _graphicsSettings.IsWindowed = value; }
    }

    public IEnumerable<MultisampleType> WindowedMultisampleTypes
    {
      get { return _graphicsSettings.WindowedDeviceCombo.MultisampleTypes.Select(mst => mst.Key); }
    }

    public Present WindowedPresent
    {
      get { return _graphicsSettings.WindowedPresent; }
    }

    /// <summary>
    /// Picks the best graphics device and initializes it.
    /// </summary>
    /// <param name="form">The form.</param>
    /// <returns>Device, if a good device was found, else <c>null</c>.</returns>
    public DeviceEx SetupDirectX(Form form)
    {
      _window = form;
      RenderTarget = form;
      _enumerationSettings.ConfirmDeviceCallback = ConfirmDevice;
      _enumerationSettings.Enumerate();

      if (_ourRenderTarget.Cursor == null)
      {
        // Set up a default cursor
        _ourRenderTarget.Cursor = Cursors.Default;
      }

      try
      {
        if (!FindBestModes())
          Environment.Exit(0);

        // Initialize the 3D environment for the app
        _graphicsSettings.IsWindowed = true;
        return CreateDevice();
      }
      catch (Exception ex)
      {
        HandleException(ex, ApplicationMessage.ApplicationMustExit);
        return null;
      }
    }

    /// <summary>
    /// Sets up graphicsSettings with best available windowed mode, subject to 
    /// the <paramref name="doesRequireHardware"/> and <paramref name="doesRequireReference"/> constraints.  
    /// </summary>
    /// <param name="doesRequireHardware">The device requires hardware support.</param>
    /// <param name="doesRequireReference">The device requires the ref device.</param>
    /// <returns><c>true</c> if a mode is found, <c>false</c> otherwise.</returns>
    public bool FindBestWindowedMode(bool doesRequireHardware, bool doesRequireReference)
    {
      // Get display mode of primary adapter (which is assumed to be where the window will appear)

      DisplayMode primaryDesktopDisplayMode = MPDirect3D.Direct3D.Adapters[0].CurrentDisplayMode;
      bool perfHudFound = false;
      for (int i = 0; i < MPDirect3D.Direct3D.Adapters.Count; ++i)
      {
        AdapterInformation adapter = MPDirect3D.Direct3D.Adapters[i];
        string name = adapter.Details.Description;
        if ("NVIDIA PerfHUD".Equals(name, StringComparison.InvariantCultureIgnoreCase))
        {
          ServiceRegistration.Get<ILogger>().Info("DirectX: Found PerfHUD adapter: {0} {1} ", i, adapter.Details.Description);
          primaryDesktopDisplayMode = adapter.CurrentDisplayMode;
          perfHudFound = true;
          _usingPerfHud = true;
          doesRequireReference = true;
          break;
        }
      }
      GraphicsAdapterInfo bestAdapterInfo = null;
      GraphicsDeviceInfo bestDeviceInfo = null;
      DeviceCombo bestDeviceCombo = null;
      foreach (GraphicsAdapterInfo adapterInfo in _enumerationSettings.AdapterInfoList)
      {
        if (perfHudFound)
        {
          string name = adapterInfo.AdapterDetails.Description;
          if (!"NVIDIA PerfHUD".Equals(name, StringComparison.InvariantCultureIgnoreCase))
            continue;
        }
        /*
        if (GUIGraphicsContext._useScreenSelector)
        {
          adapterInfo = FindAdapterForScreen(GUI.Library.GUIGraphicsContext.currentScreen);
          primaryDesktopDisplayMode = Direct3D.Adapters[adapterInfo.AdapterOrdinal].CurrentDisplayMode;
        }*/
        foreach (GraphicsDeviceInfo deviceInfo in adapterInfo.DeviceInfos)
        {
          if (doesRequireHardware && deviceInfo.DevType != DeviceType.Hardware)
            continue;
          if (doesRequireReference && deviceInfo.DevType != DeviceType.Reference)
            continue;

          foreach (DeviceCombo deviceCombo in deviceInfo.DeviceCombos)
          {
            bool adapterMatchesBackBuffer = (deviceCombo.BackBufferFormat == deviceCombo.AdapterFormat);
            if (!deviceCombo.IsWindowed)
              continue;
            if (deviceCombo.AdapterFormat != primaryDesktopDisplayMode.Format)
              continue;

            // If we haven't found a compatible DeviceCombo yet, or if this set
            // is better (because it's a HAL, and/or because formats match better),
            // save it
            if (bestDeviceCombo == null ||
                bestDeviceCombo.DevType != DeviceType.Hardware && deviceInfo.DevType == DeviceType.Hardware ||
                deviceCombo.DevType == DeviceType.Hardware && adapterMatchesBackBuffer)
            {
              bestAdapterInfo = adapterInfo;
              bestDeviceInfo = deviceInfo;
              bestDeviceCombo = deviceCombo;
              if (deviceInfo.DevType == DeviceType.Hardware && adapterMatchesBackBuffer)
                // This windowed device combo looks great -- take it
                goto EndWindowedDeviceComboSearch;
              // Otherwise keep looking for a better windowed device combo
            }
          }
        }
        //if (GUIGraphicsContext._useScreenSelector)
        //  break;// no need to loop again.. result would be the same
      }

    EndWindowedDeviceComboSearch:
      if (bestDeviceCombo == null)
        return false;

      _graphicsSettings.WindowedAdapterInfo = bestAdapterInfo;
      _graphicsSettings.WindowedDeviceInfo = bestDeviceInfo;
      _graphicsSettings.WindowedDeviceCombo = bestDeviceCombo;
      _graphicsSettings.WindowedDisplayMode = primaryDesktopDisplayMode;
      _graphicsSettings.WindowedWidth = _ourRenderTarget.Width;
      _graphicsSettings.WindowedHeight = _ourRenderTarget.Height;
      if (_enumerationSettings.AppUsesDepthBuffer)
        _graphicsSettings.WindowedDepthStencilBufferFormat = bestDeviceCombo.DepthStencilFormats.FirstOrDefault();

      AppSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<AppSettings>();

      _graphicsSettings.WindowedMultisampleType = settings.MultisampleType;
      _graphicsSettings.WindowedMultisampleQuality = 0;

      _graphicsSettings.WindowedVertexProcessingType = bestDeviceCombo.VertexProcessingTypes.FirstOrDefault();
      _graphicsSettings.WindowedPresentInterval = bestDeviceCombo.PresentIntervals.FirstOrDefault();

      return true;
    }

    /// <summary>
    /// Sets up graphicsSettings with best available fullscreen mode, subject to 
    /// the doesRequireHardware and doesRequireReference constraints.  
    /// </summary>
    /// <param name="doesRequireHardware">Does the device require hardware support</param>
    /// <param name="doesRequireReference">Does the device require the ref device</param>
    /// <returns>true if a mode is found, false otherwise</returns>
    public bool FindBestFullscreenMode(bool doesRequireHardware, bool doesRequireReference)
    {
      // For fullscreen, default to first HAL DeviceCombo that supports the current desktop 
      // display mode, or any display mode if HAL is not compatible with the desktop mode, or 
      // non-HAL if no HAL is available
      DisplayMode adapterDesktopDisplayMode;
      DisplayMode bestAdapterDesktopDisplayMode = new DisplayMode {Width = 0, Height = 0, Format = 0, RefreshRate = 0};

      GraphicsAdapterInfo bestAdapterInfo = null;
      GraphicsDeviceInfo bestDeviceInfo = null;
      DeviceCombo bestDeviceCombo = null;

      foreach (GraphicsAdapterInfo adapterInfoIterate in _enumerationSettings.AdapterInfoList)
      {
        GraphicsAdapterInfo adapterInfo = adapterInfoIterate;

        //if (GUIGraphicsContext._useScreenSelector)
        //  adapterInfo = FindAdapterForScreen(GUI.Library.GUIGraphicsContext.currentScreen);

        adapterDesktopDisplayMode = MPDirect3D.Direct3D.Adapters[adapterInfo.AdapterOrdinal].CurrentDisplayMode;
        foreach (GraphicsDeviceInfo deviceInfo in adapterInfo.DeviceInfos)
        {
          if (doesRequireHardware && deviceInfo.DevType != DeviceType.Hardware)
            continue;
          if (doesRequireReference && deviceInfo.DevType != DeviceType.Reference)
            continue;

          foreach (DeviceCombo deviceCombo in deviceInfo.DeviceCombos)
          {
            bool adapterMatchesBackBuffer = (deviceCombo.BackBufferFormat == deviceCombo.AdapterFormat);
            bool adapterMatchesDesktop = (deviceCombo.AdapterFormat == adapterDesktopDisplayMode.Format);
            if (deviceCombo.IsWindowed)
              continue;

            // If we haven't found a compatible set yet, or if this set
            // is better (because it's a HAL, and/or because formats match better),
            // save it
            if (bestDeviceCombo == null ||
                bestDeviceCombo.DevType != DeviceType.Hardware && deviceInfo.DevType == DeviceType.Hardware ||
                bestDeviceCombo.DevType == DeviceType.Hardware &&
                bestDeviceCombo.AdapterFormat != adapterDesktopDisplayMode.Format && adapterMatchesDesktop ||
                bestDeviceCombo.DevType == DeviceType.Hardware && adapterMatchesDesktop && adapterMatchesBackBuffer)
            {
              bestAdapterDesktopDisplayMode = adapterDesktopDisplayMode;
              bestAdapterInfo = adapterInfo;
              bestDeviceInfo = deviceInfo;
              bestDeviceCombo = deviceCombo;
              if (deviceInfo.DevType == DeviceType.Hardware && adapterMatchesDesktop && adapterMatchesBackBuffer)
                // This fullscreen device combo looks great -- take it
                goto EndFullscreenDeviceComboSearch;
              // Otherwise keep looking for a better fullscreen device combo
            }
          }
        }
        //if (GUIGraphicsContext._useScreenSelector)
        //  break;// no need to loop again.. result would be the same
      }

    EndFullscreenDeviceComboSearch:
      if (bestDeviceCombo == null)
        return false;

      // Need to find a display mode on the best adapter that uses pBestDeviceCombo->AdapterFormat
      // and is as close to bestAdapterDesktopDisplayMode's res as possible

      int NumberOfFullscreenDisplayModes = 0;
      foreach (DisplayMode displayMode in bestAdapterInfo.DisplayModes)
      {
        if (displayMode.Format != bestDeviceCombo.AdapterFormat)
          continue;
        if (NumberOfFullscreenDisplayModes == _graphicsSettings.FullscreenDisplayModes.Length)
          break;
        if (displayMode.Width == bestAdapterDesktopDisplayMode.Width &&
            displayMode.Height == bestAdapterDesktopDisplayMode.Height &&
            displayMode.RefreshRate == bestAdapterDesktopDisplayMode.RefreshRate)
        {
          _graphicsSettings.CurrentFullscreenDisplayMode = NumberOfFullscreenDisplayModes;
          _graphicsSettings.DesktopDisplayMode = NumberOfFullscreenDisplayModes;
        }
        _graphicsSettings.FullscreenDisplayModes[NumberOfFullscreenDisplayModes++] = displayMode;
      }

      if (NumberOfFullscreenDisplayModes == 0)
        return false;

      _graphicsSettings.FullscreenAdapterInfo = bestAdapterInfo;
      _graphicsSettings.FullscreenDeviceInfo = bestDeviceInfo;
      _graphicsSettings.FullscreenDeviceCombo = bestDeviceCombo;

      if (_enumerationSettings.AppUsesDepthBuffer)
        _graphicsSettings.FullscreenDepthStencilBufferFormat = bestDeviceCombo.DepthStencilFormats.FirstOrDefault();
      
      KeyValuePair<MultisampleType, int> mst2quality = bestDeviceCombo.MultisampleTypes.LastOrDefault();
      _graphicsSettings.FullscreenMultisampleType = mst2quality.Key;
      _graphicsSettings.FullscreenMultisampleQuality = 0;

      _graphicsSettings.FullscreenVertexProcessingType = bestDeviceCombo.VertexProcessingTypes.FirstOrDefault();
      _graphicsSettings.FullscreenPresentInterval = PresentInterval.Default;

      return true;
    }

    /// <summary>
    /// Find the best fullscreen / windowed modes.
    /// </summary>
    /// <returns>true if the settings were initialized</returns>
    public bool FindBestModes()
    {

      if (!FindBestFullscreenMode(false, false))
      {
        ServiceRegistration.Get<ILogger>().Critical("D3DSetup: failed to find best fullscreen mode.");
        return false;
      }
      if (!FindBestWindowedMode(false, false))
      {
        ServiceRegistration.Get<ILogger>().Critical("D3DSetup: failed to find best windowed mode.");
        return false;
      }
      return true;
    }

    /// <summary>
    /// Creates the DirectX device.
    /// </summary>
    public DeviceEx CreateDevice()
    {
      GraphicsAdapterInfo adapterInfo = _graphicsSettings.AdapterInfo;
      GraphicsDeviceInfo deviceInfo = _graphicsSettings.DeviceInfo;

      // Set up the presentation parameters
      BuildPresentParamsFromSettings();

      if ((deviceInfo.Caps.PrimitiveMiscCaps & PrimitiveMiscCaps.NullReference) != 0)
        // Warn user about null ref device that can't render anything
        HandleException(new NullReferenceDeviceException(), ApplicationMessage.None);

      CreateFlags createFlags;
      if (_graphicsSettings.VertexProcessingType == VertexProcessingType.Software)
        createFlags = CreateFlags.SoftwareVertexProcessing;
      else if (_graphicsSettings.VertexProcessingType == VertexProcessingType.Mixed)
        createFlags = CreateFlags.MixedVertexProcessing;
      else if (_graphicsSettings.VertexProcessingType == VertexProcessingType.Hardware)
        createFlags = CreateFlags.HardwareVertexProcessing;
      else if (_graphicsSettings.VertexProcessingType == VertexProcessingType.PureHardware)
        createFlags = CreateFlags.HardwareVertexProcessing; // | CreateFlags.PureDevice;
      else
        throw new ApplicationException();

      ServiceRegistration.Get<ILogger>().Info("DirectX: Using adapter: {0} {1} {2}",
          _graphicsSettings.AdapterOrdinal,
          MPDirect3D.Direct3D.Adapters[_graphicsSettings.AdapterOrdinal].Details.Description,
          _graphicsSettings.DevType);
      try
      {
        // Create the device
        DeviceEx result = new DeviceEx(MPDirect3D.Direct3D,
            _graphicsSettings.AdapterOrdinal,
            _graphicsSettings.DevType,
            _ourRenderTarget.Handle,
            createFlags | CreateFlags.Multithreaded,
            _presentParams);

        // When moving from fullscreen to windowed mode, it is important to
        // adjust the window size after recreating the device rather than
        // beforehand to ensure that you get the window size you want.  For
        // example, when switching from 640x480 fullscreen to windowed with
        // a 1000x600 window on a 1024x768 desktop, it is impossible to set
        // the window size to 1000x600 until after the display mode has
        // changed to 1024x768, because windows cannot be larger than the
        // desktop.

        StringBuilder sb = new StringBuilder();

        // Store device description
        if (deviceInfo.DevType == DeviceType.Reference)
          sb.Append("REF");
        else if (deviceInfo.DevType == DeviceType.Hardware)
          sb.Append("HAL");
        else if (deviceInfo.DevType == DeviceType.Software)
          sb.Append("SW");

        if (deviceInfo.DevType == DeviceType.Hardware)
        {
          sb.Append(": ");
          sb.Append(adapterInfo.AdapterDetails.Description);
        }

        // Set device stats string
        _deviceStats = sb.ToString();
        ServiceRegistration.Get<ILogger>().Info("DirectX: {0}", _deviceStats);
        return result;
      }
      catch (Exception e)
      {
        // If that failed, fall back to the reference rasterizer
        if (deviceInfo.DevType == DeviceType.Hardware)
        {
          if (FindBestWindowedMode(false, true))
          {
            _graphicsSettings.IsWindowed = true;

            // Let the user know we are switching from HAL to the reference rasterizer
            HandleException(e, ApplicationMessage.WarnSwitchToRef);

            return CreateDevice();
          }
        }
      }
      return null;
    }

    protected virtual bool ConfirmDevice(Capabilities caps, VertexProcessingType vertexProcessingType,
        Format adapterFormat, Format backBufferFormat)
    {
      return true;
    }

    /// <summary>
    /// Displays sample exceptions to the user
    /// </summary>
    /// <param name="e">The exception that was thrown</param>
    /// <param name="Type">Extra information on how to handle the exception</param>
    public void HandleException(Exception e, ApplicationMessage Type)
    {
      ServiceRegistration.Get<ILogger>().Error(e);
      // Build a message to display to the user
      IList<string> strMsg = new List<string>();
      if (e != null)
        strMsg.Add(e.Message);
      MessageBoxIcon icon = MessageBoxIcon.Exclamation;
      switch (Type)
      {
        case ApplicationMessage.ApplicationMustExit:
          strMsg.Add("MediaPortal has to be closed.");
          icon = MessageBoxIcon.Error;
          break;
        case ApplicationMessage.WarnSwitchToRef:
          strMsg.Add("\n\nSwitching to the reference rasterizer, a software device that implements the entire\n" +
              "Direct3D feature set, but runs very slowly.");
          icon = MessageBoxIcon.Information;
          break;
      }
      MessageBox.Show(StringUtils.Join("\n\n", strMsg), "Error DirectX layer", MessageBoxButtons.OK, icon);
    }

    /// <summary>
    /// Build presentation parameters from the current settings
    /// </summary>
    public void BuildPresentParamsFromSettings()
    {
      _presentParams.Windowed = _graphicsSettings.IsWindowed;

      _presentParams.EnableAutoDepthStencil = false;

      ServiceRegistration.Get<ILogger>().Debug("BuildPresentParamsFromSettings windowed {0} {1} {2}",
          _graphicsSettings.IsWindowed, _ourRenderTarget.ClientRectangle.Width, _ourRenderTarget.ClientRectangle.Height);

      if (_graphicsSettings.IsWindowed)
      {
        _presentParams.Multisample =  _graphicsSettings.WindowedMultisampleType;
        _presentParams.MultisampleQuality = 0;
        _presentParams.AutoDepthStencilFormat = _graphicsSettings.WindowedDepthStencilBufferFormat;
        _presentParams.BackBufferWidth = _ourRenderTarget.ClientRectangle.Width;
        _presentParams.BackBufferHeight = _ourRenderTarget.ClientRectangle.Height;
        _presentParams.BackBufferFormat = _graphicsSettings.BackBufferFormat;
        _presentParams.BackBufferCount = _graphicsSettings.BackBufferCount;
#if PROFILE_PERFORMANCE
        _presentParams.PresentationInterval = PresentInterval.Immediate;
#else
        if (_usingPerfHud)
          _presentParams.PresentationInterval = PresentInterval.Immediate;
        else
          _presentParams.PresentationInterval = PresentInterval.One; 
#endif
        _presentParams.FullScreenRefreshRateInHertz = 0;
        _presentParams.SwapEffect = _graphicsSettings.WindowedSwapEffect;
        _presentParams.PresentFlags = PresentFlags.Video; //PresentFlag.LockableBackBuffer;
        _presentParams.DeviceWindowHandle = _ourRenderTarget.Handle;
        _presentParams.Windowed = true;
      }
      else
      {
        _presentParams.Multisample = _graphicsSettings.FullscreenMultisampleType;
        _presentParams.MultisampleQuality = _graphicsSettings.FullscreenMultisampleQuality;
        //_presentParams.AutoDepthStencilFormat = _graphicsSettings.FullscreenDepthStencilBufferFormat;

        _presentParams.BackBufferWidth = _graphicsSettings.DisplayMode.Width;
        _presentParams.BackBufferHeight = _graphicsSettings.DisplayMode.Height;
        _presentParams.BackBufferFormat = _graphicsSettings.DeviceCombo.BackBufferFormat;
        _presentParams.BackBufferCount = _graphicsSettings.BackBufferCount;

#if PROFILE_PERFORMANCE
        _presentParams.PresentationInterval = PresentInterval.Immediate;
#else
        if (_usingPerfHud)
          _presentParams.PresentationInterval = PresentInterval.Immediate;
        else
          _presentParams.PresentationInterval = PresentInterval.One;
#endif
        _presentParams.FullScreenRefreshRateInHertz = _graphicsSettings.DisplayMode.RefreshRate;
        _presentParams.SwapEffect = SwapEffect.Discard;
        _presentParams.PresentFlags = PresentFlags.Video; //|PresentFlag.LockableBackBuffer;
        _presentParams.DeviceWindowHandle = _window.Handle;
        _presentParams.Windowed = false;
      }
    }
  }
}
