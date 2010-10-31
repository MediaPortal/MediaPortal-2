#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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
using MediaPortal.Core.General;
using MediaPortal.UI.SkinEngine.DirectX;

namespace MediaPortal.UI.SkinEngine.SkinManagement
{                         
  public delegate void SkinResourcesChangedHandler(SkinResources newResources);

  /// <summary>
  /// Holds context variables which are used by the skin controls.
  /// </summary>
  public class SkinContext
  {
    #region Private fields

    private static readonly AbstractProperty _windowSizeProperty = new SProperty(typeof(Size), new Size(1920, 1080));
    private static readonly WeakEventMulticastDelegate _skinResourcesChangedDelegate = new WeakEventMulticastDelegate();
    private static SkinResources _skinResources = new Skin("[not initialized]"); // Avoid initialization issues. So we don't need to check "if SkinResources == null" every time
    private static Form _form;
    private static Thread _renderThread = null;
    private static DateTime _frameRenderingStartTime;
    private static float _fps = 0;

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
      set { _windowSizeProperty.SetValue(value); }
    }

    public static DateTime FrameRenderingStartTime
    {
      get { return _frameRenderingStartTime; }
      set { _frameRenderingStartTime = value; }
    }

    /// <summary>
    /// Gets or sets the Application's main windows form.
    /// </summary>
    public static Form Form
    {
      get { return _form; }
      set { _form = value; }
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
      set
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
    /// TODO: to be removed
    public static float MaxZoomHeight
    {
      get { return GraphicsDevice.DesktopHeight / (float) _skinResources.SkinHeight; }
    }

    /// <summary>
    /// Gets the maximum zoom in the X direction. Setting an X zoom of <see cref="MaxZoomWidth"/>
    /// given the current active skin will fill the skin contents to the complete X area.
    /// </summary>
    /// <remarks>
    /// See the comment in <see cref="MaxZoomHeight"/>.
    /// </remarks>
    /// TODO: to be removed
    public static float MaxZoomWidth
    {
      get { return GraphicsDevice.DesktopWidth / (float) _skinResources.SkinWidth; }
    }

    public static float FPS
    {
      get { return _fps; }
      set { _fps = value; }
    }

    /// <summary>
    /// Gets or sets the thread which is currently executing the render loop.
    /// When this property is <c>null</c>, an error might have occured and the render thread stopped.
    /// </summary>
    public static Thread RenderThread
    {
      get { return _renderThread; }
      set { _renderThread = value; }
    }
  }
}
