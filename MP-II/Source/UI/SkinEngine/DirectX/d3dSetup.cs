//#define PROFILE_PERFORMANCE
#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using SlimDX;
using SlimDX.Direct3D9;

namespace MediaPortal.UI.SkinEngine.DirectX
{
  public class MPDirect3D 
  {

    private static Direct3D _d3d;

    public static Direct3D Direct3D
    {
      get { return _d3d; }
    }

    public static void Load()
    {
 
      if(_d3d == null)
        _d3d = new Direct3D();
    }

    public static void Unload()
    {
      if (_d3d != null)
      {
        _d3d.Dispose();
      }
      _d3d = null;
    }
  }

  internal class d3dSetup
  {
    /// <summary>
    /// Messages that can be used when displaying an error
    /// </summary>
    public enum ApplicationMessage
    {
      None,
      ApplicationMustExit,
      WarnSwitchToRef
    }

    //private bool _windowed = true;

    private System.Windows.Forms.Control _ourRenderTarget; // The window we will render too

    protected D3DEnumeration _enumerationSettings = new D3DEnumeration();
    // We need to keep track of our enumeration settings

    protected D3DSettings _graphicsSettings = new D3DSettings();
    private PresentParameters _presentParams = new PresentParameters();
    protected string _deviceStats; // String to hold D3D device stats
    private Form _window;
    private CancelEventHandler _cancelEventHandler;
    bool _usingPerfHud = false;

    protected System.Windows.Forms.Control RenderTarget
    {
      get { return _ourRenderTarget; }
      set { _ourRenderTarget = value; }
    }

    public IList<DisplayMode> GetDisplayModes()
    {
      IList<DisplayMode> result = new List<DisplayMode>();
      foreach (DisplayMode mode in _graphicsSettings.FullscreenDisplayModes)
      {
        if ((mode.Width == 0) && (mode.Height == 0) && (mode.RefreshRate == 0))
          continue;
        result.Add(mode);
      }
      return result;
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

    /// <summary>
    /// Picks the best graphics device, and initializes it
    /// </summary>
    /// <param name="form">The form.</param>
    /// <param name="exclusive">set to true if exclusive mode</param>
    /// <returns>true if a good device was found, false otherwise</returns>
    public bool SetupDirectX(Form form, bool exclusiveMode)
    {
      _cancelEventHandler = CancelAutoResizeEvent;
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
        {
          System.Environment.Exit(0);
        }

        /*

        // Depending on witch screen MP is started it will move to the monitor where it was configured to be
        Screen formOnScreen = Screen.FromRectangle(Bounds);
        if (!formOnScreen.Equals(GUIGraphicsContext.currentScreen))
        {
          Rectangle newBounds = Bounds;
          Point location = this.Location;

          if (newBounds.Width > GUIGraphicsContext.currentScreen.Bounds.Width)
            newBounds.Width = GUIGraphicsContext.currentScreen.Bounds.Width;
          if (newBounds.Height > GUIGraphicsContext.currentScreen.Bounds.Height)
            newBounds.Height = GUIGraphicsContext.currentScreen.Bounds.Height;

          newBounds.X = (GUIGraphicsContext.currentScreen.Bounds.Width - newBounds.Width) / 2;
          newBounds.Y = (GUIGraphicsContext.currentScreen.Bounds.Height - newBounds.Height) / 2;

          newBounds.X += GUIGraphicsContext.currentScreen.Bounds.Left;
          newBounds.Y += GUIGraphicsContext.currentScreen.Bounds.Top;

          this.Bounds = newBounds;
        }
        oldBounds = Bounds;

        */
        // Initialize the 3D environment for the app
        _graphicsSettings.IsWindowed = !exclusiveMode;
        InitializeEnvironment();
        // Initialize the app's custom scene stuff
      }
      catch (SampleException d3de)
      {
        HandleSampleException(d3de, ApplicationMessage.ApplicationMustExit);
        return false;
      }
      catch
      {
        HandleSampleException(new SampleException(), ApplicationMessage.ApplicationMustExit);
        return false;
      }

      // The app is ready to go
      //ready = true;


      return true;
    }

