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
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using MediaPortal.Common.General;
using MediaPortal.UI.SkinEngine.DirectX;
using MediaPortal.UI.SkinEngine.DirectX.RenderStrategy;
using SlimDX.Direct3D9;

namespace MediaPortal.UI.SkinEngine.SkinManagement
{
  public delegate void SkinResourcesChangedHandler(SkinResources newResources);

  /// <summary>
  /// Holds context variables which are used by the skin controls. This class may also be accessed from other plugins, for example video players.
  /// </summary>
  public static class SkinContext
  {
    #region Private fields

    private static readonly AbstractProperty _windowSizeProperty = new SProperty(typeof(Size), new Size(1920, 1080));
    private static readonly WeakEventMulticastDelegate _skinResourcesChangedDelegate = new WeakEventMulticastDelegate();
    private static SkinResources _skinResources = new Skin("[not initialized]"); // Avoid initialization issues. So we don't need to check "if SkinResources == null" every time
    private static DateTime _frameRenderingStartTime;
    private static float _fps = 0;
    private static volatile Thread _renderThread = null;

    public static uint SystemTickCount;

    #endregion

    public static event SkinResourcesChangedHandler SkinResourcesChanged
    {
      add { _skinResourcesChangedDelegate.Attach(value); }
      remove { _skinResourcesChangedDelegate.Detach(value); }
    }

    public static AbstractProperty WindowSizeProperty
    {
      get { return _windowSizeProperty; }
    }

    /// <summary>
    /// Returns the application window's extends. This is the maximum available space to render.
    /// </summary>
    public static Size WindowSize
    {
      get { return (Size) _windowSizeProperty.GetValue(); }
      internal set { _windowSizeProperty.SetValue(value); }
    }

    public static DateTime FrameRenderingStartTime
    {
      get { return _frameRenderingStartTime; }
      internal set { _frameRenderingStartTime = value; }
    }

    /// <summary>
    /// Gets or sets the Application's main windows form.
    /// </summary>
    public static Form Form
    {
      get { return GraphicsDevice.Setup.RenderTarget; }
    }

    public static Thread RenderThread
    {
      get { return _renderThread; }
      internal set { _renderThread = value; }
    }

    /// <summary>
    /// Gets the DirectX device.
    /// </summary>
    public static DeviceEx Device
    {
      get { return GraphicsDevice.Device; }
    }

    /// <summary>
    /// Returns the Direct3D instance of the SkinEngine.
    /// </summary>
    public static Direct3DEx Direct3D
    {
      get { return MPDirect3D.Direct3D; }
    }

    /// <summary>
    /// Gets the current RenderStrategy (affects frame sync and present mode).
    /// </summary>
    public static IRenderStrategy RenderStrategy
    {
      get { return GraphicsDevice.RenderStrategy; }
    }

    /// <summary>
    /// Toggles between different RenderStrategies (affects frame sync and present mode).
    /// </summary>
    public static void NextRenderStrategy()
    {
      GraphicsDevice.NextRenderStrategy();
    }

    /// <summary>
    /// Indicates if device is using multi sample antialiasing (MSAA).
    /// </summary>
    public static bool IsMultiSample
    {
      get { return GraphicsDevice.Setup.IsMultiSample; }
    }

    /// <summary>
    /// Exposes an event of the rendering process. It gets fired immediately after DeviceEx.BeginScene.
    /// </summary>
    public static event EventHandler DeviceSceneBegin
    {
      add { GraphicsDevice.DeviceSceneBegin += value; }
      remove { GraphicsDevice.DeviceSceneBegin -= value; }
    }

    /// <summary>
    /// Exposes an event of the rendering process. It gets fired immediately before DeviceEx.EndScene.
    /// </summary>
    public static event EventHandler DeviceSceneEnd
    {
      add { GraphicsDevice.DeviceSceneEnd += value; }
      remove { GraphicsDevice.DeviceSceneEnd -= value; }
    }

    /// <summary>
    /// Exposes an event of the rendering process. It gets fired immediately after DeviceEx.PresentEx.
    /// </summary>
    public static event EventHandler DeviceScenePresented
    {
      add { GraphicsDevice.DeviceScenePresented += value; }
      remove { GraphicsDevice.DeviceScenePresented -= value; }
    }

    /// <summary>
    /// Gets the back-buffer width of the DeviceEx.
    /// </summary>
    public static int BackBufferWidth
    {
      get { return GraphicsDevice.Width; }
    }

    /// <summary>
    /// Gets the back-buffer height of the DeviceEx.
    /// </summary>
    public static int BackBufferHeight
    {
      get { return GraphicsDevice.Height; }
    }

    /// <summary>
    /// Returns the current display mode used in the SkinEngine.
    /// </summary>
    public static DisplayMode CurrentDisplayMode
    {
      get
      {
        int ordinal = GraphicsDevice.Device.Capabilities.AdapterOrdinal;
        AdapterInformation adapterInfo = MPDirect3D.Direct3D.Adapters[ordinal];
        return adapterInfo.CurrentDisplayMode;
      }
    }

    /// <summary>
    /// Gets or sets the skin resources currently in use.
    /// A query to this resource collection will automatically fallback on the
    /// next resource collection in the priority chain. For example,
    /// if a requested resource is not present, it will fallback to the
    /// default theme/skin.
    /// </summary>
    public static SkinResources @SkinResources
    {
      get { return _skinResources; }
      internal set
      {
        _skinResources = value;
        _skinResourcesChangedDelegate.Fire(new object[] {_skinResources});
      }
    }

    /// <summary>
    /// Defines the maximum zoom in the Y direction. Setting a Y zoom of <see cref="MaxZoomHeight"/>
    /// given the current active skin will fill the skin contents to the complete Y area.
    /// </summary>
    /// <remarks>
    /// X and Y zoom settings are independent because of different aspect ratios.
    /// Please also note that at a given time, screenfiles from multiple skins may be shown at the
    /// screen (window plus dialog). Everytime it is possible that a skinfile from the default skin
    /// is shown. The returned value by this property only takes respect of the current active skin.
    /// </remarks>
    public static float MaxZoomHeight
    {
      get { return GraphicsDevice.Setup.DesktopHeight / (float) _skinResources.SkinHeight; }
    }

    /// <summary>
    /// Gets the maximum zoom in the X direction. Setting an X zoom of <see cref="MaxZoomWidth"/>
    /// given the current active skin will fill the skin contents to the complete X area.
    /// </summary>
    /// <remarks>
    /// See the comment in <see cref="MaxZoomHeight"/>.
    /// </remarks>
    public static float MaxZoomWidth
    {
      get { return GraphicsDevice.Setup.DesktopWidth / (float) _skinResources.SkinWidth; }
    }

    /// <summary>
    /// Gets the current average fraction frames per seconds.
    /// </summary>
    public static float FPS
    {
      get { return _fps; }
      internal set { _fps = value; }
    }
  }
}
