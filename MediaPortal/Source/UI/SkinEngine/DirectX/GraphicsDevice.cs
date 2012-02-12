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

// Define MAX_FRAMERATE to avoid MP from targeting a fixed framerate. With MAX_FRAMERATE defined,
// the system will output as many frames as possible. But video playback might produce wrong frames with this
// setting, so don't use it in release builds.
//#define MAX_FRAMERATE

// Define PROFILE_FRAMERATE to make MP log its current framerate every second. Don't use this setting in release builds.
//#define PROFILE_FRAMERATE

using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.UI.SkinEngine.ContentManagement;
using MediaPortal.UI.SkinEngine.Players;
using MediaPortal.UI.SkinEngine.ScreenManagement;
using MediaPortal.UI.SkinEngine.SkinManagement;
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
    private static bool _deviceLost = false;
    private static DxCapabilities _dxCapabilities = null;
    private static ScreenManager _screenManager = null;
    private static float _targetFrameRate = 25;
    private static int _msPerFrame = (int) (1.0 / _targetFrameRate);
    private static DateTime _frameRenderingStartTime;
    private static int _fpsCounter = 0;
    private static DateTime _fpsTimer;

    #endregion

    public static Matrix TransformWorld;
    public static Matrix TransformView;
    public static Matrix TransformProjection;
    public static Matrix FinalTransform;

    /// <summary>
    /// Returns the information if the graphics device was lost.
    /// </summary>
    /// <remarks>
    /// The device has to be reclaimed by calling <see cref="ReclaimDevice"/>.
    /// TODO: Describe when the device can get lost (Change of graphics parameters? Monitor change? Error in graphics driver?)
    /// </remarks>
    public static bool DeviceLost
    {
      get { return _deviceLost; }
    }

    /// <summary>
    /// Returns the target rendering target framerate. This value can be changed according to screen refresh rate or video fps.
    /// </summary>
    public static float TargetFrameRate
    {
      get { return _targetFrameRate; }
    }

    /// <summary>
    /// Returns the desired time per frame in ms.
    /// </summary>
    public static int MsPerFrame
    {
      get { return _msPerFrame; }
    }

    public static ScreenManager ScreenManager
    {
      get { return _screenManager; }
      internal set { _screenManager = value; }
    }

    /// <summary>
    /// Gets or sets the DirectX device.
    /// </summary>
    public static DeviceEx Device
    {
      get { return _device; }
      set { _device = value; }
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

    public static DateTime LastRenderTime
    {
       get { return _frameRenderingStartTime; }
    }

    public static D3DSetup Setup
    {
      get { return _setup; }
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

        Capabilities deviceCapabilities = _device.Capabilities;
        _backBuffer = _device.GetRenderTarget(0);
        int ordinal = deviceCapabilities.AdapterOrdinal;
        AdapterInformation adapterInfo = MPDirect3D.Direct3D.Adapters[ordinal];
        DisplayMode currentMode = adapterInfo.CurrentDisplayMode;
        AdaptTargetFrameRateToDisplayMode(currentMode);
        ServiceRegistration.Get<ILogger>().Info("GraphicsDevice: DirectX initialized {0}x{1} (format: {2} {3} Hz)", Width,
            Height, currentMode.Format, _targetFrameRate);
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
        ResetPerformanceData();
        UIResourcesHelper.ReallocUIResources();
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Critical("GraphicsDevice: Failed to setup DirectX", ex);
        Environment.Exit(0);
      }
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

    public static void SetFrameRate(float frameRate)
    {
      if (frameRate == 0)
        frameRate = 1;
      _targetFrameRate = frameRate;
      _msPerFrame = (int) (1000/_targetFrameRate);
    }

    private static void ResetPerformanceData()
    {
      _fpsTimer = DateTime.Now;
      _fpsCounter = 0;
    }

    private static void AdaptTargetFrameRateToDisplayMode(DisplayMode displayMode)
    {
      SetFrameRate(displayMode.RefreshRate);
    }

    private static void ResetDxDevice()
    {
      _setup.BuildPresentParamsFromSettings();

      _device.Reset(_setup.PresentParameters);

      ResetPerformanceData();
    }

    /// <summary>
    /// Resets the DirectX device.
    /// </summary>
    public static bool Reset()
    {
      _screenManager.ExecuteWithTempReleasedResources(() => ExecuteInMainThread(() =>
          {
            // Note that the thread which created the device must call this (see http://msdn.microsoft.com/en-us/library/windows/desktop/bb174344%28v=vs.85%29.aspx ).
            // Note also that only the thread which handles window messages is allowed to call CreateDevice and Reset
            // (see http://msdn.microsoft.com/en-us/library/windows/desktop/bb147224%28v=vs.85%29.aspx )
            ServiceRegistration.Get<ILogger>().Debug("GraphicsDevice: Reset DirectX");
            UIResourcesHelper.ReleaseUIResources();
            ServiceRegistration.Get<ILogger>().Debug("GraphicsDevice: ContentManager.TotalAllocationSize = {0}", ContentManager.Instance.TotalAllocationSize / (1024 * 1024));

            if (_backBuffer != null)
              _backBuffer.Dispose();
            _backBuffer = null;

            ResetDxDevice();
            Capabilities deviceCapabilities = _device.Capabilities;
            int ordinal = deviceCapabilities.AdapterOrdinal;
            AdapterInformation adapterInfo = MPDirect3D.Direct3D.Adapters[ordinal];
            DisplayMode currentMode = adapterInfo.CurrentDisplayMode;
            AdaptTargetFrameRateToDisplayMode(currentMode);
            ServiceRegistration.Get<ILogger>().Debug("GraphicsDevice: DirectX reset {0}x{1} format: {2} {3} Hz", Width, Height,
                currentMode.Format, _targetFrameRate);
            _backBuffer = _device.GetRenderTarget(0);
            _dxCapabilities = DxCapabilities.RequestCapabilities(deviceCapabilities, currentMode);

            UIResourcesHelper.ReallocUIResources();
          }));
      return true;
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
    /// Renders the entire scene.
    /// </summary>
    /// <param name="doWaitForNextFame"><c>true</c>, if this method should wait to the correct frame start time
    /// before it renders, else <c>false</c>.</param>
    /// <returns><c>true</c>, if the caller should wait some milliseconds before rendering the next time.</returns>
    public static bool Render(bool doWaitForNextFame)
    {
      if (_device == null || _deviceLost)
        return true;
#if (MAX_FRAMERATE == false)
      if (doWaitForNextFame)
        WaitForNextFrame();
#endif
      _frameRenderingStartTime = DateTime.Now;
      _renderAndResourceAccessLock.EnterReadLock();
      try
      {
        _device.Clear(ClearFlags.Target, Color.Black, 1.0f, 0);
        _device.BeginScene();

        _screenManager.Render();

        _device.EndScene();
        _device.PresentEx(_setup.Present);

        _fpsCounter += 1;
        TimeSpan ts = DateTime.Now - _fpsTimer;
        if (ts.TotalSeconds >= 1.0f)
        {
          float secs = (float) ts.TotalSeconds;
          SkinContext.FPS = _fpsCounter / secs;
#if PROFILE_FRAMERATE
          ServiceRegistration.Get<ILogger>().Debug("RenderLoop: {0} frames per second, {1} total frames until last measurement", SkinContext.FPS, _fpsCounter);
#endif
          _fpsCounter = 0;
          _fpsTimer = DateTime.Now;
        }
        ContentManager.Instance.Clean();
      }
      catch (Direct3D9Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("GraphicsDevice: Lost DirectX device", e);
        _deviceLost = true;
        return true;
      }
      finally
      {
        _renderAndResourceAccessLock.ExitReadLock();
      }
      return false;
    }

#if (MAX_FRAMERATE == false)
    /// <summary>
    /// Waits for the next frame to be drawn. It calculates the required difference to fit the <see cref="TargetFrameRate"/>.
    /// </summary>
    private static void WaitForNextFrame()
    {
      int msToNextFrame = _msPerFrame - (DateTime.Now - _frameRenderingStartTime).Milliseconds;
      if (msToNextFrame > 0)
        Thread.Sleep(msToNextFrame);
    }
#endif

    /// <summary>
    /// Reclaims the DirectX device if it has been lost (<see cref="DeviceLost"/>).
    /// </summary>
    /// <returns><c>true</c>, if the device could successfully be reclaimed. In this case, <see cref="DeviceLost"/> will be reset to
    /// <c>false</c>. <c>false</c>, if the device could not be reclaimed.</returns>
    public static bool ReclaimDevice()
    {
      if (_backBuffer != null)
      {
        _backBuffer.Dispose();

        _backBuffer = null;
        PlayersHelper.ReleaseGUIResources();
        ContentManager.Instance.Free();
      }

      Result result = _device.TestCooperativeLevel();

      if (result == ResultCode.DeviceNotReset)
      {
        ServiceRegistration.Get<ILogger>().Warn("GraphicsDevice: Aquired DirectX device");
        try
        {
          ServiceRegistration.Get<ILogger>().Warn("GraphicsDevice: Device reset");
          ResetDxDevice();
          int ordinal = _device.Capabilities.AdapterOrdinal;
          AdapterInformation adapterInfo = MPDirect3D.Direct3D.Adapters[ordinal];
          DisplayMode currentMode = adapterInfo.CurrentDisplayMode;
          AdaptTargetFrameRateToDisplayMode(currentMode);
          ServiceRegistration.Get<ILogger>().Debug("GraphicsDevice: DirectX reset {0}x{1} format: {2} {3} Hz", Width, Height,
              currentMode.Format, _targetFrameRate);
          _backBuffer = _device.GetRenderTarget(0);
          PlayersHelper.ReallocGUIResources();
          ServiceRegistration.Get<ILogger>().Warn("GraphicsDevice: Aquired device reset");
          ResetPerformanceData();
          return true;
        }
        catch (Exception ex)
        {
          ServiceRegistration.Get<ILogger>().Warn("GraphicsDevice: Reset failed", ex);
          return false;
        }
      }
      _deviceLost = false;
      return true;
    }
  }
}
