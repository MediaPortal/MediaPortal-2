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
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
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
    public static bool UseBatching = false;

    #region Private fields

    private static readonly AbstractProperty _windowSizeProperty = new SProperty(typeof(SizeF), new SizeF(1920, 1080));
    private static readonly WeakEventMulticastDelegate _skinResourcesChangedDelegate = new WeakEventMulticastDelegate();
    private static SkinResources _skinResources = new Skin("[not initialized]"); // Avoid initialization issues. So we don't need to check "if SkinResources == null" every time
    // FIXME Albert78: Those should be Stack, not List
    private static List<ExtendedMatrix> _combinedRenderTransforms = new List<ExtendedMatrix>();
    private static List<ExtendedMatrix> _combinedLayoutTransforms = new List<ExtendedMatrix>();
    private static Stack<Rectangle> _scissorRects = new Stack<Rectangle>();
    private static Stack<double> _opacity = new Stack<double>();
    private static double _finalOpacity = 1.0;
    private static ExtendedMatrix _finalRenderTransform = new ExtendedMatrix();
    private static ExtendedMatrix _finalLayoutTransform = new ExtendedMatrix();
    private static Form _form;
    private static bool _isRendering = false;
    private static AbstractProperty _zoomProperty = new WProperty(typeof(SizeF), new SizeF(1, 1));
    private static float _Zorder = 1.0f;
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

    // Zorder ranges from from 0.0f (as close as you can get) to 1.0f (as far away as you can get). 
    // start far away and move closer.
    public static void ResetZorder()
    {
      _Zorder = 1.0f;
    }

    public static void SetZOrder(float value)
    {
      _Zorder = value;
    }

    public static float GetZorder()
    {
      _Zorder -= 0.001f;
      return _Zorder;
    }

    public static void AddOpacity(double opacity)
    {
      _finalOpacity *= opacity;
      _opacity.Push(_finalOpacity);
    }

    public static void RemoveOpacity()
    {
      _opacity.Pop();
      _finalOpacity = _opacity.Count > 0 ? _opacity.Peek() : 1.0;
    }

    public static double Opacity
    {
      get { return _finalOpacity; }
    }

    public static void AddScissorRect(Rectangle scissorRect)
    {
      Rectangle? finalScissorRect = FinalScissorRect;
      if (finalScissorRect.HasValue)
        scissorRect.Intersect(finalScissorRect.Value);
      _scissorRects.Push(scissorRect);
    }

    public static void RemoveScissorRect()
    {
      _scissorRects.Pop();
    }

    public static Rectangle? FinalScissorRect
    {
      get { return _scissorRects.Count > 0 ? new Rectangle?(_scissorRects.Peek()) : null; }
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

    public static AbstractProperty ZoomProperty
    {
      get { return _zoomProperty; }
    }

    public static SizeF Zoom
    {
      get { return (SizeF) _zoomProperty.GetValue(); }
      set { _zoomProperty.SetValue(value); }
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
      get { return GraphicsDevice.DesktopHeight / (float) _skinResources.SkinHeight; }
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
      get { return GraphicsDevice.DesktopWidth / (float) _skinResources.SkinWidth; }
    }

    public static float FPS
    {
      get { return _fps; }
      set { _fps = value; }
    }

    public static List<ExtendedMatrix> CombinedRenderTransforms
    {
      get { return _combinedRenderTransforms; }
      set
      {
        _combinedRenderTransforms = value;
        UpdateFinalRenderTransform();
      }
    }

    /// <summary>
    /// Adds the transform matrix to the current transform stack
    /// </summary>
    /// <param name="matrix">The matrix.</param>
    public static void AddRenderTransform(ExtendedMatrix matrix)
    {
      if (_combinedRenderTransforms.Count > 0)
        _combinedRenderTransforms.Add(matrix.Multiply(_combinedRenderTransforms[_combinedRenderTransforms.Count - 1]));
      else
        _combinedRenderTransforms.Add(matrix);
      UpdateFinalRenderTransform();
    }

    /// <summary>
    /// Removes the top transform from the transform stack.
    /// </summary>
    public static void RemoveRenderTransform()
    {
      if (_combinedRenderTransforms.Count > 0)
        _combinedRenderTransforms.RemoveAt(_combinedRenderTransforms.Count - 1);
      UpdateFinalRenderTransform();
    }

    /// <summary>
    /// Sets the final transform.
    /// </summary>
    public static void UpdateFinalRenderTransform()
    {
      _finalRenderTransform = _combinedRenderTransforms.Count > 0 ? _combinedRenderTransforms[_combinedRenderTransforms.Count - 1] : new ExtendedMatrix();
    }

    /// <summary>
    /// Gets or sets the final render transform matrix.
    /// </summary>
    public static ExtendedMatrix FinalRenderTransform
    {
      get { return _finalRenderTransform; }
      set { _finalRenderTransform = value; }
    }

    public static void AddLayoutTransform(ExtendedMatrix matrix)
    {
      if (_combinedLayoutTransforms.Count > 0)
        _combinedLayoutTransforms.Add(matrix.Multiply(_combinedLayoutTransforms[_combinedLayoutTransforms.Count - 1]));
      else
        _combinedLayoutTransforms.Add(matrix);
      UpdateFinalLayoutTransform();
    }

    /// <summary>
    /// Removes the top transform from the transform stack.
    /// </summary>
    public static void RemoveLayoutTransform()
    {
      if (_combinedLayoutTransforms.Count > 0)
        _combinedLayoutTransforms.RemoveAt(_combinedLayoutTransforms.Count - 1);
      UpdateFinalLayoutTransform();
    }

    /// <summary>
    /// Sets the final transform.
    /// </summary>
    public static void UpdateFinalLayoutTransform()
    {
      _finalLayoutTransform = _combinedLayoutTransforms.Count > 0 ? _combinedLayoutTransforms[_combinedLayoutTransforms.Count - 1] : new ExtendedMatrix();
    }

    /// <summary>
    /// Gets or sets the final matrix.
    /// </summary>
    public static ExtendedMatrix FinalLayoutTransform
    {
      get { return _finalLayoutTransform; }
      set { _finalLayoutTransform = value; }
    }

    /// <summary>
    /// Gets or sets a value indicating whether application is currently processing in its render loop.
    /// When this property is <c>false</c>, an error might have occured and the render thread stopped.
    /// </summary>
    public static bool IsRendering
    {
      get { return _isRendering; }
      set { _isRendering = value; }
    }
  }
}
