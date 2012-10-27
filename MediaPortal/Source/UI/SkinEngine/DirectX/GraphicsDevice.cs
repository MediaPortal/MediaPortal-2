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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.UI.SkinEngine.ContentManagement;
using MediaPortal.UI.SkinEngine.DirectX.RenderStrategy;
using MediaPortal.UI.SkinEngine.ScreenManagement;
using MediaPortal.UI.SkinEngine.Utils;
using SlimDX;
using SlimDX.Direct3D9;

namespace MediaPortal.UI.SkinEngine.DirectX
{
  public delegate void WorkDlgt();

  internal static class GraphicsDevice
  {
    #region Variables

    private static readonly D3DSetup _setup = new D3DSetup();
    private static readonly ReaderWriterLockSlim _renderAndResourceAccessLock = new ReaderWriterLockSlim();
    private static DeviceEx _device;
    private static Surface _backBuffer;
    private static bool _deviceOk = true;
    private static DxCapabilities _dxCapabilities = null;
    private static ScreenManager _screenManager = null;

    #endregion

    public static Matrix TransformWorld;
    public static Matrix TransformView;
    public static Matrix TransformProjection;
    public static Matrix FinalTransform;

    // Render process related events
    public static event EventHandler DeviceSceneBegin;
    public static event EventHandler DeviceSceneEnd;
    public static event EventHandler DeviceScenePresented;

    // RenderModeType related fields
    private static int _currentRenderStrategyIndex = 0;
    private static List<IRenderStrategy> _renderStrategies;

    /// <summary>
    /// Returns the information if the graphics device is healthy, which means it was neither lost nor hung nor removed.
    /// </summary>
    /// <remarks>
    /// If this property is <c>false</c>, <see cref="ReclaimDevice"/> must be called.
    /// </remarks>
    public static bool DeviceOk
    {
      get { return _deviceOk; }
    }

    /// <summary>
    /// Returns the target rendering target framerate. This value can be changed according to screen refresh rate or video fps using
    /// one of the methods <see cref="AdaptTargetFrameRateToDisplayMode"/> or <see cref="SetFrameRate"/>.
    /// </summary>
    public static double TargetFrameRate
    {
      get { return RenderStrategy.TargetFrameRate; }
    }

    /// <summary>
    /// Returns the desired time per frame in ms.
    /// </summary>
    public static double MsPerFrame
    {
      get { return RenderStrategy.MsPerFrame; }
    }

    public static ScreenManager ScreenManager
    {
      get { return _screenManager; }
      internal set { _screenManager = value; }
    }

    /// <summary>
    /// Gets the DirectX device which is used to render.
    /// </summary>
    public static DeviceEx Device
    {
      get { return _device; }
    }

    /// <summary>
    /// Gets the DirectX back-buffer width.
    /// </summary>
    public static int Width
    {
      get { return _setup.PresentParameters.BackBufferWidth; }
    }

    /// <summary>
    /// Gets the DirectX back-buffer height.
    /// </summary>
    public static int Height
    {
      get { return _setup.PresentParameters.BackBufferHeight; }
    }

    public static DxCapabilities DxCapabilities
    {
      get { return _dxCapabilities; }
    }

    public static D3DSetup Setup
    {
      get { return _setup; }
    }

    public static DisplayMode CurrentDisplayMode
    {
      get
      {
        Capabilities deviceCapabilities = _device.Capabilities;
        int ordinal = deviceCapabilities.AdapterOrdinal;
        AdapterInformation adapterInfo = MPDirect3D.Direct3D.Adapters[ordinal];
        return adapterInfo.CurrentDisplayMode;
      }
    }

    /// <summary>
    /// Lock to be used during DirectX (and maybe also other) resource access and during rendering.
    /// </summary>
    public static ReaderWriterLockSlim RenderAndResourceAccessLock
    {
      get { return _renderAndResourceAccessLock; }
    }

    public static void ExecuteInMainThread(WorkDlgt method)
    {
      _setup.RenderTarget.Invoke(method);
    }

    public static void ReCreateDXDevice()
    {
      _screenManager.ExecuteWithTempReleasedResources(() => ExecuteInMainThread(DoReCreateDevice_MainThread));
    }

    /// <summary>
    /// Calls <see cref="DeviceEx.CheckDeviceState"/> on the DX device and returns its result.
    /// </summary>
    internal static DeviceState CheckDeviceState()
    {
      return _device.CheckDeviceState(_setup.RenderTarget.Handle);
    }

