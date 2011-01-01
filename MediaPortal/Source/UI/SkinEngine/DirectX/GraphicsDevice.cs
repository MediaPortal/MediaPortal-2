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
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.UI.SkinEngine.ContentManagement;
using MediaPortal.UI.SkinEngine.Players;
using MediaPortal.UI.SkinEngine.ScreenManagement;
using MediaPortal.UI.SkinEngine.SkinManagement;
using SlimDX;
using SlimDX.Direct3D9;

namespace MediaPortal.UI.SkinEngine.DirectX
{
  public static class GraphicsDevice
  {
    #region Variables

    private static readonly D3DSetup _setup = new D3DSetup();
    private static DeviceEx _device;
    private static Surface _backBuffer;
    private static bool _deviceLost = false;
    private static int _anisotropy;
    private static bool _supportsFiltering;
    private static bool _supportsAlphaBlend;
    private static bool _supportsShaders = false;
    private static bool _firstTimeInitialisation = true;
    private static ScreenManager _screenManager = null;
    private static int _targetFrameRate = 0;
    private static DateTime _frameRenderingStartTime;
    private static int _fpsCounter = 0;
    private static DateTime _fpsTimer;

    #endregion

    public static Matrix TransformWorld;
    public static Matrix TransformView;
    public static Matrix TransformProjection;
    public static Matrix FinalTransform;

    public static void Initialize(Form window)
    {
      try
      {
        ServiceRegistration.Get<ILogger>().Debug("GraphicsDevice: Initialize DirectX");
        MPDirect3D.Load();
        _setup.SetupDirectX(window);
        _backBuffer = _device.GetRenderTarget(0);
        int ordinal = _device.Capabilities.AdapterOrdinal;
        AdapterInformation adapterInfo = MPDirect3D.Direct3D.Adapters[ordinal];
        DisplayMode currentMode = adapterInfo.CurrentDisplayMode;
        AdaptTargetFrameRateToDisplayMode(currentMode);
        ServiceRegistration.Get<ILogger>().Info("GraphicsDevice: DirectX initialized {0}x{1} (format: {2} {3} Hz)", Width,
            Height, currentMode.Format, _targetFrameRate);
        GetCapabilities();
        ResetPerformanceData();
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Critical("GraphicsDevice: Failed to setup DirectX", ex);
        Environment.Exit(0);
      }
    }

    public static void Dispose()
    {
      if (_backBuffer != null)
        _backBuffer.Dispose();
      _backBuffer = null;

      if (_device != null)
        _device.Dispose();
      _device = null;
      MPDirect3D.Unload();
    }

    public static bool DeviceLost
    {
      get { return _deviceLost; }
      set { _deviceLost = value; }
    }

    /// <summary>
    /// Returns the target rendering target framerate. This value can be changed according to screen refresh rate or video fps.
    /// </summary>
    public static Int32 TargetFrameRate
    {
      get { return _targetFrameRate; }
    }