    /// <summary>
    /// Sets up graphicsSettings with best available windowed mode, subject to 
    /// the doesRequireHardware and doesRequireReference constraints.  
    /// </summary>
    /// <param name="doesRequireHardware">Does the device require hardware support</param>
    /// <param name="doesRequireReference">Does the device require the ref device</param>
    /// <returns>true if a mode is found, false otherwise</returns>
    public bool FindBestWindowedMode(bool doesRequireHardware, bool doesRequireReference)
    {
      // Get display mode of primary adapter (which is assumed to be where the window 
      // will appear)

      DisplayMode primaryDesktopDisplayMode = MPDirect3D.Direct3D.Adapters[0].CurrentDisplayMode;
      bool perfHudFound = false;
      for (int i = 0; i < MPDirect3D.Direct3D.Adapters.Count; ++i)
      {
        string name = MPDirect3D.Direct3D.Adapters[i].Details.Description;
        if (String.Compare(name, "NVIDIA PerfHUD", true) == 0)
        {
          ServiceScope.Get<ILogger>().Info("DirectX: found perfhud adapter:{0} {1} ",
                  i, MPDirect3D.Direct3D.Adapters[i].Details.Description);
          primaryDesktopDisplayMode = MPDirect3D.Direct3D.Adapters[i].CurrentDisplayMode;
          perfHudFound = true;
          _usingPerfHud = true;
          doesRequireReference = true;
          break;
        }

      }
      GraphicsAdapterInfo bestAdapterInfo = null;
      GraphicsDeviceInfo bestDeviceInfo = null;
      DeviceCombo bestDeviceCombo = null;
      foreach (GraphicsAdapterInfo adapterInfoIterate in _enumerationSettings.AdapterInfoList)
      {
        GraphicsAdapterInfo adapterInfo = adapterInfoIterate;

        if (perfHudFound)
        {
          string name = adapterInfo.AdapterDetails.Description;
          if (String.Compare(name, "NVIDIA PerfHUD", true) != 0) continue;
        }
        /*
        if (GUIGraphicsContext._useScreenSelector)
        {
          adapterInfo = FindAdapterForScreen(GUI.Library.GUIGraphicsContext.currentScreen);
          primaryDesktopDisplayMode = Direct3D.Adapters[adapterInfo.AdapterOrdinal].CurrentDisplayMode;
        }*/
        foreach (GraphicsDeviceInfo deviceInfo in adapterInfo.DeviceInfoList)
        {
          if (doesRequireHardware && deviceInfo.DevType != DeviceType.Hardware)
          {
            continue;
          }
          if (doesRequireReference && deviceInfo.DevType != DeviceType.Reference)
          {
            continue;
          }

          foreach (DeviceCombo deviceCombo in deviceInfo.DeviceComboList)
          {
            bool adapterMatchesBackBuffer = (deviceCombo.BackBufferFormat == deviceCombo.AdapterFormat);
            if (!deviceCombo.IsWindowed)
            {
              continue;
            }
            if (deviceCombo.AdapterFormat != primaryDesktopDisplayMode.Format)
            {
              continue;
            }

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
              {
                // This windowed device combo looks great -- take it
                goto EndWindowedDeviceComboSearch;
              }
              // Otherwise keep looking for a better windowed device combo
            }
          }
        }
        //if (GUIGraphicsContext._useScreenSelector)
        //  break;// no need to loop again.. result would be the same
      }

    EndWindowedDeviceComboSearch:
      if (bestDeviceCombo == null)
      {
        return false;
      }

      _graphicsSettings.WindowedAdapterInfo = bestAdapterInfo;
      _graphicsSettings.WindowedDeviceInfo = bestDeviceInfo;
      _graphicsSettings.WindowedDeviceCombo = bestDeviceCombo;
      _graphicsSettings.IsWindowed = true;
      _graphicsSettings.WindowedDisplayMode = primaryDesktopDisplayMode;
      _graphicsSettings.WindowedWidth = _ourRenderTarget.Width;
      _graphicsSettings.WindowedHeight = _ourRenderTarget.Height;
      if (_enumerationSettings.AppUsesDepthBuffer)
      {
        _graphicsSettings.WindowedDepthStencilBufferFormat = (Format)bestDeviceCombo.DepthStencilFormatList[0];
      }
      int iQuality = 0; //bestDeviceCombo.MultisampleTypeList.Count-1;
      if (bestDeviceCombo.MultisampleTypeList.Count > 0)
        iQuality = bestDeviceCombo.MultisampleTypeList.Count - 1;
      _graphicsSettings.WindowedMultisampleType = (MultisampleType)bestDeviceCombo.MultisampleTypeList[iQuality];
      _graphicsSettings.WindowedMultisampleQuality = 0; //(int)bestDeviceCombo.MultisampleQualityList[iQuality];

      _graphicsSettings.WindowedVertexProcessingType =
        (VertexProcessingType)bestDeviceCombo.VertexProcessingTypeList[0];
      _graphicsSettings.WindowedPresentInterval = (PresentInterval)bestDeviceCombo.PresentIntervalList[0];

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
      DisplayMode bestAdapterDesktopDisplayMode = new DisplayMode();
      bestAdapterDesktopDisplayMode.Width = 0;
      bestAdapterDesktopDisplayMode.Height = 0;
      bestAdapterDesktopDisplayMode.Format = 0;
      bestAdapterDesktopDisplayMode.RefreshRate = 0;

      GraphicsAdapterInfo bestAdapterInfo = null;
      GraphicsDeviceInfo bestDeviceInfo = null;
      DeviceCombo bestDeviceCombo = null;

      foreach (GraphicsAdapterInfo adapterInfoIterate in _enumerationSettings.AdapterInfoList)
      {
        GraphicsAdapterInfo adapterInfo = adapterInfoIterate;

        /*if (GUIGraphicsContext._useScreenSelector)
        {
          adapterInfo = FindAdapterForScreen(GUI.Library.GUIGraphicsContext.currentScreen);
        }*/

        adapterDesktopDisplayMode = MPDirect3D.Direct3D.Adapters[adapterInfo.AdapterOrdinal].CurrentDisplayMode;
        foreach (GraphicsDeviceInfo deviceInfo in adapterInfo.DeviceInfoList)
        {
          if (doesRequireHardware && deviceInfo.DevType != DeviceType.Hardware)
          {
            continue;
          }
          if (doesRequireReference && deviceInfo.DevType != DeviceType.Reference)
          {
            continue;
          }

          foreach (DeviceCombo deviceCombo in deviceInfo.DeviceComboList)
          {
            bool adapterMatchesBackBuffer = (deviceCombo.BackBufferFormat == deviceCombo.AdapterFormat);
            bool adapterMatchesDesktop = (deviceCombo.AdapterFormat == adapterDesktopDisplayMode.Format);
            if (deviceCombo.IsWindowed)
            {
              continue;
            }

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
              {
                // This fullscreen device combo looks great -- take it
                goto EndFullscreenDeviceComboSearch;
              }
              // Otherwise keep looking for a better fullscreen device combo
            }
          }
        }
        //if (GUIGraphicsContext._useScreenSelector)
        //  break;// no need to loop again.. result would be the same
      }