    /// <summary>
    /// Initializes or re-initializes the DirectX device and the backbuffer. This is necessary in the initialization phase
    /// of the SkinEngine and after a parameter was changed which affects the DX device creation.
    /// </summary>
    /// <remarks>
    /// This method has to be called from the main application thread because the DirectX device will be created by this method.
    /// </remarks>
    /// <param name="window">The window which is being used as render target; that window will contain the DX device.</param>
    internal static void Initialize_MainThread(Form window)
    {
      _setup.RenderTarget = window;
      DoReCreateDevice_MainThread();
    }

    /// <summary>
    /// Creates or re-creates the DirectX device and the backbuffer. This is necessary in the initialization phase
    /// of the SkinEngine and after a parameter was changed which affects the DX device creation.
    /// </summary>
    internal static void DoReCreateDevice_MainThread()
    {
      try
      {
        // Note that only the thread which handles window messages is allowed to call CreateDevice and Reset
        // (see http://msdn.microsoft.com/en-us/library/windows/desktop/bb147224%28v=vs.85%29.aspx )
        ServiceRegistration.Get<ILogger>().Debug("GraphicsDevice: Initializing DirectX");
        MPDirect3D.Load();

        // Cleanup-part: Only necessary during re-initialization
        UIResourcesHelper.ReleaseUIResources();
        if (_backBuffer != null)
          _backBuffer.Dispose();
        if (_device != null)
          _device.Dispose();
        _device = _setup.SetupDirectX();
        // End cleanup part

        SetupRenderStrategies();

        Capabilities deviceCapabilities = _device.Capabilities;
        _backBuffer = _device.GetRenderTarget(0);
        int ordinal = deviceCapabilities.AdapterOrdinal;
        AdapterInformation adapterInfo = MPDirect3D.Direct3D.Adapters[ordinal];
        DisplayMode currentMode = adapterInfo.CurrentDisplayMode;
        AdaptTargetFrameRateToDisplayMode(currentMode);
        LogScreenMode(currentMode);
        bool firstTimeInitialization = _dxCapabilities == null;
        _dxCapabilities = DxCapabilities.RequestCapabilities(deviceCapabilities, currentMode);
        if (firstTimeInitialization)
        {
          if (!_dxCapabilities.SupportsShaders)
          {
            string text = String.Format("MediaPortal 2 needs a graphics card wich supports shader model 2.0\nYour card does NOT support this.\nMediaportal 2 will continue but migh run slow");
            MessageBox.Show(text, "GraphicAdapter", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
          }
        }
        SetRenderState();
        UIResourcesHelper.ReallocUIResources();
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Critical("GraphicsDevice: Failed to setup DirectX", ex);
        Environment.Exit(0);
      }
    }

    /// <summary>
    /// Setups all <see cref="IRenderStrategy"/>s.
    /// </summary>
    private static void SetupRenderStrategies()
    {
      _renderStrategies = new List<IRenderStrategy>
        {
          new Default(_setup), 
          new VSync(_setup), 
          new MaxPerformance(_setup)
        };
      if (_setup.IsMultiSample)
        _renderStrategies.RemoveAll(s => !s.IsMultiSampleCompatible);
      _currentRenderStrategyIndex = 0;
    }

    private static void LogScreenMode(DisplayMode mode)
    {
      ServiceRegistration.Get<ILogger>().Info("GraphicsDevice: DirectX initialized {0}x{1} (format: {2} {3} Hz)", Width,
          Height, mode.Format, TargetFrameRate);
    }

    /// <summary>
    /// Gets the current <see cref="IRenderStrategy"/>.
    /// </summary>
    public static IRenderStrategy RenderStrategy
    {
      get { return _renderStrategies[_currentRenderStrategyIndex]; }
    }

    /// <summary>
    /// Switches through all possible RenderStrategies.
    /// </summary>
    public static void NextRenderStrategy()
    {
      _currentRenderStrategyIndex = (_currentRenderStrategyIndex + 1) % _renderStrategies.Count;
      LogScreenMode(CurrentDisplayMode);
    }

    internal static void Dispose()
    {
      if (_backBuffer != null)
        _backBuffer.Dispose();
      _backBuffer = null;

      if (_device != null)
        _device.Dispose();
      _device = null;
      MPDirect3D.Unload();
      _renderAndResourceAccessLock.Dispose();
    }

    public static void SetFrameRate(double frameRate)
    {
      RenderStrategy.SetTargetFrameRate(frameRate);
    }

    private static void AdaptTargetFrameRateToDisplayMode(DisplayMode displayMode)
    {
      SetFrameRate(displayMode.RefreshRate);
    }

    /// <summary>
    /// Resets the DirectX device. This will release all screens, other UI resources and our back buffer, reset the DX device and realloc
    /// all resources.
    /// </summary>
    public static bool Reset()
    {
      ServiceRegistration.Get<ILogger>().Warn("GraphicsDevice: Resetting DX device...");
      _screenManager.ExecuteWithTempReleasedResources(() => ExecuteInMainThread(() =>
          {
            // Note that the thread which created the device must call this (see http://msdn.microsoft.com/en-us/library/windows/desktop/bb174344%28v=vs.85%29.aspx ).
            // Note also that only the thread which handles window messages is allowed to call CreateDevice and Reset
            // (see http://msdn.microsoft.com/en-us/library/windows/desktop/bb147224%28v=vs.85%29.aspx )
            ServiceRegistration.Get<ILogger>().Debug("GraphicsDevice: Reset DirectX");
            UIResourcesHelper.ReleaseUIResources();

            if (ContentManager.Instance.TotalAllocationSize != 0)
              ServiceRegistration.Get<ILogger>().Warn("GraphicsDevice: ContentManager.TotalAllocationSize = {0}, should be 0!", ContentManager.Instance.TotalAllocationSize / (1024 * 1024));

            if (_backBuffer != null)
              _backBuffer.Dispose();
            _backBuffer = null;

            _setup.BuildPresentParamsFromSettings();
            _device.ResetEx(_setup.PresentParameters);

            SetupRenderStrategies();

            Capabilities deviceCapabilities = _device.Capabilities;
            int ordinal = deviceCapabilities.AdapterOrdinal;
            AdapterInformation adapterInfo = MPDirect3D.Direct3D.Adapters[ordinal];
            DisplayMode currentMode = adapterInfo.CurrentDisplayMode;
            AdaptTargetFrameRateToDisplayMode(currentMode);
            ServiceRegistration.Get<ILogger>().Debug("GraphicsDevice: DirectX reset {0}x{1} format: {2} {3} Hz", Width, Height,
                currentMode.Format, TargetFrameRate);
            _backBuffer = _device.GetRenderTarget(0);
            _dxCapabilities = DxCapabilities.RequestCapabilities(deviceCapabilities, currentMode);

            ScreenRefreshWorkaround();

            UIResourcesHelper.ReallocUIResources();
          }));
      ServiceRegistration.Get<ILogger>().Warn("GraphicsDevice: Device successfully reset");
      return true;
    }

    /// <summary>
    /// This workaround is required for changing the AntiAliasing setting from "None" to any AA mode. The rendering stalls until the window is moved,
    /// the focus is lost and gained or the window size changed. So we work around that problem by changing the size of the MainForm temporary,
    /// so the screen get's refreshed properly.
    /// </summary>
    /// <remarks>
    /// To reproduce the problem, remove the call to this method, go to the AntiAliasing configuration (Settings/Appearance/GUI antialiasing)
    /// and check that the AA mode "None" is currently active. If you now switch to any other AA mode than "None", the last rendered GUI image
    /// remains on screen but isn't updated any more. If you move the mouse over the position where buttons are located, you hear button sounds,
    /// but you don't see an up-to-date image.
    /// The problem only occurs when you switch from AA None to any other AA mode, but switching between different AA levels is working fine!
    /// TODO: Find a proper solution and remove this workaround.
    /// </remarks>
    private static void ScreenRefreshWorkaround()
    {
      Form target = _setup.RenderTarget;
      Size oldSize = target.ClientSize;
      target.ClientSize = new Size(oldSize.Width, oldSize.Height + 1);
      target.ClientSize = new Size(oldSize.Width, oldSize.Height);
    }

    /// <summary>
    /// Reclaims the DirectX device if it has been lost (see <see cref="DeviceOk"/>).
    /// </summary>
    /// <returns><c>true</c>, if the device could successfully be reclaimed. In this case, <see cref="DeviceOk"/> will be reset to
    /// <c>false</c>. <c>false</c>, if the device could not be reclaimed.</returns>
    public static bool ReclaimDevice()
    {
      try
      {
        // Handling is implemented based on http://msdn.microsoft.com/en-us/library/windows/desktop/bb172554%28v=vs.85%29.aspx
        switch (CheckDeviceState())
        {
          case DeviceState.Ok:
            _deviceOk = true;
            break;
          case DeviceState.DeviceLost:
          case DeviceState.DeviceHung:
            Reset();
            _deviceOk = CheckDeviceState() == DeviceState.Ok;
            break;
          case DeviceState.DeviceRemoved:
            // Albert, 2012-02-26: Note that this code path is not verified/tested yet. The description of D3DERR_DEVICEREMOVED in
            // http://msdn.microsoft.com/en-us/library/windows/desktop/bb172554%28v=vs.85%29.aspx says the device must be re-created,
            // so we do that here. I'm not sure if we maybe must re-create our IDirect3D9Ex object and do all the initialization work
            // again, according to http://msdn.microsoft.com/en-us/library/bb219800%28v=vs.85%29.aspx#Lost_Device_Behavior_Changes
            ReCreateDXDevice();
            _deviceOk = CheckDeviceState() == DeviceState.Ok;
            break;
          default:
            _deviceOk = false;
            break;
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Warn("GraphicsDevice: Reclaiming DX device failed", ex);
        _deviceOk = false;
      }
      return !_deviceOk;
    }

    /// <summary>
    /// Sets the directx render states and project matrices.
    /// </summary>
    public static void SetRenderState()
    {
      _device.SetRenderState(RenderState.CullMode, Cull.None);
      _device.SetRenderState(RenderState.Lighting, false);

      _device.SetRenderState(RenderState.ZEnable, false);
      _device.SetRenderState(RenderState.ZWriteEnable, false);

      _device.SetRenderState(RenderState.FillMode, FillMode.Solid);
      _device.SetRenderState(RenderState.AlphaBlendEnable, true);
      _device.SetRenderState(RenderState.SourceBlend, Blend.One);
      _device.SetRenderState(RenderState.DestinationBlend, Blend.InverseSourceAlpha);

      if (_dxCapabilities.SupportsAlphaBlend)
      {
        _device.SetRenderState(RenderState.AlphaTestEnable, true);
        _device.SetRenderState(RenderState.AlphaRef, 0x05);
        _device.SetRenderState(RenderState.AlphaFunc, Compare.GreaterEqual);
      }
      for (int sampler = 0; sampler < 3; sampler++)
      {
        _device.SetSamplerState(sampler, SamplerState.MinFilter, TextureFilter.Linear);
        _device.SetSamplerState(sampler, SamplerState.MagFilter, TextureFilter.Linear);
        _device.SetSamplerState(sampler, SamplerState.MipFilter, TextureFilter.None);
      }

      // Projection onto screen space
      SetCameraProjection(Width, Height);
    }

    /// <summary>
    /// Creates and the camera and projection matrices use in future rendering. FinalTransform is 
    /// updated to reflect this change.
    /// </summary>
    /// <param name="width">The width of the desired screen-space.</param>
    /// <param name="height">The height of the desired screen-space.</param>
    public static void SetCameraProjection(int width, int height)
    {
      float w = width * 0.5f;
      float h = height * 0.5f;

      // Setup a 2D camera view
      Matrix flipY = Matrix.Scaling(1.0f, -1.0f, 1.0f);
      Matrix translate = Matrix.Translation(-w, -h, 0.0f);
      TransformView = Matrix.Multiply(translate, flipY);

      TransformProjection = Matrix.OrthoOffCenterLH(-w, w, -h, h, 0.0f, 2.0f);
      FinalTransform = TransformView * TransformProjection;
    }
    
    /// <summary>
    /// Fires an event if listeners are available.
    /// </summary>
    /// <param name="eventHandler"></param>
    private static void Fire(EventHandler eventHandler)
    {
      try
      {
        if (eventHandler != null)
          eventHandler(null, EventArgs.Empty);
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("Error executing render event handler:", e);
      }
    }

    /// <summary>
    /// Renders the entire scene.
    /// </summary>
    /// <param name="doWaitForNextFame"><c>true</c>, if this method should wait to the correct frame start time
    /// before it renders, else <c>false</c>.</param>
    /// <returns><c>true</c>, if the caller should wait some milliseconds before rendering the next time.</returns>
    public static bool Render(bool doWaitForNextFame)
    {
      if (_device == null || !_deviceOk)
        return true;

      RenderStrategy.BeginRender(doWaitForNextFame);

      _renderAndResourceAccessLock.EnterReadLock();
      try
      {
        _device.Clear(ClearFlags.Target, Color.Black, 1.0f, 0);
        _device.BeginScene();

        Fire(DeviceSceneBegin);

        _screenManager.Render();

        Fire(DeviceSceneEnd);

        _device.EndScene();

        _device.PresentEx(RenderStrategy.PresentMode);

        Fire(DeviceScenePresented);

        ContentManager.Instance.Clean();
      }
      catch (Direct3D9Exception e)
      {
        DeviceState state = CheckDeviceState();
        ServiceRegistration.Get<ILogger>().Warn("GraphicsDevice: DirectX Exception, DeviceState: {0}", e, state);
        _deviceOk = state == DeviceState.Ok;
        return !_deviceOk;
      }
      finally
      {
        _renderAndResourceAccessLock.ExitReadLock();
      }
      return false;
    }
  }
}
