#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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

    private Form _renderTarget; // The window we will render to

    protected D3DEnumeration _enumerationSettings = new D3DEnumeration();

    protected DisplayMode _desktopDisplayMode;
    protected D3DConfiguration _currentGraphicsConfiguration = null;
    private PresentParameters _presentParams = null;

    public Form RenderTarget
    {
      get { return _renderTarget; }
      set { _renderTarget = value; }
    }

    public DisplayMode DesktopDisplayMode
    {
      get { return _desktopDisplayMode; }
    }

    public int DesktopHeight
    {
      get { return _desktopDisplayMode.Height; }
    }

    public int DesktopWidth
    {
      get { return _desktopDisplayMode.Width; }
    }

    public PresentParameters PresentParameters
    {
      get { return _presentParams; }
      set { _presentParams = value; }
    }

    public D3DConfiguration CurrentConfiguration
    {
      get { return _currentGraphicsConfiguration; }
    }

    public bool IsMultiSample
    {
      get { return _presentParams.Multisample != MultisampleType.None; }
    }

    public Present Present
    {
      get { return IsMultiSample ? Present.None : Present.ForceImmediate; }
    }

    /// <summary>
    /// Returns all available MultisampleTypes for the current windowed configuration.
    /// </summary>
    public IEnumerable<MultisampleType> MultisampleTypes
    {
      get { return _currentGraphicsConfiguration.DeviceCombo.MultisampleTypes.Select(mst => mst.Key); }
    }

    /// <summary>
    /// Picks the best graphics device and initializes it.
    /// </summary>
    /// <returns>Created device, if a good device could be created, else <c>null</c>.</returns>
    public DeviceEx SetupDirectX()
    {
      _enumerationSettings.ConfirmDeviceCallback = ConfirmDevice;
      _enumerationSettings.Enumerate();

      if (_renderTarget.Cursor == null)
        _renderTarget.Cursor = Cursors.Default;

      try
      {
        D3DConfiguration configuration = FindBestWindowedMode(false, false);
        if (configuration == null)
        {
          ServiceRegistration.Get<ILogger>().Critical("D3DSetup: Failed to find best windowed display mode.");
          Environment.Exit(0);
        }

        // Initialize the 3D environment for the app
        try
        { 
          return CreateDevice(configuration);
        }
        catch (Exception e)
        {
          ServiceRegistration.Get<ILogger>().Critical("D3DSetup: Failed to initialize device. Falling back to reference rasterizer.", e);
          if (configuration.DeviceInfo.DevType == DeviceType.Hardware)
          {
            // Let the user know we are switching from HAL to the reference rasterizer
            HandleException(e, ApplicationMessage.WarnSwitchToRef);

            configuration = FindBestWindowedMode(false, true);
            if (configuration == null)
            {
              ServiceRegistration.Get<ILogger>().Critical("D3DSetup: Failed to find display mode for reference rasterizer.");
              Environment.Exit(0);
            }

            return CreateDevice(configuration);
          }
        }
      }
      catch (Exception ex)
      {
        HandleException(ex, ApplicationMessage.ApplicationMustExit);
      }
      Environment.Exit(0);
      return null;
    }

    /// <summary>
    /// Creates the DirectX device.
    /// </summary>
    public DeviceEx CreateDevice(D3DConfiguration configuration)
    {
      GraphicsAdapterInfo adapterInfo = configuration.AdapterInfo;
      GraphicsDeviceInfo deviceInfo = configuration.DeviceInfo;

      // Set up the presentation parameters
      _presentParams = BuildPresentParamsFromSettings(_currentGraphicsConfiguration = configuration);

      if ((deviceInfo.Caps.PrimitiveMiscCaps & PrimitiveMiscCaps.NullReference) != 0)
        // Warn user about null ref device that can't render anything
        HandleException(new NullReferenceDeviceException(), ApplicationMessage.None);

      CreateFlags createFlags;
      if (configuration.DeviceCombo.VertexProcessingTypes.Contains(VertexProcessingType.PureHardware))
        createFlags = CreateFlags.HardwareVertexProcessing; // | CreateFlags.PureDevice;
      else if (configuration.DeviceCombo.VertexProcessingTypes.Contains(VertexProcessingType.Hardware))
        createFlags = CreateFlags.HardwareVertexProcessing;
      else if (configuration.DeviceCombo.VertexProcessingTypes.Contains(VertexProcessingType.Mixed))
        createFlags = CreateFlags.MixedVertexProcessing;
      else if (configuration.DeviceCombo.VertexProcessingTypes.Contains(VertexProcessingType.Software))
        createFlags = CreateFlags.SoftwareVertexProcessing;
      else
        throw new ApplicationException();

      ServiceRegistration.Get<ILogger>().Info("DirectX: Using adapter: {0} {1} {2}",
          configuration.AdapterInfo.AdapterOrdinal,
          MPDirect3D.Direct3D.Adapters[configuration.AdapterInfo.AdapterOrdinal].Details.Description,
          configuration.DeviceInfo.DevType);

      // Create the device
      DeviceEx result = new DeviceEx(MPDirect3D.Direct3D,
          configuration.AdapterInfo.AdapterOrdinal,
          configuration.DeviceInfo.DevType,
          _renderTarget.Handle,
          createFlags | CreateFlags.Multithreaded | CreateFlags.EnablePresentStatistics,
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

      ServiceRegistration.Get<ILogger>().Info("DirectX: {0}", sb.ToString());
      return result;
    }

    /// <summary>
    /// Returns a settings object with best available windowed mode, according to 
    /// the <paramref name="doesRequireHardware"/> and <paramref name="doesRequireReference"/> constraints.  
    /// </summary>
    /// <param name="doesRequireHardware">The device requires hardware support.</param>
    /// <param name="doesRequireReference">The device requires the ref device.</param>
    /// <returns><c>true</c> if a mode is found, <c>false</c> otherwise.</returns>
    public D3DConfiguration FindBestWindowedMode(bool doesRequireHardware, bool doesRequireReference)
    {
      D3DConfiguration result = new D3DConfiguration();

      // Get display mode of primary adapter (which is assumed to be where the window will appear)
      DisplayMode primaryDesktopDisplayMode = MPDirect3D.Direct3D.Adapters[0].CurrentDisplayMode;
      GraphicsAdapterInfo bestAdapterInfo = null;
      GraphicsDeviceInfo bestDeviceInfo = null;
      DeviceCombo bestDeviceCombo = null;
      foreach (GraphicsAdapterInfo adapterInfo in _enumerationSettings.AdapterInfoList)
      {
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
            if (!deviceCombo.IsWindowed)
              continue;
            if (deviceCombo.AdapterFormat != primaryDesktopDisplayMode.Format)
              continue;

            bool adapterMatchesBackBuffer = (deviceCombo.BackBufferFormat == deviceCombo.AdapterFormat);

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
        return null;

      result.AdapterInfo = bestAdapterInfo;
      result.DeviceInfo = bestDeviceInfo;
      result.DeviceCombo = bestDeviceCombo;
      result.DisplayMode = primaryDesktopDisplayMode;

      return result;
    }

    /// <summary>
    /// Returns a settings object with best available fullscreen mode, according to 
    /// the <paramref name="doesRequireHardware"/> and <paramref name="doesRequireReference"/> constraints.  
    /// </summary>
    /// <param name="doesRequireHardware">The device requires hardware support.</param>
    /// <param name="doesRequireReference">The device requires the ref device.</param>
    /// <returns><c>true</c> if a mode is found, <c>false</c> otherwise.</returns>
    public bool FindBestFullscreenMode(bool doesRequireHardware, bool doesRequireReference)
    {
      D3DConfiguration result = new D3DConfiguration();

      // For fullscreen, default to first HAL DeviceCombo that supports the current desktop 
      // display mode, or any display mode if HAL is not compatible with the desktop mode, or 
      // non-HAL if no HAL is available
      DisplayMode bestAdapterDesktopDisplayMode = new DisplayMode();

      GraphicsAdapterInfo bestAdapterInfo = null;
      GraphicsDeviceInfo bestDeviceInfo = null;
      DeviceCombo bestDeviceCombo = null;

      foreach (GraphicsAdapterInfo adapterInfo in _enumerationSettings.AdapterInfoList)
      {
        //if (GUIGraphicsContext._useScreenSelector)
        //  adapterInfo = FindAdapterForScreen(GUI.Library.GUIGraphicsContext.currentScreen);

        DisplayMode adapterDesktopDisplayMode = MPDirect3D.Direct3D.Adapters[adapterInfo.AdapterOrdinal].CurrentDisplayMode;
        foreach (GraphicsDeviceInfo deviceInfo in adapterInfo.DeviceInfos)
        {
          if (doesRequireHardware && deviceInfo.DevType != DeviceType.Hardware)
            continue;
          if (doesRequireReference && deviceInfo.DevType != DeviceType.Reference)
            continue;

          foreach (DeviceCombo deviceCombo in deviceInfo.DeviceCombos)
          {
            if (deviceCombo.IsWindowed)
              continue;

            bool adapterMatchesBackBuffer = (deviceCombo.BackBufferFormat == deviceCombo.AdapterFormat);
            bool adapterMatchesDesktop = (deviceCombo.AdapterFormat == adapterDesktopDisplayMode.Format);

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
      foreach (DisplayMode displayMode in bestAdapterInfo.DisplayModes)
      {
        if (displayMode.Format != bestDeviceCombo.AdapterFormat)
          continue;
        if (displayMode.Width == bestAdapterDesktopDisplayMode.Width &&
            displayMode.Height == bestAdapterDesktopDisplayMode.Height &&
            displayMode.RefreshRate == bestAdapterDesktopDisplayMode.RefreshRate)
          _desktopDisplayMode = displayMode;
      }

      result.DisplayMode = bestAdapterDesktopDisplayMode;
      result.AdapterInfo = bestAdapterInfo;
      result.DeviceInfo = bestDeviceInfo;
      result.DeviceCombo = bestDeviceCombo;

      return true;
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
          strMsg.Add("Switching to the reference rasterizer, a software device that implements the entire\n" +
              "Direct3D feature set, but runs very slowly.");
          icon = MessageBoxIcon.Information;
          break;
      }
      MessageBox.Show(StringUtils.Join("\n\n", strMsg), "Error DirectX layer", MessageBoxButtons.OK, icon);
    }

    /// <summary>
    /// Build presentation parameters from the current settings.
    /// </summary>
    public void BuildPresentParamsFromSettings()
    {
      _presentParams = BuildPresentParamsFromSettings(_currentGraphicsConfiguration);
    }

    /// <summary>
    /// Build presentation parameters from the given settings.
    /// </summary>
    /// <param name="configuration">Graphics configuration to use.</param>
    public PresentParameters BuildPresentParamsFromSettings(D3DConfiguration configuration)
    {
      int backBufferWidth;
      int backBufferHeight;
      if (configuration.DeviceCombo.IsWindowed)
      {
        backBufferWidth = _renderTarget.ClientRectangle.Width;
        backBufferHeight = _renderTarget.ClientRectangle.Height;
      }
      else
      {
        backBufferWidth = configuration.DisplayMode.Width;
        backBufferHeight = configuration.DisplayMode.Height;
      }

      ServiceRegistration.Get<ILogger>().Debug("BuildPresentParamsFromSettings: Windowed = {0},  {1} x {2}",
          configuration.DeviceCombo.IsWindowed, backBufferWidth, backBufferHeight);

      PresentParameters result = new PresentParameters();

      AppSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<AppSettings>();

      DeviceCombo dc = configuration.DeviceCombo;
      MultisampleType mst = settings.MultisampleType;
      mst = dc.MultisampleTypes.ContainsKey(mst) ? mst : MultisampleType.None;
      result.Multisample = mst;
      result.MultisampleQuality = 0;
      result.EnableAutoDepthStencil = false;
      result.AutoDepthStencilFormat = dc.DepthStencilFormats.FirstOrDefault(dsf =>
          !dc.DepthStencilMultiSampleConflicts.Contains(new DepthStencilMultiSampleConflict {DepthStencilFormat = dsf, MultisampleType = mst}));
      // Note that PresentFlags.Video makes NVidia graphics drivers switch off multisampling antialiasing
      result.PresentFlags = PresentFlags.None;
      result.DeviceWindowHandle = _renderTarget.Handle;
      result.Windowed = configuration.DeviceCombo.IsWindowed;
      result.BackBufferFormat = configuration.DeviceCombo.BackBufferFormat;
      result.BackBufferCount = 4; // 2 to 4 are recommended for FlipEx swap mode
#if PROFILE_PERFORMANCE
      _presentParams.PresentationInterval = PresentInterval.Immediate;
#else
      result.PresentationInterval = PresentInterval.One;
#endif
      result.FullScreenRefreshRateInHertz = result.Windowed ? 0 : configuration.DisplayMode.RefreshRate;
      
      // From http://msdn.microsoft.com/en-us/library/windows/desktop/bb173422%28v=vs.85%29.aspx :
      // To use multisampling, the SwapEffect member of D3DPRESENT_PARAMETER must be set to D3DSWAPEFFECT_DISCARD.
      // SwapEffect must be set to SwapEffect.FlipEx to support the Present property to be Present.ForceImmediate
      // (see http://msdn.microsoft.com/en-us/library/windows/desktop/bb174343%28v=vs.85%29.aspx )
      result.SwapEffect = mst == MultisampleType.None ? SwapEffect.FlipEx : SwapEffect.Discard;

      result.BackBufferWidth = backBufferWidth;
      result.BackBufferHeight = backBufferHeight;

      return result;
    }
  }
}
