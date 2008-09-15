#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
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
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Presentation.Players;
using MediaPortal.Presentation.Screen;
using MediaPortal.SkinEngine.ContentManagement;
using SlimDX;
using SlimDX.Direct3D9;
using MediaPortal.SkinEngine.DirectX;
using MediaPortal.SkinEngine.Rendering;
using MediaPortal.SkinEngine.SkinManagement;

namespace MediaPortal.SkinEngine
{

  public class GraphicsDevice : IDisposable
  {
    #region variables

    private static d3dSetup _setup = new d3dSetup();
    private static Device _device;
    private static Surface _backBuffer;
    private static uint _lastRender;
    private static bool _videoThreadActive = false;
    private static bool _deviceLost = false;
    private static int _anisotropy;
    private static bool _supportsFiltering;
    private static bool _supportsAlphaBlend;
    private static bool _supportsShaders = false;
    private static bool _firstTimeInitialisation = true;
    private static bool _firstTimeInitialisationMemory = true;

    #endregion
    public static Matrix TransformWorld;
    public static Matrix TransformView;
    public static Matrix TransformProjection;
    public static Matrix FinalTransform;

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphicsDevice"/> class.
    /// </summary>
    /// <param name="window">The window.</param>
    public GraphicsDevice(Form window, bool maximize)
    {
      if (_firstTimeInitialisationMemory)
      {
        _firstTimeInitialisationMemory = false;
#if NOTUSED
        Microsoft.DirectX.DirectDraw.Device tmpDev = new Microsoft.DirectX.DirectDraw.Device(Microsoft.DirectX.DirectDraw.CreateFlags.HardwareOnly);
        Microsoft.DirectX.DirectDraw.GetCapsStruct caps = tmpDev.GetCaps();
        tmpDev.Dispose();
        tmpDev = null;

        int videoMemory = caps.HardwareCaps.VideoMemoryTotal / (1000 * 1000);
        ServiceScope.Get<ILogger>().Info("Directx: Total Video Memory:{0} MB", videoMemory);
        if (videoMemory < 128)
        {
          string text = String.Format("MediaPortal-II needs a graphics card with at least 128 MB video memory\nYour card does only has {0} MB.\nMediaportal-II will continue but migh run slow", videoMemory);
          MessageBox.Show(text, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }
#endif
      }
      try
      {
        ServiceScope.Get<ILogger>().Debug("GraphicsDevice: Initialize DirectX");
        MPDirect3D.Load();
        _setup.SetupDirectX(window, maximize);
        _backBuffer = _device.GetRenderTarget(0);
        int ordinal = GraphicsDevice.Device.Capabilities.AdapterOrdinal;
        AdapterInformation adapterInfo = MPDirect3D.Direct3D.Adapters[ordinal];
        ServiceScope.Get<ILogger>().Info("GraphicsDevice: DirectX initialized {0}x{1} format: {2} {3} Hz", Width,
                                          Height, adapterInfo.CurrentDisplayMode.Format,
                                          adapterInfo.CurrentDisplayMode.RefreshRate);
        GetCapabilities();
      }
      catch (Exception ex)
      {
        ServiceScope.Get<ILogger>().Critical("GraphicsDevice: failed to set-up DirectX");
        ServiceScope.Get<ILogger>().Critical(ex);
        System.Environment.Exit(0);
      }
    }

    public static bool DeviceLost
    {
      get { return _deviceLost; }
      set { _deviceLost = false; }
    }

    private static void GetCapabilities()
    {
      _anisotropy = _device.Capabilities.MaxAnisotropy;
      _supportsFiltering = MPDirect3D.Direct3D.CheckDeviceFormat(
        _device.Capabilities.AdapterOrdinal,
        _device.Capabilities.DeviceType,
        _device.GetDisplayMode(0).Format,
        Usage.RenderTarget | Usage.QueryFilter, ResourceType.Texture,
        Format.A8R8G8B8);

      _supportsAlphaBlend = MPDirect3D.Direct3D.CheckDeviceFormat(_device.Capabilities.AdapterOrdinal,
                                                      _device.Capabilities.DeviceType,
                                                      _device.GetDisplayMode(0).Format,
                                                      Usage.RenderTarget | Usage.QueryPostPixelShaderBlending,
                                                      ResourceType.Surface,
                                                      Format.A8R8G8B8);
      int vertexShaderVersion = Device.Capabilities.VertexShaderVersion.Major;
      int pixelShaderVersion = Device.Capabilities.PixelShaderVersion.Major;
      ServiceScope.Get<ILogger>().Info("DirectX: Pixel shader support: {0}.{1}", Device.Capabilities.PixelShaderVersion.Major, Device.Capabilities.PixelShaderVersion.Minor);
      ServiceScope.Get<ILogger>().Info("DirectX: Vertex shader support: {0}.{1}", Device.Capabilities.VertexShaderVersion.Major, Device.Capabilities.VertexShaderVersion.Minor);
      if (pixelShaderVersion >= 2 && vertexShaderVersion >= 2)
      {
        _supportsShaders = true;
      }
      else
      {
        _supportsShaders = false;
      }
      if (_firstTimeInitialisation)
      {
        _firstTimeInitialisation = false;
        if (pixelShaderVersion < 2 || vertexShaderVersion < 2)
        {
          string text = String.Format("MediaPortal-II needs a graphics card wich supports shader model 2.0\nYour card does NOT support this.\nMediaportal-II will continue but migh run slow");
          MessageBox.Show(text, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }
      }
    }


    /// <summary>
    /// Resets the DirectX device.
    /// </summary>
    /// <param name="exclusiveMode">If set to <c>true</c> then use DirectX exclusive mode
    /// else DirectX windowed mode.</param>
    /// <param name="mode">Mode to set on the DirectX device.</param>
    public static bool Reset(bool exclusiveMode, string displaySetting)
    {

        ServiceScope.Get<ILogger>().Debug("GraphicsDevice: Reset DirectX, exclusive: {0} {1} {2}", exclusiveMode, ContentManager.TextureReferences, ContentManager.VertexReferences);
        if (ContentManager.TextureReferences == 0 && ContentManager.VertexReferences == 0)
        {
          if (_backBuffer != null)
          {
            _backBuffer.Dispose();
          }
          _backBuffer = null;
          _setup.SwitchExlusiveOrWindowed(exclusiveMode, displaySetting);
          int ordinal = GraphicsDevice.Device.Capabilities.AdapterOrdinal;
          AdapterInformation adapterInfo = MPDirect3D.Direct3D.Adapters[ordinal];
          ServiceScope.Get<ILogger>().Debug("GraphicsDevice: DirectX reset {0}x{1} format: {2} {3} Hz", Width, Height,
                                            adapterInfo.CurrentDisplayMode.Format,
                                            adapterInfo.CurrentDisplayMode.RefreshRate);
          _backBuffer = _device.GetRenderTarget(0);
          GetCapabilities();
        }
        else
        {
          ServiceScope.Get<ILogger>().Error("GraphicsDevice: cannot reset directx. {0} {1}", ContentManager.TextureReferences, ContentManager.VertexReferences);
        }
        return true;
        ServiceScope.Get<ILogger>().Error("GraphicsDevice: Failed to reset DirectX");
        // ServiceScope.Get<ILogger>().Error(ex);

      return false;
    }

    public static bool IsWindowed
    {
      get { return _setup.Windowed; }
    }

    #region IDisposable Members

    /// <summary>
    /// Disposes this instance.
    /// </summary>
    public void Dispose()
    {
      if (_backBuffer != null)
      {
        _backBuffer.Dispose();
      }
      _backBuffer = null;

      if (_device != null)
      {
        _device.Dispose();
      }
      _device = null;
      MPDirect3D.Unload();
    }
    #endregion

    /// <summary>
    /// Gets or sets the DirectX Device.
    /// </summary>
    /// <value>The DirectX Device.</value>
    public static Device Device
    {
      get { return _device; }
      set { _device = value; }
    }




    /// <summary>
    /// Gets the directx back-buffer width.
    /// </summary>
    /// <value>The directx back-buffer width.</value>
    public static int Width
    {
      get { return _setup.PresentParameters.BackBufferWidth; }
    }

    /// <summary>
    /// Gets the directx back-buffer height.
    /// </summary>
    /// <value>The directx back-buffer height.</value>
    public static int Height
    {
      get { return _setup.PresentParameters.BackBufferHeight; }
    }

    public static bool SupportsShaders
    {
      get { return _supportsShaders; }
    }

    /// <summary>
    /// Sets the directx render states and project matrices
    /// </summary>
    public static void SetRenderState()
    {
      Device.SetRenderState(RenderState.CullMode, Cull.None);
      Device.SetRenderState(RenderState.Lighting, false);

      // Z order must be enabled for batching to work
      if (SkinContext.UseBatching == true)
      {
        Device.SetRenderState(RenderState.ZEnable, true);
        Device.SetRenderState(RenderState.ZWriteEnable, true);
      }
      else
      {
        Device.SetRenderState(RenderState.ZEnable, false);
        Device.SetRenderState(RenderState.ZWriteEnable, false);
      }
      Device.SetRenderState(RenderState.FillMode, FillMode.Solid);
      Device.SetRenderState(RenderState.AlphaBlendEnable, true);
      Device.SetRenderState(RenderState.SourceBlend, Blend.SourceAlpha);
      Device.SetRenderState(RenderState.DestinationBlend, Blend.InverseSourceAlpha);
      Device.SetTextureStageState(0, TextureStage.ColorOperation, TextureOperation.Modulate);
      Device.SetTextureStageState(0, TextureStage.ColorArg0, TextureArgument.Texture);
      Device.SetTextureStageState(0, TextureStage.ColorArg1, TextureArgument.Diffuse);
      Device.SetTextureStageState(0, TextureStage.AlphaOperation, TextureOperation.Modulate);
      Device.SetTextureStageState(0, TextureStage.AlphaArg0, TextureArgument.Texture);
      Device.SetTextureStageState(0, TextureStage.AlphaArg1, TextureArgument.Diffuse);

      //Device.SetTextureStageState(1, TextureStage.ColorOperation, TextureOperation.Modulate);
      //Device.SetTextureStageState(1, TextureStage.ColorArg0, TextureArgument.Texture);
      //Device.SetTextureStageState(1, TextureStage.ColorArg1, TextureArgument.Current);
      //Device.SetTextureStageState(1, TextureStage.AlphaOperation, TextureOperation.Modulate);
      //Device.SetTextureStageState(1, TextureStage.AlphaArg0, TextureArgument.Texture);
      //Device.SetTextureStageState(1, TextureStage.AlphaArg1, TextureArgument.Current);


      if (_supportsAlphaBlend)
      {
        Device.SetRenderState(RenderState.AlphaTestEnable, true);
        Device.SetRenderState(RenderState.AlphaRef, 0x01);
        Device.SetRenderState(RenderState.AlphaFunc, Compare.GreaterEqual);
      }
      if (_supportsFiltering)
      {
        /* DX9 supports the following filter combinations
         * Filter       Minification Magnifcation Mipmap
         * None                  No            No    Yes
         * Point                Yes           Yes    Yes
         * Linear               Yes           Yes    Yes
         * Anisotropic          Yes           Yes     No
         * Flat Cubic            No           Yes     No
         * Gaussian Cubic        No           Yes     No
         */
        try
        {
          Device.SetSamplerState(0, SamplerState.MinFilter, TextureFilter.Anisotropic);
          Device.SetSamplerState(0, SamplerState.MagFilter, TextureFilter.Anisotropic);
          Device.SetSamplerState(0, SamplerState.MipFilter, TextureFilter.Linear);
          Device.SetSamplerState(0, SamplerState.MaxAnisotropy, _anisotropy);
        }
        catch (Exception)
        {
          _supportsFiltering = false;
        }
      }
      else
      {
        Device.SetSamplerState(0, SamplerState.MinFilter, TextureFilter.Point);
        Device.SetSamplerState(0, SamplerState.MagFilter, TextureFilter.Point);
        Device.SetSamplerState(0, SamplerState.MipFilter, TextureFilter.Point);
      }


      int gw = Width;
      int gh = Height;
      gw = Width;
      gh = Height;
      InitializeZoom();
      Point camera = new Point(gw / 2, gh / 2);
      // and calculate the offset from the screen center
      Point offset = new Point(camera.X - (gw / 2), camera.Y - (gh / 2));

      // grab the viewport dimensions and location
      Viewport viewport = Device.Viewport;
      float w = Width * 0.5f; // viewport.Width * 0.5f;
      float h = Height * 0.5f; // viewport.Height * 0.5f;

      Matrix mtxWorld;
      mtxWorld = Matrix.Identity;
      //GraphicsDevice.TransformWorld = mtxWorld;

      // camera view.  Multiply the Y coord by -1) { translate so that everything is relative to the camera
      // position.
      Matrix flipY, translate, mtxView;
      flipY = Matrix.Scaling(1.0f, -1.0f, 1.0f);
      translate = Matrix.Translation(-(viewport.X + w + offset.X), -(viewport.Y + h + offset.Y), 2 * h);
      mtxView = Matrix.Multiply(translate, flipY);
      GraphicsDevice.TransformView = mtxView;

      // projection onto screen space
      Matrix mtxProjection = Matrix.PerspectiveOffCenterLH((-w - offset.X) * 0.5f, //Minimum x-value of the view volume.
                                                           (w - offset.X) * 0.5f, //Maximum x-value of the view volume.
                                                           (-h + offset.Y) * 0.5f, //Minimum y-value of the view volume.
                                                           (h + offset.Y) * 0.5f, //Maximum y-value of the view volume.
                                                           h, //Minimum z-value of the view volume.
                                                           100 * h); //Maximum z-value of the view volume.
      GraphicsDevice.TransformProjection = mtxProjection;

      GraphicsDevice.FinalTransform = GraphicsDevice.TransformView * GraphicsDevice.TransformProjection;
      ////GraphicsDevice.TransformWorld = Matrix.Identity;
      //GraphicsDevice.TransformView = Matrix.LookAtLH(new Vector3(0, 0, -10.0f), new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, -1.0f, 0.0f));
      //GraphicsDevice.TransformProjection = Matrix.PerspectiveFovLH((float)Math.PI / 4.0f, 1.0f, 1.0f, 100.0f);
    }

    public static void InitializeZoom()
    {
      SkinContext.Zoom = new SizeF(Width / (float)SkinContext.SkinWidth, Height / (float)SkinContext.SkinHeight);
    }

    /// <summary>
    /// Renders the entire scene
    /// This method gets called normally by our own renderthread
    /// But.. when a movie is playing, it is called from EVR/VMR9 thread
    /// reason for this is that we need to sync the drawing with the video
    /// the mainloop variable indicates if the method is called by our own renderthread or by the evr/vmr9 thread
    /// </summary>
    /// <param name="calledFromOurRenderThread">
    /// set to true when our own render thread called this method. 
    /// False when called from a EVR/VMR9 thread</param>
    /// <returns></returns>
    /// 
    public static bool Render(bool calledFromOurRenderThread)
    {
      if (_device == null)
      {
        return true;
      }

      if (_deviceLost)
      {
        return true;
      }
      lock (_setup)
      {
        uint time = (uint)Environment.TickCount;
        //if evr/vmr9 called this method 
        if (!calledFromOurRenderThread && !_videoThreadActive)
        {
          Trace.WriteLine(String.Format("vmr9 renderer starts {0}", time - _lastRender));
          _videoThreadActive = true;
        }

        //if our own reder thread called this method
        if (calledFromOurRenderThread)
        {
          //is evr/vmr9 thread also rendering?
          if (_videoThreadActive)
          {
            //yes. Check if the evr/vmr9 thread is still busy
            if (time - _lastRender < 500)
            {
              //yes then we return since evr/vmr9 thread is doing the rendering for us
              return true;
            }

            //evr/vmr9 thread stopped (can happen when video is paused for example)
            //so... let our own render thread do the rendering again
            _videoThreadActive = false;
            Trace.WriteLine(String.Format("vmr9 renderer seems stopped, back to main loop {0}", (time - _lastRender)));
          }
        }

        _lastRender = time;
        try
        {
          //_device.SetRenderTarget(0, _backBuffer);

          //Clear the backbuffer to a blue color (ARGB = 000000ff)

          if (SkinContext.UseBatching == true)
          {
            _device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.Black, 1.0f, 0);
          }
          else
          {
            _device.Clear(ClearFlags.Target, Color.Black, 1.0f, 0);
          }



          //Begin the scene
          _device.BeginScene();

          GraphicsDevice.Device.VertexFormat = PositionColored2Textured.Format;
          //render the window(s)
          if (!SkinContext.ScreenSaverActive)
          {

            ScreenManager manager = (ScreenManager)ServiceScope.Get<IScreenManager>();
            manager.Render();
            if (SkinContext.UseBatching)
            {
              RenderPipeline.Instance.Render();
            }
          }
          //End the scene
          _device.EndScene();
          _device.Present();
          //if (calledFromOurRenderThread)
          //{
          //  _device.Present();
          //}
        }
        catch (SlimDX.Direct3D9.Direct3D9Exception ex)
        {
          ServiceScope.Get<ILogger>().Warn("GraphicsDevice: Lost DirectX device");
          _deviceLost = true;
          return true;
        }
        ContentManager.Clean();
      }
      return false;
    }

    public static bool ReclaimDevice()
    {
      if (_backBuffer != null)
      {
        _backBuffer.Dispose();

        _backBuffer = null;
        ServiceScope.Get<IPlayerCollection>().Dispose();
        ContentManager.Free();
      }

      Result result = _device.TestCooperativeLevel();

      if (result == ResultCode.DeviceNotReset)
      {
        ServiceScope.Get<ILogger>().Warn("GraphicsDevice: Aquired DirectX device");
        try
        {
          ServiceScope.Get<ILogger>().Warn("GraphicsDevice: Device reset");
          _setup.Reset();
          int ordinal = GraphicsDevice.Device.Capabilities.AdapterOrdinal;
          AdapterInformation adapterInfo = MPDirect3D.Direct3D.Adapters[ordinal];
          ServiceScope.Get<ILogger>().Debug("GraphicsDevice: DirectX reset {0}x{1} format: {2} {3} Hz", Width, Height,
                                            adapterInfo.CurrentDisplayMode.Format,
                                            adapterInfo.CurrentDisplayMode.RefreshRate);
          _backBuffer = _device.GetRenderTarget(0);
          ServiceScope.Get<ILogger>().Warn("GraphicsDevice: Aquired device reset");
          return true;
        }
        catch (Exception ex)
        {
          ServiceScope.Get<ILogger>().Warn("GraphicsDevice: Reset failed");
          ServiceScope.Get<ILogger>().Error(ex);
        }
      }
      return false;
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