    EndFullscreenDeviceComboSearch:
      if (bestDeviceCombo == null)
      {
        return false;
      }

      // Need to find a display mode on the best adapter that uses pBestDeviceCombo->AdapterFormat
      // and is as close to bestAdapterDesktopDisplayMode's res as possible

      int NumberOfFullscreenDisplayModes = 0;
      foreach (DisplayMode displayMode in bestAdapterInfo.DisplayModeList)
      {
        if (displayMode.Format != bestDeviceCombo.AdapterFormat)
        {
          continue;
        }
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
      _graphicsSettings.IsWindowed = false;

      if (_enumerationSettings.AppUsesDepthBuffer)
      {
        _graphicsSettings.FullscreenDepthStencilBufferFormat = (Format)bestDeviceCombo.DepthStencilFormatList[0];
      }
      int iQuality = 0; //bestDeviceCombo.MultisampleTypeList.Count-1;
      if (bestDeviceCombo.MultisampleTypeList.Count > 0)
        iQuality = bestDeviceCombo.MultisampleTypeList.Count - 1;
      _graphicsSettings.FullscreenMultisampleType = (MultisampleType)bestDeviceCombo.MultisampleTypeList[iQuality];
      _graphicsSettings.FullscreenMultisampleQuality = 0;
      _graphicsSettings.FullscreenVertexProcessingType = (VertexProcessingType)bestDeviceCombo.VertexProcessingTypeList[0];
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
        ServiceScope.Get<ILogger>().Critical("d3dSetup: failed to find best fullscreen mode.");
        return false;
      }
      if (!FindBestWindowedMode(false, false))
      {
        ServiceScope.Get<ILogger>().Critical("d3dSetup: failed to find best windowed mode.");
        return false;
      }
      return true;
    }

    /// <summary>
    /// Initialize the graphics environment
    /// </summary>
    public void InitializeEnvironment()
    {
      GraphicsAdapterInfo adapterInfo = _graphicsSettings.AdapterInfo;
      GraphicsDeviceInfo deviceInfo = _graphicsSettings.DeviceInfo;

      // Set up the presentation parameters
      BuildPresentParamsFromSettings();

      if ((deviceInfo.Caps.PrimitiveMiscCaps & PrimitiveMiscCaps.NullReference) != 0)
      {
        // Warn user about null ref device that can't render anything
        HandleSampleException(new NullReferenceDeviceException(), ApplicationMessage.None);
      }

      CreateFlags createFlags;
      if (_graphicsSettings.VertexProcessingType == VertexProcessingType.Software)
      {
        createFlags = CreateFlags.SoftwareVertexProcessing;
      }
      else if (_graphicsSettings.VertexProcessingType == VertexProcessingType.Mixed)
      {
        createFlags = CreateFlags.MixedVertexProcessing;
      }
      else if (_graphicsSettings.VertexProcessingType == VertexProcessingType.Hardware)
      {
        createFlags = CreateFlags.HardwareVertexProcessing;
      }
      else if (_graphicsSettings.VertexProcessingType == VertexProcessingType.PureHardware)
      {
        createFlags = CreateFlags.HardwareVertexProcessing; // | CreateFlags.PureDevice;
      }
      else
      {
        throw new ApplicationException();
      }

      // Make sure to allow multithreaded apps if we need them
      //presentParams.ForceNoMultiThreadedFlag = !isMultiThreaded;

      ServiceScope.Get<ILogger>().Info("DirectX: Using adapter: {0} {1} {2}",
              _graphicsSettings.AdapterOrdinal,
              MPDirect3D.Direct3D.Adapters[_graphicsSettings.AdapterOrdinal].Details.Description,
              _graphicsSettings.DevType);
      try
      {
        // Create the device
        //Device.IsUsingEventHandlers = false;

        GraphicsDevice.Device = new Device(MPDirect3D.Direct3D,
                                           _graphicsSettings.AdapterOrdinal,
                                           _graphicsSettings.DevType,
                                           _ourRenderTarget.Handle,
                                           createFlags | CreateFlags.Multithreaded,
                                           _presentParams);

        //GraphicsDevice.Device.DeviceResizing += _cancelEventHandler;
        // Cache our local objects
        //renderState = GraphicsDevice.Device.RenderState;
        //sampleState = GraphicsDevice.Device.SamplerState;
        //textureStates = GraphicsDevice.Device.TextureState;
        // When moving from fullscreen to windowed mode, it is important to
        // adjust the window size after recreating the device rather than
        // beforehand to ensure that you get the window size you want.  For
        // example, when switching from 640x480 fullscreen to windowed with
        // a 1000x600 window on a 1024x768 desktop, it is impossible to set
        // the window size to 1000x600 until after the display mode has
        // changed to 1024x768, because windows cannot be larger than the
        // desktop.
        //if (_windowed)
        {
          // Make sure main window isn't topmost, so error message is visible
          /*Size currentClientSize = this.ClientSize;

          this.Size = this.ClientSize;
          this.SendToBack();
          this.BringToFront();
          this.ClientSize = currentClientSize;
          this.TopMost = alwaysOnTop;*/
        }

        StringBuilder sb = new StringBuilder();


        // Store device description
        if (deviceInfo.DevType == DeviceType.Reference)
        {
          sb.Append("REF");
        }
        else if (deviceInfo.DevType == DeviceType.Hardware)
        {
          sb.Append("HAL");
        }
        else if (deviceInfo.DevType == DeviceType.Software)
        {
          sb.Append("SW");
        }

        /*
        if ((behaviorFlags.HardwareVertexProcessing) &&
            (behaviorFlags.PureDevice))
        {
          if (deviceInfo.DevType == DeviceType.Hardware)
          {
            sb.Append(" (pure hw vp)");
          }
          else
          {
            sb.Append(" (simulated pure hw vp)");
          }
        }
        else if (behaviorFlags.HardwareVertexProcessing)
        {
          if (deviceInfo.DevType == DeviceType.Hardware)
          {
            sb.Append(" (hw vp)");
          }
          else
          {
            sb.Append(" (simulated hw vp)");
          }
        }
        else if (behaviorFlags.MixedVertexProcessing)
        {
          if (deviceInfo.DevType == DeviceType.Hardware)
          {
            sb.Append(" (mixed vp)");
          }
          else
          {
            sb.Append(" (simulated mixed vp)");
          }
        }
        else if (behaviorFlags.SoftwareVertexProcessing)
        {
          sb.Append(" (sw vp)");
        }
        */
        if (deviceInfo.DevType == DeviceType.Hardware)
        {
          sb.Append(": ");
          sb.Append(adapterInfo.AdapterDetails.Description);
        }

        // Set device stats string
        _deviceStats = sb.ToString();
        ServiceScope.Get<ILogger>().Info("DirectX: {0}", _deviceStats);
        // Set up the fullscreen cursor
        /*if (showCursorWhenFullscreen && !windowed)
        {
          Cursor ourCursor = this.Cursor;
          GraphicsDevice.Device.SetCursor(ourCursor, true);
          GraphicsDevice.Device.ShowCursor(true);
        }

        // Confine cursor to fullscreen window
        if (clipCursorWhenFullscreen && !windowed)
        {
          Rectangle rcWindow = this.ClientRectangle;
        }*/

        // Setup the event handlers for our device
        //GraphicsDevice.Device.DeviceLost += new System.EventHandler(this.OnDeviceLost);
        //GraphicsDevice.Device.DeviceReset += new EventHandler(this.OnDeviceReset);
        //GraphicsDevice.Device.Disposing += new System.EventHandler(this.OnDeviceDisposing);
        //GraphicsDevice.Device.DeviceResizing += new System.ComponentModel.CancelEventHandler(this.EnvironmentResized);

        // Initialize the app's device-dependent objects
        try
        {
          //InitializeDeviceObjects();
          //OnDeviceReset(null, null);
          //active = true;
        }
        catch (Exception)
        {
          //Log.Error("D3D: InitializeDeviceObjects - Exception: {0}", ex.ToString());
          // Cleanup before we try again
          //OnDeviceLost(null, null);
          //OnDeviceDisposing(null, null);
          GraphicsDevice.Device.Dispose();
          GraphicsDevice.Device = null;
          //if (this.Disposing)
          //  return;
        }
      }
      catch (Exception)
      {
        // If that failed, fall back to the reference rasterizer
        if (deviceInfo.DevType == DeviceType.Hardware)
        {
          if (FindBestWindowedMode(false, true))
          {
            _graphicsSettings.IsWindowed = true;

            // Make sure main window isn't topmost, so error message is visible
            /*Size currentClientSize = this.ClientSize;
            this.Size = this.ClientSize;
            this.SendToBack();
            this.BringToFront();
            this.ClientSize = currentClientSize;
            this.TopMost = alwaysOnTop;*/

            // Let the user know we are switching from HAL to the reference rasterizer
            HandleSampleException(null, ApplicationMessage.WarnSwitchToRef);

            InitializeEnvironment();
          }
        }
      }
    }

    protected static void CancelAutoResizeEvent(object sender, CancelEventArgs e)
    {
      // Cancel, you stupid bastard!
      e.Cancel = true;
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
    public void HandleSampleException(SampleException e, ApplicationMessage Type)
    {
      // Build a message to display to the user
      string strMsg = "";
      if (e != null)
      {
        strMsg = e.Message;
      }
      //Log.Error("D3D: Exception: {0} {1} {2}", strMsg, strSource, strStack);
      if (ApplicationMessage.ApplicationMustExit == Type)
      {
        strMsg += "\n\nMediaPortal has to be closed.";
        MessageBox.Show(strMsg, "", MessageBoxButtons.OK, MessageBoxIcon.Error);

        // Close the window, which shuts down the app
        //if (this.IsHandleCreated)
        //  this.Close();
      }
      else
      {
        if (ApplicationMessage.WarnSwitchToRef == Type)
        {
          strMsg = "\n\nSwitching to the reference rasterizer,\n";
        }

        strMsg += "a software device that implements the entire\n";
        strMsg += "Direct3D feature set, but runs very slowly.";
        MessageBox.Show(strMsg, "", MessageBoxButtons.OK, MessageBoxIcon.Information);
      }
    }

    #region Various SampleExceptions

    /// <summary>
    /// The default sample exception type
    /// </summary>
    public class SampleException : ApplicationException
    {
      /// <summary>
      /// Return information about the exception
      /// </summary>
      public override string Message
      {
        get
        {
          string strMsg = "Generic application error. Enable\n";
          strMsg += "debug output for detailed information.";

          return strMsg;
        }
      }
    }


    /// <summary>
    /// Exception informing user no compatible devices were found
    /// </summary>
    public class NoCompatibleDevicesException : SampleException
    {
      /// <summary>
      /// Return information about the exception
      /// </summary>
      public override string Message
      {
        get
        {
          string strMsg = "This sample cannot run in a desktop\n";
          strMsg += "window with the current display settings.\n";
          strMsg += "Please change your desktop settings to a\n";
          strMsg += "16- or 32-bit display mode and re-run this\n";
          strMsg += "sample.";

          return strMsg;
        }
      }
    }


    /// <summary>
    /// An exception for when the ReferenceDevice is null
    /// </summary>
    public class NullReferenceDeviceException : SampleException
    {
      /// <summary>
      /// Return information about the exception
      /// </summary>
      public override string Message
      {
        get
        {
          string strMsg = "Warning: Nothing will be rendered.\n";
          strMsg += "The reference rendering device was selected, but your\n";
          strMsg += "computer only has a reduced-functionality reference device\n";
          strMsg += "installed. Please check if your graphics card and\n";
          strMsg += "drivers meet the minimum system requirements.\n";

          return strMsg;
        }
      }
    }


    /// <summary>
    /// An exception for when reset fails
    /// </summary>
    public class ResetFailedException : SampleException
    {
      /// <summary>
      /// Return information about the exception
      /// </summary>
      public override string Message
      {
        get
        {
          return "Could not reset the Direct3D device.";
        }
      }
    }


    /// <summary>
    /// The exception thrown when media couldn't be found
    /// </summary>
    public class MediaNotFoundException : SampleException
    {
      private readonly string mediaFile;

      public MediaNotFoundException(string filename)
      {
        mediaFile = filename;
      }

      public MediaNotFoundException()
      {
        mediaFile = string.Empty;
      }


      /// <summary>
      /// Return information about the exception
      /// </summary>
      public override string Message
      {
        get
        {
          string strMsg = "Could not load required media.";
          if (mediaFile.Length > 0)
          {
            strMsg += string.Format("\r\nFile: {0}", mediaFile);
          }

          return strMsg;
        }
      }
    }

    #endregion

    #region Native Methods

    /// <summary>
    /// Will hold native methods which are interop'd
    /// </summary>
    public class NativeMethods
    {
      #region Win32 User Messages / Structures

      /// <summary>Show window flags styles</summary>
      public enum ShowWindowFlags : uint
      {
        Hide = 0,
        ShowNormal = 1,
        Normal = 1,
        ShowMinimized = 2,
        ShowMaximized = 3,
        ShowNoActivate = 4,
        Show = 5,
        Minimize = 6,
        ShowMinNoActivate = 7,
        ShowNotActivated = 8,
        Restore = 9,
        ShowDefault = 10,
        ForceMinimize = 11,
      }


      /// <summary>Window styles</summary>
      [Flags]
      public enum WindowStyles : uint
      {
        Overlapped = 0x00000000,
        Popup = 0x80000000,
        Child = 0x40000000,
        Minimize = 0x20000000,
        Visible = 0x10000000,
        Disabled = 0x08000000,
        ClipSiblings = 0x04000000,
        ClipChildren = 0x02000000,
        Maximize = 0x01000000,
        Caption = 0x00C00000, /* WindowStyles.Border | WindowStyles.DialogFrame  */
        Border = 0x00800000,
        DialogFrame = 0x00400000,
        VerticalScroll = 0x00200000,
        HorizontalScroll = 0x00100000,
        SystemMenu = 0x00080000,
        ThickFrame = 0x00040000,
        Group = 0x00020000,
        TabStop = 0x00010000,
        MinimizeBox = 0x00020000,
        MaximizeBox = 0x00010000,
      }


      /// <summary>Peek message flags</summary>
      public enum PeekMessageFlags : uint
      {
        NoRemove = 0,
        Remove = 1,
        NoYield = 2,
      }


      /// <summary>Window messages</summary>
      public enum WindowMessage : uint
      {
        // Misc messages
        Destroy = 0x0002,
        Close = 0x0010,
        Quit = 0x0012,
        Paint = 0x000F,
        SetCursor = 0x0020,
        ActivateApplication = 0x001C,
        EnterMenuLoop = 0x0211,
        ExitMenuLoop = 0x0212,
        NonClientHitTest = 0x0084,
        PowerBroadcast = 0x0218,
        SystemCommand = 0x0112,
        GetMinMax = 0x0024,

        // Keyboard messages
        KeyDown = 0x0100,
        KeyUp = 0x0101,
        Character = 0x0102,
        SystemKeyDown = 0x0104,
        SystemKeyUp = 0x0105,
        SystemCharacter = 0x0106,

        // Mouse messages
        MouseMove = 0x0200,
        LeftButtonDown = 0x0201,
        LeftButtonUp = 0x0202,
        LeftButtonDoubleClick = 0x0203,
        RightButtonDown = 0x0204,
        RightButtonUp = 0x0205,
        RightButtonDoubleClick = 0x0206,
        MiddleButtonDown = 0x0207,
        MiddleButtonUp = 0x0208,
        MiddleButtonDoubleClick = 0x0209,
        MouseWheel = 0x020a,
        XButtonDown = 0x020B,
        XButtonUp = 0x020c,
        XButtonDoubleClick = 0x020d,
        MouseFirst = LeftButtonDown, // Skip mouse move, it happens a lot and there is another message for that
        MouseLast = XButtonDoubleClick,

        // Sizing
        EnterSizeMove = 0x0231,
        ExitSizeMove = 0x0232,
        Size = 0x0005,
      }


      /// <summary>Mouse buttons</summary>
      public enum MouseButtons
      {
        Left = 0x0001,
        Right = 0x0002,
        Middle = 0x0010,
        Side1 = 0x0020,
        Side2 = 0x0040,
      }


      /// <summary>Windows Message</summary>
      [StructLayout(LayoutKind.Sequential)]
      public struct Message
      {
        public IntPtr hWnd;
        public WindowMessage msg;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public Point p;
      }


      /// <summary>MinMax Info structure</summary>
      [StructLayout(LayoutKind.Sequential)]
      public struct MinMaxInformation
      {
        public Point reserved;
        public Point MaxSize;
        public Point MaxPosition;
        public Point MinTrackSize;
        public Point MaxTrackSize;
      }


      /// <summary>Monitor Info structure</summary>
      [StructLayout(LayoutKind.Sequential)]
      public struct MonitorInformation
      {
        public uint Size; // Size of this structure
        public Rectangle MonitorRectangle;
        public Rectangle WorkRectangle;
        public uint Flags; // Possible flags
      }


      /// <summary>Window class structure</summary>
      [StructLayout(LayoutKind.Sequential)]
      public struct WindowClass
      {
        public int Styles;
        [MarshalAs(UnmanagedType.FunctionPtr)]
        public WndProcDelegate WindowsProc;
        private int ExtraClassData;
        private int ExtraWindowData;
        public IntPtr InstanceHandle;
        public IntPtr IconHandle;
        public IntPtr CursorHandle;
        public IntPtr backgroundBrush;
        [MarshalAs(UnmanagedType.LPTStr)]
        public string MenuName;
        [MarshalAs(UnmanagedType.LPTStr)]
        public string ClassName;
      }

      #endregion

      #region Delegates

      public delegate IntPtr WndProcDelegate(IntPtr hWnd, WindowMessage msg, IntPtr wParam, IntPtr lParam);

      #endregion

      #region Windows API calls

      [SuppressUnmanagedCodeSecurity] // We won't use this maliciously
      [DllImport("winmm.dll")]
      public static extern IntPtr timeBeginPeriod(uint period);

      [SuppressUnmanagedCodeSecurity] // We won't use this maliciously
      [DllImport("User32.dll", CharSet = CharSet.Auto)]
      public static extern bool PeekMessage(out Message msg, IntPtr hWnd, uint messageFilterMin, uint messageFilterMax,
                                            PeekMessageFlags flags);

      [SuppressUnmanagedCodeSecurity] // We won't use this maliciously
      [DllImport("User32.dll", CharSet = CharSet.Auto)]
      public static extern bool TranslateMessage(ref Message msg);

      [SuppressUnmanagedCodeSecurity] // We won't use this maliciously
      [DllImport("User32.dll", CharSet = CharSet.Auto)]
      public static extern bool DispatchMessage(ref Message msg);

      [SuppressUnmanagedCodeSecurity] // We won't use this maliciously
      [DllImport("User32.dll", CharSet = CharSet.Auto)]
      public static extern IntPtr DefWindowProc(IntPtr hWnd, WindowMessage msg, IntPtr wParam, IntPtr lParam);

      [SuppressUnmanagedCodeSecurity] // We won't use this maliciously
      [DllImport("User32.dll", CharSet = CharSet.Auto)]
      public static extern void PostQuitMessage(int exitCode);

      [SuppressUnmanagedCodeSecurity] // We won't use this maliciously
      [DllImport("User32.dll", CharSet = CharSet.Auto)]
#if(_WIN64)
  private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int index, [MarshalAs(UnmanagedType.FunctionPtr)] WndProcDelegate windowCallback);
#else
      private static extern IntPtr SetWindowLong(IntPtr hWnd, int index,
                                                 [MarshalAs(UnmanagedType.FunctionPtr)] WndProcDelegate windowCallback);
#endif

      [SuppressUnmanagedCodeSecurity] // We won't use this maliciously
      [DllImport("User32.dll", EntryPoint = "SetWindowLong", CharSet = CharSet.Auto)]
      private static extern IntPtr SetWindowLongStyle(IntPtr hWnd, int index, WindowStyles style);

      [SuppressUnmanagedCodeSecurity] // We won't use this maliciously
      [DllImport("User32.dll", EntryPoint = "GetWindowLong", CharSet = CharSet.Auto)]
      private static extern WindowStyles GetWindowLongStyle(IntPtr hWnd, int index);

      [SuppressUnmanagedCodeSecurity] // We won't use this maliciously
      [DllImport("kernel32")]
      public static extern bool QueryPerformanceFrequency(ref long PerformanceFrequency);

      [SuppressUnmanagedCodeSecurity] // We won't use this maliciously
      [DllImport("kernel32")]
      public static extern bool QueryPerformanceCounter(ref long PerformanceCount);

      [SuppressUnmanagedCodeSecurity] // We won't use this maliciously
      [DllImport("User32.dll", CharSet = CharSet.Auto)]
      public static extern bool GetClientRect(IntPtr hWnd, out Rectangle rect);

      [SuppressUnmanagedCodeSecurity] // We won't use this maliciously
      [DllImport("User32.dll", CharSet = CharSet.Auto)]
      public static extern bool GetWindowRect(IntPtr hWnd, out Rectangle rect);

      [SuppressUnmanagedCodeSecurity] // We won't use this maliciously
      [DllImport("User32.dll", CharSet = CharSet.Auto)]
      public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndAfter, int x, int y, int w, int h, uint flags);

      [SuppressUnmanagedCodeSecurity] // We won't use this maliciously
      [DllImport("User32.dll", CharSet = CharSet.Auto)]
      public static extern bool ScreenToClient(IntPtr hWnd, ref Point rect);

      [SuppressUnmanagedCodeSecurity] // We won't use this maliciously
      [DllImport("User32.dll", CharSet = CharSet.Auto)]
      public static extern IntPtr SetFocus(IntPtr hWnd);

      [SuppressUnmanagedCodeSecurity] // We won't use this maliciously
      [DllImport("User32.dll", CharSet = CharSet.Auto)]
      public static extern IntPtr GetParent(IntPtr hWnd);

      [SuppressUnmanagedCodeSecurity] // We won't use this maliciously
      [DllImport("User32.dll", CharSet = CharSet.Auto)]
      public static extern bool GetMonitorInfo(IntPtr hWnd, ref MonitorInformation info);

      [SuppressUnmanagedCodeSecurity] // We won't use this maliciously
      [DllImport("User32.dll", CharSet = CharSet.Auto)]
      public static extern IntPtr MonitorFromWindow(IntPtr hWnd, uint flags);

      [SuppressUnmanagedCodeSecurity] // We won't use this maliciously
      [DllImport("User32.dll", CharSet = CharSet.Auto)]
      public static extern short GetAsyncKeyState(uint key);

      [SuppressUnmanagedCodeSecurity] // We won't use this maliciously
      [DllImport("User32.dll", CharSet = CharSet.Auto)]
      public static extern IntPtr SetCapture(IntPtr handle);

      [SuppressUnmanagedCodeSecurity] // We won't use this maliciously
      [DllImport("User32.dll", CharSet = CharSet.Auto)]
      public static extern bool ReleaseCapture();

      [SuppressUnmanagedCodeSecurity] // We won't use this maliciously
      [DllImport("User32.dll", CharSet = CharSet.Auto)]
      public static extern bool ShowWindow(IntPtr hWnd, ShowWindowFlags flags);

      [SuppressUnmanagedCodeSecurity] // We won't use this maliciously
      [DllImport("User32.dll", CharSet = CharSet.Auto)]
      public static extern bool SetMenu(IntPtr hWnd, IntPtr menuHandle);

      [SuppressUnmanagedCodeSecurity] // We won't use this maliciously
      [DllImport("User32.dll", CharSet = CharSet.Auto)]
      public static extern bool DestroyWindow(IntPtr hWnd);

      [SuppressUnmanagedCodeSecurity] // We won't use this maliciously
      [DllImport("User32.dll", CharSet = CharSet.Auto)]
      public static extern bool IsIconic(IntPtr hWnd);

      [SuppressUnmanagedCodeSecurity] // We won't use this maliciously
      [DllImport("User32.dll", CharSet = CharSet.Auto)]
      public static extern bool AdjustWindowRect(ref Rectangle rect, WindowStyles style,
                                                 [MarshalAs(UnmanagedType.Bool)] bool menu);

      [SuppressUnmanagedCodeSecurity] // We won't use this maliciously
      [DllImport("User32.dll", CharSet = CharSet.Auto)]
      public static extern IntPtr SendMessage(IntPtr windowHandle, WindowMessage msg, IntPtr w, IntPtr l);

      [SuppressUnmanagedCodeSecurity] // We won't use this maliciously
      [DllImport("User32.dll", CharSet = CharSet.Auto)]
      public static extern IntPtr RegisterClass(ref WindowClass wndClass);

      [SuppressUnmanagedCodeSecurity] // We won't use this maliciously
      [DllImport("User32.dll", CharSet = CharSet.Auto)]
      public static extern bool UnregisterClass([MarshalAs(UnmanagedType.LPTStr)] string className,
                                                IntPtr instanceHandle);

      [SuppressUnmanagedCodeSecurity] // We won't use this maliciously
      [DllImport("User32.dll", EntryPoint = "CreateWindowEx", CharSet = CharSet.Auto)]
      public static extern IntPtr CreateWindow(int exStyle, [MarshalAs(UnmanagedType.LPTStr)] string className,
                                               [MarshalAs(UnmanagedType.LPTStr)] string windowName,
                                               WindowStyles style, int x, int y, int width, int height, IntPtr parent,
                                               IntPtr menuHandle, IntPtr instanceHandle, IntPtr zero);

      [SuppressUnmanagedCodeSecurity] // We won't use this maliciously
      [DllImport("User32.dll", CharSet = CharSet.Auto)]
      public static extern int GetCaretBlinkTime();

      #endregion

      #region Class Methods

      private NativeMethods() { } // No creation
      /// <summary>Hooks window messages to go through this new callback</summary>
      public static void HookWindowsMessages(IntPtr window, WndProcDelegate callback)
      {
#if(_WIN64)
  SetWindowLongPtr(window, -4, callback);
#else
        SetWindowLong(window, -4, callback);
#endif
      }

      /// <summary>Set new window style</summary>
      public static void SetStyle(IntPtr window, WindowStyles newStyle)
      {
        SetWindowLongStyle(window, -16, newStyle);
      }

      /// <summary>Get new window style</summary>
      public static WindowStyles GetStyle(IntPtr window)
      {
        return GetWindowLongStyle(window, -16);
      }

      /// <summary>Returns the low word</summary>
      public static short LoWord(uint l)
      {
        return (short)(l & 0xffff);
      }

      /// <summary>Returns the high word</summary>
      public static short HiWord(uint l)
      {
        return (short)(l >> 16);
      }

      /// <summary>Makes two shorts into a long</summary>
      public static uint MakeUInt32(short l, short r)
      {
        return (uint)((l & 0xffff) | ((r & 0xffff) << 16));
      }

      /// <summary>Is this key down right now</summary>
      public static bool IsKeyDown(Keys key)
      {
        return (GetAsyncKeyState((int)Keys.ShiftKey) & 0x8000) != 0;
      }

      #endregion
    }

    /// <summary>
    /// Build presentation parameters from the current settings
    /// </summary>
    public void BuildPresentParamsFromSettings()
    {
      _presentParams.Windowed = _graphicsSettings.IsWindowed;
      _presentParams.BackBufferCount = 2;

      // Z order must be enabled for batching to work
      if (MediaPortal.UI.SkinEngine.SkinManagement.SkinContext.UseBatching == true)
      {
        _presentParams.EnableAutoDepthStencil = true;
        _presentParams.AutoDepthStencilFormat = Format.D24X8;
      }
      else
      {
        _presentParams.EnableAutoDepthStencil = false;
      }

      ServiceScope.Get<ILogger>().Debug("BuildPresentParamsFromSettings windowed {0} {1} {2}",
        _graphicsSettings.IsWindowed, _ourRenderTarget.ClientRectangle.Width, _ourRenderTarget.ClientRectangle.Height);

      if (_graphicsSettings.IsWindowed)
      {
        _presentParams.Multisample = MultisampleType.None;// _graphicsSettings.WindowedMultisampleType;
        _presentParams.MultisampleQuality = 0;// _graphicsSettings.WindowedMultisampleQuality;
        //_presentParams.AutoDepthStencilFormat = _graphicsSettings.WindowedDepthStencilBufferFormat;
        _presentParams.BackBufferWidth = _ourRenderTarget.ClientRectangle.Width;
        _presentParams.BackBufferHeight = _ourRenderTarget.ClientRectangle.Height;
        _presentParams.BackBufferFormat = _graphicsSettings.BackBufferFormat;
#if PROFILE_PERFORMANCE
        _presentParams.PresentationInterval = PresentInterval.Immediate; // Immediate.Default;
#else
        if (_usingPerfHud)
          _presentParams.PresentationInterval = PresentInterval.Immediate;
        else
        _presentParams.PresentationInterval = PresentInterval.Default; // Immediate.Default;
#endif
        _presentParams.FullScreenRefreshRateInHertz = 0;
        _presentParams.SwapEffect = SwapEffect.Discard;
        _presentParams.PresentFlags = PresentFlags.Video; //PresentFlag.LockableBackBuffer;
        _presentParams.DeviceWindowHandle = _ourRenderTarget.Handle;
        _presentParams.Windowed = true;
        //         _presentParams.PresentationInterval = PresentInterval.Immediate;
      }
      else
      {
        _presentParams.Multisample = _graphicsSettings.FullscreenMultisampleType;
        _presentParams.MultisampleQuality = _graphicsSettings.FullscreenMultisampleQuality;
        //_presentParams.AutoDepthStencilFormat = _graphicsSettings.FullscreenDepthStencilBufferFormat;

        _presentParams.BackBufferWidth = _graphicsSettings.DisplayMode.Width;
        _presentParams.BackBufferHeight = _graphicsSettings.DisplayMode.Height;
        _presentParams.BackBufferFormat = _graphicsSettings.DeviceCombo.BackBufferFormat;
        
#if PROFILE_PERFORMANCE
        _presentParams.PresentationInterval = PresentInterval.Immediate; // Immediate.Default;
#else
        if (_usingPerfHud)
          _presentParams.PresentationInterval = PresentInterval.Immediate;
        else
          _presentParams.PresentationInterval = PresentInterval.Default;
#endif
        _presentParams.FullScreenRefreshRateInHertz = _graphicsSettings.DisplayMode.RefreshRate;
        _presentParams.SwapEffect = SwapEffect.Discard;
        _presentParams.PresentFlags = PresentFlags.Video; //|PresentFlag.LockableBackBuffer;
        _presentParams.DeviceWindowHandle = _window.Handle;
        _presentParams.Windowed = false;
      }
    }

    public void SwitchExlusiveOrWindowed(bool exclusiveMode, string displaySetting)
    {
      _graphicsSettings.IsWindowed = !exclusiveMode;
      if (exclusiveMode)
      {
        DisplayMode mode = ToDisplayMode(displaySetting);
        ServiceScope.Get<ILogger>().Debug("SwitchExlusiveOrWindowed  {0} {1} {2}", mode.Width, mode.Height, mode.RefreshRate);

        for (int i = 0; i < _graphicsSettings.FullscreenDisplayModes.Length; i++)
        {
          DisplayMode compareMode = _graphicsSettings.FullscreenDisplayModes[i];
          if ((compareMode.Width == mode.Width) && (compareMode.Height == mode.Height) &&
              (compareMode.RefreshRate == mode.RefreshRate))
          {
            _graphicsSettings.CurrentFullscreenDisplayMode = i;
            break;
          }
        }
      }

      Trace.WriteLine("----switch----");
      BuildPresentParamsFromSettings();
      //GraphicsDevice.Device.DeviceResizing -= _cancelEventHandler;

      Result result = GraphicsDevice.Device.Reset(_presentParams);

      if (result == ResultCode.DeviceLost)
      {
        result = GraphicsDevice.Device.TestCooperativeLevel();
        // Loop until it's ok to reset
        while (result == ResultCode.DeviceLost)
        {
          Thread.Sleep(10);
          result = GraphicsDevice.Device.TestCooperativeLevel();
        }
        GraphicsDevice.Device.Reset(_presentParams);
      }
    }

    public void Reset()
    {
      try
      {
        //GraphicsDevice.Device.DeviceResizing -= _cancelEventHandler;
        GraphicsDevice.Device.Reset(_presentParams);
      }
      finally
      {
        //GraphicsDevice.Device.DeviceResizing += _cancelEventHandler;
      }
    }

    protected static DisplayMode ToDisplayMode(string mode)
    {
      char[] delimiterChars = { 'x', '@' };
      string[] words = mode.Split(delimiterChars);
      DisplayMode result = new DisplayMode();
      result.Width = Int32.Parse(words[0]);
      result.Height = Int32.Parse(words[1]);
      result.RefreshRate = Int32.Parse(words[2]);
      return result;
    }

    #endregion
  }
}