    private static void GetCapabilities()
    {
      _anisotropy = _device.Capabilities.MaxAnisotropy;
      _supportsFiltering = MPDirect3D.Direct3D.CheckDeviceFormat(
          _device.Capabilities.AdapterOrdinal,
          _device.Capabilities.DeviceType,
          _device.GetDisplayMode(0).Format,
          Usage.RenderTarget | Usage.QueryFilter,
          ResourceType.Texture,
          Format.A8R8G8B8);

      _supportsAlphaBlend = MPDirect3D.Direct3D.CheckDeviceFormat(_device.Capabilities.AdapterOrdinal,
          _device.Capabilities.DeviceType, _device.GetDisplayMode(0).Format,
          Usage.RenderTarget | Usage.QueryPostPixelShaderBlending,
          ResourceType.Surface, Format.A8R8G8B8);
      int vertexShaderVersion = _device.Capabilities.VertexShaderVersion.Major;
      int pixelShaderVersion = _device.Capabilities.PixelShaderVersion.Major;
      ServiceRegistration.Get<ILogger>().Info("DirectX: Pixel shader support: {0}.{1}", _device.Capabilities.PixelShaderVersion.Major, _device.Capabilities.PixelShaderVersion.Minor);
      ServiceRegistration.Get<ILogger>().Info("DirectX: Vertex shader support: {0}.{1}", _device.Capabilities.VertexShaderVersion.Major, _device.Capabilities.VertexShaderVersion.Minor);
      _supportsShaders = pixelShaderVersion >= 2 && vertexShaderVersion >= 2;
      if (_firstTimeInitialisation)
      {
        _firstTimeInitialisation = false;
        if (!_supportsShaders)
        {
          string text = String.Format("MediaPortal 2 needs a graphics card wich supports shader model 2.0\nYour card does NOT support this.\nMediaportal 2 will continue but migh run slow");
          MessageBox.Show(text, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }
      }
    }

    private static void ResetPerformanceData()
    {
      _fpsTimer = DateTime.Now;
      _fpsCounter = 0;
    }

    private static void AdaptTargetFrameRateToDisplayMode(DisplayMode displayMode)
    {
      _targetFrameRate = displayMode.RefreshRate;
    }

    private static void ResetDxDevice()
    {
      _setup.BuildPresentParamsFromSettings();

      Result result = _device.Reset(_setup.PresentParameters);

      if (result == ResultCode.DeviceLost)
      {
        result = _device.TestCooperativeLevel();
        // Loop until it's ok to reset
        while (result == ResultCode.DeviceLost)
        {
          Thread.Sleep(10);
          result = _device.TestCooperativeLevel();
        }
        _device.Reset(_setup.PresentParameters);
      }
      ResetPerformanceData();
    }

    /// <summary>
    /// Resets the DirectX device.
    /// </summary>
    public static bool Reset()
    {
      ServiceRegistration.Get<ILogger>().Debug("GraphicsDevice: Reset DirectX, {0}", ServiceRegistration.Get<ContentManager>().TotalAllocationSize / (1024 * 1024));
      if (_backBuffer != null)
        _backBuffer.Dispose();
      _backBuffer = null;
      _setup.BuildPresentParamsFromSettings();
      ResetDxDevice();
      int ordinal = _device.Capabilities.AdapterOrdinal;
      AdapterInformation adapterInfo = MPDirect3D.Direct3D.Adapters[ordinal];
      DisplayMode currentMode = adapterInfo.CurrentDisplayMode;
      AdaptTargetFrameRateToDisplayMode(currentMode);
      ServiceRegistration.Get<ILogger>().Debug("GraphicsDevice: DirectX reset {0}x{1} format: {2} {3} Hz", Width, Height,
          currentMode.Format, _targetFrameRate);
      _backBuffer = _device.GetRenderTarget(0);
      GetCapabilities();
      return true;
    }

    public static ScreenManager ScreenManager
    {
      get { return _screenManager; }
      set { _screenManager = value; }
    }

    public static bool IsWindowed
    {
      get { return _setup.Windowed; }
    }

    /// <summary>
    /// Gets or sets the DirectX Device.
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

    public static bool SupportsShaders
    {
      get { return _supportsShaders; }
    }

    public static DateTime LastRenderTime
    {
       get { return _frameRenderingStartTime; }
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
      _device.SetRenderState(RenderState.SourceBlend, Blend.SourceAlpha);
      _device.SetRenderState(RenderState.DestinationBlend, Blend.InverseSourceAlpha);

      if (_supportsAlphaBlend)
      {
        _device.SetRenderState(RenderState.AlphaTestEnable, true);
        _device.SetRenderState(RenderState.AlphaRef, 0x01);
        _device.SetRenderState(RenderState.AlphaFunc, Compare.GreaterEqual);
      }
      _device.SetSamplerState(0, SamplerState.MinFilter, TextureFilter.Linear);
      _device.SetSamplerState(0, SamplerState.MagFilter, TextureFilter.Linear);
      _device.SetSamplerState(0, SamplerState.MipFilter, TextureFilter.Linear);

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

      // Camera view. Multiply the Y coord by -1) { translate so that everything is relative to the camera position.
      Matrix flipY = Matrix.Scaling(1.0f, -1.0f, 1.0f);
      Matrix translate = Matrix.Translation(-w, -h, 2 * h);
      TransformView = Matrix.Multiply(translate, flipY);

      w *= 0.5f;
      h *= 0.5f;

      TransformProjection = Matrix.PerspectiveOffCenterLH(-w, w, -h, h, h * 2.0f, h * 200.0f); 
      FinalTransform = TransformView * TransformProjection;
    }

    /// <summary>
    /// Renders the entire scene.
    /// </summary>
    /// <returns><c>true</c>, if the caller should wait some milliseconds before rendering the next time.</returns>
    public static bool Render(bool doWaitForNextFame)
    {
      if (_device == null || _deviceLost)
        return true;
      _frameRenderingStartTime = DateTime.Now;
      lock (_setup)
      {
        try
        {
          _device.Clear(ClearFlags.Target, Color.Black, 1.0f, 0);
          _device.BeginScene();

          _screenManager.Render();

          _device.EndScene();
          _device.PresentEx(Present.ForceImmediate);

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
        }
        catch (Direct3D9Exception e)
        {
          ServiceRegistration.Get<ILogger>().Warn("GraphicsDevice: Lost DirectX device", e);
          _deviceLost = true;
          return true;
        }
        ServiceRegistration.Get<ContentManager>().Clean();
      }
#if (MAX_FRAMERATE == false)
      if (doWaitForNextFame)
        WaitForNextFrame();
#endif
      return false;
    }

#if (MAX_FRAMERATE == false)
    /// <summary>
    /// Waits for the next frame to be drawn. It calculates the required difference to fit the <see cref="TargetFrameRate"/>.
    /// </summary>
    private static void WaitForNextFrame()
    {
      int msToNextFrame = (int) (1000f / _targetFrameRate - (DateTime.Now - _frameRenderingStartTime).Milliseconds); 
      if (msToNextFrame > 0)
        Thread.Sleep(msToNextFrame);
    }
#endif

    public static bool ReclaimDevice()
    {
      if (_backBuffer != null)
      {
        _backBuffer.Dispose();

        _backBuffer = null;
        PlayersHelper.ReleaseGUIResources();
        ServiceRegistration.Get<ContentManager>().Free();
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
      return true;
    }

    /// <summary>
    /// Returns available display modes for the display device
    /// </summary>
    public static IList<DisplayMode> GetDisplayModes()
    {
      return _setup.GetDisplayModes();
    }

    public static string DesktopDisplayMode
    {
      get { return _setup.DesktopDisplayMode; }
    }

    public static int DesktopHeight
    {
      get { return _setup.DesktopHeight; }
    }

    public static int DesktopWidth
    {
      get { return _setup.DesktopWidth; }
    }
  }
}
