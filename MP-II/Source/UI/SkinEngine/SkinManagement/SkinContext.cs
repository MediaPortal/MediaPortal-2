#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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
using System.Drawing;
using System.Windows.Forms;
using MediaPortal.Core.General;

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

    private static string _skinName = null;
    private static string _themeName = null;
    private static SkinResources _skinResources = new SkinResources("[not initialized]"); // Avoid initialization issues. So we don't need to check "if SkinResources == null" every time
    private static int _skinWidth = 0;
    private static int _skinHeight = 0;
    // FIXME Albert78: Those should be Stack, not List
    private static List<ExtendedMatrix> _groupTransforms = new List<ExtendedMatrix>();
    private static List<ExtendedMatrix> _layoutTransforms = new List<ExtendedMatrix>();
    private static Stack<Rectangle> _scissorRects = new Stack<Rectangle>();
    private static Stack<double> _opacity = new Stack<double>();
    private static double _finalOpacity = 1.0;
    private static ExtendedMatrix _finalTransform = new ExtendedMatrix();
    private static ExtendedMatrix _finalLayoutTransform = new ExtendedMatrix();
    private static ExtendedMatrix _tempTransform = null;
    private static Form _form;
    private static bool _isRendering = false;
    private static Property _zoomProperty = new Property(typeof(SizeF), new SizeF(1, 1));
    private static float _Zorder = 1.0f;
    private static DateTime _now;
    private static float _fps = 0;

    public static uint TimePassed;

    #endregion

    public static event SkinResourcesChangedHandler SkinResourcesChanged;

    public static DateTime Now
    {
      get { return _now; }
      set { _now = value; }
    }

    // Zorder ranges from from 0.0f (as close as you can get) to 1.0f (as far away as you can get). 
    // start far away and move closer.
    public static void ResetZorder()
    {
      _Zorder = 1.0f;
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
    /// <value>The form.</value>
    public static Form Form
    {
      get { return _form; }
      set { _form = value; }
    }

    /// <summary>
    /// Gets or sets the width of the current skin.
    /// </summary>
    public static int SkinWidth
    {
      get { return _skinWidth; }
      set { _skinWidth = value; }
    }

    /// <summary>
    /// Gets or sets the height of the current skin.
    /// </summary>
    public static int SkinHeight
    {
      get { return _skinHeight; }
      set { _skinHeight = value; }
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
        if (SkinResourcesChanged != null)
          SkinResourcesChanged(_skinResources);
      }
    }

    /// <summary>
    /// Gets the name of the currently active skin.
    /// </summary>
    public static string SkinName
    {
      get { return _skinName; }
      set { _skinName = value; }
    }

    /// <summary>
    /// Gets the name of the currently active theme.
    /// </summary>
    public static string ThemeName
    {
      get { return _themeName; }
      set { _themeName = value; }
    }

    public static Property ZoomProperty
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
      get { return GraphicsDevice.DesktopHeight / (float) SkinHeight; }
    }

    /// <summary>
    /// Defines the maximum zoom in the X direction. Setting a X zoom of <see cref="MaxZoomWidth"/>
    /// given the current active skin will fill the skin contents to the complete X area.
    /// </summary>
    /// <remarks>
    /// See the comment in <see cref="MaxZoomHeight"/>.
    /// </remarks>
    public static float MaxZoomWidth
    {
      get { return GraphicsDevice.DesktopWidth / (float) SkinWidth; }
    }

    public static float FPS
    {
      get { return _fps; }
      set { _fps = value; }
    }

    public static List<ExtendedMatrix> Transforms
    {
      get { return _groupTransforms; }
      set { _groupTransforms = value; }
    }

    /// <summary>
    /// Adds the transform matrix to the current transform stack
    /// </summary>
    /// <param name="matrix">The matrix.</param>
    public static void AddTransform(ExtendedMatrix matrix)
    {
      if (_groupTransforms.Count > 0)
        _groupTransforms.Add(matrix.Multiply(_groupTransforms[_groupTransforms.Count - 1]));
      else
        _groupTransforms.Add(matrix);
      UpdateFinalTransform(_groupTransforms[_groupTransforms.Count - 1]);
    }

    /// <summary>
    /// Removes the top transform from the transform stack.
    /// </summary>
    public static void RemoveTransform()
    {
      if (_groupTransforms.Count > 0)
        _groupTransforms.RemoveAt(_groupTransforms.Count - 1);
      if (_groupTransforms.Count > 0)
        UpdateFinalTransform(_groupTransforms[_groupTransforms.Count - 1]);
      else
        UpdateFinalTransform(new ExtendedMatrix());
    }

    /// <summary>
    /// Sets the final transform.
    /// </summary>
    /// <param name="matrix">The transform to set.</param>
    public static void UpdateFinalTransform(ExtendedMatrix matrix)
    {
      _finalTransform = matrix;
    }

    /// <summary>
    /// Gets or sets the final matrix.
    /// </summary>
    public static ExtendedMatrix FinalTransform
    {
      get { return _finalTransform; }
      set { _finalTransform = value; }
    }

    public static void AddLayoutTransform(ExtendedMatrix matrix)
    {
      if (_layoutTransforms.Count > 0)
        _layoutTransforms.Add(matrix.Multiply(_layoutTransforms[_layoutTransforms.Count - 1]));
      else
        _layoutTransforms.Add(matrix);
      UpdateFinalLayoutTransform(_layoutTransforms[_layoutTransforms.Count - 1]);
    }

    /// <summary>
    /// Removes the top transform from the transform stack.
    /// </summary>
    public static void RemoveLayoutTransform()
    {
      if (_layoutTransforms.Count > 0)
        _layoutTransforms.RemoveAt(_layoutTransforms.Count - 1);
      if (_layoutTransforms.Count > 0)
        UpdateFinalLayoutTransform(_layoutTransforms[_layoutTransforms.Count - 1]);
      else
        UpdateFinalLayoutTransform(new ExtendedMatrix());
    }

    /// <summary>
    /// Sets the final transform.
    /// </summary>
    /// <param name="matrix">The matrix.</param>
    public static void UpdateFinalLayoutTransform(ExtendedMatrix matrix)
    {
      _finalLayoutTransform = matrix;
    }

    /// <summary>
    /// Gets or sets the final matrix.
    /// </summary>
    /// <value>The final matrix.</value>
    public static ExtendedMatrix FinalLayoutTransform
    {
      get { return _finalLayoutTransform; }
      set { _finalLayoutTransform = value; }
    }

    /// <summary>
    /// Gets or sets the temporary transform.
    /// </summary>
    /// <value>The temporary transform.</value>
    public static ExtendedMatrix TemporaryTransform
    {
      get { return _tempTransform; }
      set { _tempTransform = value; }
    }

    /// <summary>
    /// Gets or sets a value indicating whether application is rendering or not.
    /// </summary>
    /// <value>
    /// 	<c>true</c> if this instance is rendering; otherwise, <c>false</c>.
    /// </value>
    public static bool IsRendering
    {
      get { return _isRendering; }
      set { _isRendering = value; }
    }
  }
}
