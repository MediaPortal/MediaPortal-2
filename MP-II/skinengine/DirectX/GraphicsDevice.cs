#region Copyright (C) 2007 Team MediaPortal

/*
    Copyright (C) 2007 Team MediaPortal
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
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Core.Players;
using MediaPortal.Core.WindowManager;
using MediaPortal.Core.Collections;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using SkinEngine.DirectX;
using SkinEngine.Fonts;

namespace SkinEngine
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

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphicsDevice"/> class.
    /// </summary>
    /// <param name="window">The window.</param>
    public GraphicsDevice(Form window, bool maximize)
    {
      try
      {
        ServiceScope.Get<ILogger>().Debug("GraphicsDevice: Initialize directx");
        _setup.SetupDirectX(window, maximize);
        _backBuffer = _device.GetRenderTarget(0);
        AdapterInformation adapterInfo = Manager.Adapters.Default;
        ServiceScope.Get<ILogger>().Debug("GraphicsDevice: directx initialized {0}x{1} format: {2} {3} Hz", Width,
                                          Height, adapterInfo.CurrentDisplayMode.Format,
                                          adapterInfo.CurrentDisplayMode.RefreshRate);
        GetCapabilities();
      }
      catch (Exception ex)
      {
        ServiceScope.Get<ILogger>().Error("GraphicsDevice: failed to setup directx");
        ServiceScope.Get<ILogger>().Error(ex);
      }
    }

    public static bool DeviceLost
    {
      get { return _deviceLost; }
      set { _deviceLost = false; }
    }

    private static void GetCapabilities()
    {
      _anisotropy = _device.DeviceCaps.MaxAnisotropy;
      _supportsFiltering = Manager.CheckDeviceFormat(
        _device.DeviceCaps.AdapterOrdinal,
        _device.DeviceCaps.DeviceType,
        _device.DisplayMode.Format,
        Usage.RenderTarget | Usage.QueryFilter, ResourceType.Textures,
        Format.A8R8G8B8);

      _supportsAlphaBlend = Manager.CheckDeviceFormat(_device.DeviceCaps.AdapterOrdinal,
                                                      _device.DeviceCaps.DeviceType,
                                                      _device.DisplayMode.Format,
                                                      Usage.RenderTarget | Usage.QueryPostPixelShaderBlending,
                                                      ResourceType.Surface,
                                                      Format.A8R8G8B8);
      int vertexShaderVersion = Device.DeviceCaps.VertexShaderVersion.Major;
      int pixelShaderVersion = Device.DeviceCaps.PixelShaderVersion.Major;
      if (pixelShaderVersion >= 2 && vertexShaderVersion >= 2)
      {
        _supportsShaders = true;
      }
      else
      {
        _supportsShaders = false;
      }
    }


    /// <summary>
    /// Resets the directx device
    /// </summary>
    /// <param name="window">The window.</param>
    /// <param name="exclusiveMode">if set to <c>true</c> then use directx exclusive mode
    /// else directx windowed mode.</param>
    public static bool Reset(Form window, bool exclusiveMode, string displaySetting)
    {
      try
      {
        ServiceScope.Get<ILogger>().Debug("GraphicsDevice: reset directx, exclusive:{0} {1} {2}", exclusiveMode, ContentManager.TextureReferences, ContentManager.VertexReferences);
        if (ContentManager.TextureReferences == 0 && ContentManager.VertexReferences == 0)
        {
          if (_backBuffer != null)
          {
            _backBuffer.Dispose();
          }
          _backBuffer = null;
          _setup.SwitchExlusiveOrWindowed(exclusiveMode, displaySetting);
          AdapterInformation adapterInfo = Manager.Adapters.Default;
          ServiceScope.Get<ILogger>().Debug("GraphicsDevice: directx reset {0}x{1} format: {2} {3} Hz", Width, Height,
                                            adapterInfo.CurrentDisplayMode.Format,
                                            adapterInfo.CurrentDisplayMode.RefreshRate);
          _backBuffer = _device.GetRenderTarget(0);
          GetCapabilities();
        }
        else
        {
          ServiceScope.Get<ILogger>().Error("GraphicsDevice: cannot reset directx. {0} {1}",ContentManager.TextureReferences,ContentManager.VertexReferences);
        }
        return true;
      }
      catch (DirectXException)
      {
        ServiceScope.Get<ILogger>().Error("GraphicsDevice: failed to reset directx");
        // ServiceScope.Get<ILogger>().Error(ex);
      }
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

      if (_device == null)
      {
        _device.Dispose();
        _device = null;
      }
    }

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
    private static void SetRenderState()
    {
      Device.RenderState.CullMode = Cull.None;
      Device.RenderState.Lighting = false;
      Device.RenderState.ZBufferEnable = false;

      Device.RenderState.FillMode = FillMode.Solid;
      Device.RenderState.AlphaBlendEnable = true;
      Device.RenderState.SourceBlend = Blend.SourceAlpha;
      Device.RenderState.DestinationBlend = Blend.InvSourceAlpha;
      Device.TextureState[0].ColorOperation = TextureOperation.Modulate;
      Device.TextureState[0].ColorArgument1 = TextureArgument.TextureColor;
      Device.TextureState[0].ColorArgument2 = TextureArgument.Diffuse;
      Device.TextureState[0].AlphaOperation = TextureOperation.Modulate;
      Device.TextureState[0].AlphaArgument1 = TextureArgument.TextureColor;
      Device.TextureState[0].AlphaArgument2 = TextureArgument.Diffuse;

      Device.TextureState[1].ColorOperation = TextureOperation.Modulate;
      Device.TextureState[1].ColorArgument1 = TextureArgument.TextureColor;
      Device.TextureState[1].ColorArgument2 = TextureArgument.Current;
      Device.TextureState[1].AlphaOperation = TextureOperation.Modulate;
      Device.TextureState[1].AlphaArgument1 = TextureArgument.TextureColor;
      Device.TextureState[1].AlphaArgument2 = TextureArgument.Current;


      if (_supportsAlphaBlend)
      {
        Device.RenderState.AlphaTestEnable = true;
        Device.RenderState.ReferenceAlpha = 0x01;
        Device.RenderState.AlphaFunction = Compare.GreaterEqual;
      }

      if (_supportsFiltering)
      {
        try
        {
          Device.SamplerState[0].MinFilter = TextureFilter.Anisotropic;
          Device.SamplerState[0].MagFilter = TextureFilter.Anisotropic;
          Device.SamplerState[0].MipFilter = TextureFilter.Anisotropic;
          Device.SamplerState[0].MaxAnisotropy = _anisotropy;
        }
        catch (Exception)
        {
          _supportsFiltering = false;
        }
      }
      else
      {
        Device.SamplerState[0].MinFilter = TextureFilter.Point;
        Device.SamplerState[0].MagFilter = TextureFilter.Point;
        Device.SamplerState[0].MipFilter = TextureFilter.Point;
      }


      int gw = Width;
      int gh = Height;
      gw = (int)SkinContext.Width;
      gh = (int)SkinContext.Height;
      Point camera = new Point(gw / 2, gh / 2);
      // and calculate the offset from the screen center
      Point offset = new Point(camera.X - (gw / 2), camera.Y - (gh / 2));

      // grab the viewport dimensions and location
      Viewport viewport = Device.Viewport;
      float w = SkinContext.Width * 0.5f; // viewport.Width * 0.5f;
      float h = SkinContext.Height * 0.5f; // viewport.Height * 0.5f;

      Matrix mtxWorld;
      mtxWorld = Matrix.Identity;
      Device.Transform.World = mtxWorld;

      // camera view.  Multiply the Y coord by -1) { translate so that everything is relative to the camera
      // position.
      Matrix flipY, translate, mtxView;
      flipY = Matrix.Scaling(1.0f, -1.0f, 1.0f);
      translate = Matrix.Translation(-(viewport.X + w + offset.X), -(viewport.Y + h + offset.Y), 2 * h);
      mtxView = Matrix.Multiply(translate, flipY);
      Device.Transform.View = mtxView;

      // projection onto screen space
      Matrix mtxProjection = Matrix.PerspectiveOffCenterLH((-w - offset.X) * 0.5f, //Minimum x-value of the view volume.
                                                           (w - offset.X) * 0.5f, //Maximum x-value of the view volume.
                                                           (-h + offset.Y) * 0.5f, //Minimum y-value of the view volume.
                                                           (h + offset.Y) * 0.5f, //Maximum y-value of the view volume.
                                                           h, //Minimum z-value of the view volume.
                                                           100 * h); //Maximum z-value of the view volume.
      Device.Transform.Projection = mtxProjection;

      //GraphicsDevice.Device.Transform.World = Matrix.Identity;
      //GraphicsDevice.Device.Transform.View = Matrix.LookAtLH(new Vector3(0, 0, -10.0f), new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, -1.0f, 0.0f));
      //GraphicsDevice.Device.Transform.Projection = Matrix.PerspectiveFovLH((float)Math.PI / 4.0f, 1.0f, 1.0f, 100.0f);
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
          _device.SetRenderTarget(0, _backBuffer);
          //Clear the backbuffer to a blue color (ARGB = 000000ff)
          //Dont remove this, MP-II uses the Z-buffer for some styles
          //rendering goes wrong when zbuffer is not cleared
          _device.Clear(ClearFlags.Target|ClearFlags.ZBuffer, Color.Black, 10000f, 0);

          SetRenderState();

          //Begin the scene
          _device.BeginScene();

          //render the window(s)
          if (!SkinContext.ScreenSaverActive)
          {
            WindowManager manager = (WindowManager)ServiceScope.Get<IWindowManager>();
            manager.Render();
          }
          //End the scene
          _device.EndScene();
          _device.Present();
        }
        catch (DeviceLostException)
        {
          ServiceScope.Get<ILogger>().Warn("GraphicsDevice:Lost directx device");
          _deviceLost = true;
          return true;
        }
        catch (Microsoft.DirectX.Direct3D.InvalidCallException ex)
        {
          ServiceScope.Get<ILogger>().Warn("GraphicsDevice: InvalidCallException");
          ServiceScope.Get<ILogger>().Error(ex);
          _deviceLost = true;
          return true;
        }
        catch (GraphicsException ex)
        {
          ServiceScope.Get<ILogger>().Warn("GraphicsDevice: graphics exception");
          ServiceScope.Get<ILogger>().Error(ex);
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
        ServiceScope.Get<PlayerCollection>().Dispose();
        FontManager.Free();
        ContentManager.Free();
      }
      try
      {
        _device.TestCooperativeLevel();
      }
      catch (DeviceNotResetException)
      {
        ServiceScope.Get<ILogger>().Warn("GraphicsDevice:aquired directx device");
        try
        {
          ServiceScope.Get<ILogger>().Warn("GraphicsDevice:device reset");
          _setup.Reset();
          AdapterInformation adapterInfo = Manager.Adapters.Default;
          ServiceScope.Get<ILogger>().Debug("GraphicsDevice: directx reset {0}x{1} format: {2} {3} Hz", Width, Height,
                                            adapterInfo.CurrentDisplayMode.Format,
                                            adapterInfo.CurrentDisplayMode.RefreshRate);
          _backBuffer = _device.GetRenderTarget(0);
          ServiceScope.Get<ILogger>().Warn("GraphicsDevice:aquired device reset");
          return true;
        }
        catch (Exception ex)
        {
          ServiceScope.Get<ILogger>().Warn("GraphicsDevice:reset failed");
          ServiceScope.Get<ILogger>().Error(ex);
        }
      }
      catch (DeviceLostException) { }
      catch (Exception ex)
      {
        ServiceScope.Get<ILogger>().Warn("GraphicsDevice:TestCooperativeLevel failed");
        ServiceScope.Get<ILogger>().Error(ex);
      }
      return false;
    }
    /// <summary>
    /// Returns available display modes for the display device
    /// </summary>

    public static ItemsCollection DisplayModes
    {
      get { return _setup.DisplayModes; }
    }

    public static string DesktopDisplayMode
    {
      get { return _setup.DesktopDisplayMode; }
    }

    #endregion
  }
}
