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
using System.IO;
using System.Windows.Forms;
using MediaPortal.Presentation.Properties;
using Presentation.SkinEngine.MpfElements.Resources;
using SlimDX;
using Presentation.SkinEngine.Players;
using Presentation.SkinEngine.Players.Geometry;
using MediaPortal.Core;
using MediaPortal.Presentation.Players;


namespace Presentation.SkinEngine.SkinManagement
{                         
  /// <summary>
  /// Holds context variables which are used by the skin controls.
  /// </summary>
  public class SkinContext
  {
    public static bool UseBatching = false;
    public static bool IsValid = false;

    #region Private fields

    private static SkinManager _skinManager = new SkinManager();
    private static Skin _skin = null;
    private static Theme _theme = null;
    private static List<ExtendedMatrix> _groupTransforms = new List<ExtendedMatrix>();
    private static List<ExtendedMatrix> _layoutTransforms = new List<ExtendedMatrix>();
    private static List<double> _opacity = new List<double>();
    private static double _finalOpacity = 1.0;
    private static ExtendedMatrix _finalTransform = new ExtendedMatrix();
    private static ExtendedMatrix _finalLayoutTransform = new ExtendedMatrix();
    private static ExtendedMatrix _tempTransform = null;
    private static Form _form;
    private static bool _gradientInUse;
    private static Vector3 _gradientPosition;
    private static Vector2 _gradientSize;
    private static DateTime _mouseTimer;
    private static bool _isRendering = false;
    private static Geometry _geometry = new Geometry();
    private static CropSettings _cropSettings = new CropSettings();
    public static DateTime _now;
    public static bool HandlingInput;
    private static Property _mouseUsedProperty = new Property(typeof(bool), false);
    private static Property _timeProperty = new Property(typeof(DateTime), DateTime.Now);
    private static bool _mouseHidden = false;
    private static DateTime _lastAction = DateTime.Now;
    public static uint TimePassed;
    public static System.Drawing.SizeF _skinZoom = new System.Drawing.SizeF(1, 1);
    public static float Z = 0.0f;

    #endregion

    [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "ShowCursor")]
    internal extern static Int32 ShowCursor(bool bShow);

    /// <summary>
    /// Prepares the skin and theme, this will load the skin and theme instances and
    /// set it as the current skin. After calling this method, the <see cref="Skin"/> and
    /// <see cref="Theme"/> properties contents can be requested.
    /// To activate the skin, <see cref="ActivateTheme"/> has to be called
    /// after this method.
    /// </summary>
    /// <param name="skinName">The skin to be prepared.</param>
    /// <param name="themeName">The theme name for the specified skin to be prepared,
    /// or <c>null</c> for the default theme of the skin.</param>
    public static void PrepareSkinAndTheme(string skinName, string themeName)
    {
      _skin = _skinManager.Skins[skinName];
      if (_skin == null)
        _skin = _skinManager.DefaultSkin;
      if (_skin == null)
        throw new Exception(string.Format("Could not load skin '{0}'", skinName));
      _theme = themeName == null ? null : _skin.Themes[themeName];
      if (_theme == null)
        _theme = _skin.DefaultTheme;
      ApplicationResources.Instance.Clear();
    }

    /// <summary>
    /// Has to be called after a call to <see cref="PrepareSkinAndTheme(string,string)"/>
    /// to activate the theme.
    /// This method must not be called before the DirectX render device is ready.
    /// </summary>
    public static void ActivateTheme()
    {
      ResourceDictionary rd = _theme == null ? null : _theme.LoadStyles();
      if (rd != null)
        ApplicationResources.Instance.Merge(rd);
    }

    public static void AddOpacity(double opacity)
    {
      _finalOpacity *= opacity;
      _opacity.Add(_finalOpacity);
    }

    public static void RemoveOpacity()
    {
      _opacity.RemoveAt(_opacity.Count - 1);
      if (_opacity.Count > 0)
      {
        _finalOpacity = _opacity[_opacity.Count - 1];
      }
      else
        _finalOpacity = 1.0;
    }

    /// <summary>
    /// Returns a resource which may be located in a relative path under the directory
    /// of the current skin or unter the directory of the current theme.
    /// </summary>
    /// <param name="resourceName">Relative file name of the resource to retrieve,
    /// for example <i>shaders/blur.fx</i>. The file name must be relative, else the
    /// resource won't be found.</param>
    /// <returns>File describing the requested resource file.</returns>
    public static FileInfo GetResourceFromThemeOrSkin(string resourceName)
    {
      FileInfo result = _theme == null ? null : _theme.GetResourceFile(resourceName);
      if (result == null)
        result = _skin == null ? null : _skin.GetResourceFile(resourceName);
      return result;
    }

    public static double Opacity
    {
      get { return _finalOpacity; }
    }

    /// <summary>
    /// Gets or sets the Application's main windows form.
    /// </summary>
    /// <value>The form.</value>
    public static Form Form
    {
      get { return _form; }
      set
      {
        _form = value;
        Application.Idle += Application_Idle;
      }
    }

    /// <summary>
    /// Gets or sets the skin currently in use.
    /// </summary>
    public static Skin @Skin
    {
      get { return _skin; }
      set { _skin = value; }
    }

    /// <summary>
    /// Gets or sets the theme currently in use.
    /// </summary>
    public static Theme @Theme
    {
      get { return _theme; }
      set { _theme = value; }
    }

    public static System.Drawing.SizeF Zoom
    {
      get { return _skinZoom; }
      set { _skinZoom = value; }
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
      {
        _groupTransforms.Add(matrix.Multiply(_groupTransforms[_groupTransforms.Count - 1]));
      }
      else
      {
        _groupTransforms.Add(matrix);
      }
      UpdateFinalTransform(_groupTransforms[_groupTransforms.Count - 1]);
    }

    /// <summary>
    /// Removes the top transform from the transform stack.
    /// </summary>
    public static void RemoveTransform()
    {
      if (_groupTransforms.Count > 0)
      {
        _groupTransforms.RemoveAt(_groupTransforms.Count - 1);
      }
      if (_groupTransforms.Count > 0)
      {
        UpdateFinalTransform(_groupTransforms[_groupTransforms.Count - 1]);
      }
      else
      {
        UpdateFinalTransform(new ExtendedMatrix());
      }
    }

    /// <summary>
    /// Sets the final transform.
    /// </summary>
    /// <param name="matrix">The matrix.</param>
    public static void UpdateFinalTransform(ExtendedMatrix matrix)
    {
      _finalTransform = matrix;
    }

    /// <summary>
    /// Gets or sets the final matrix.
    /// </summary>
    /// <value>The final matrix.</value>
    public static ExtendedMatrix FinalMatrix
    {
      get { return _finalTransform; }
      set { _finalTransform = value; }
    }


    public static void AddLayoutTransform(ExtendedMatrix matrix)
    {
      if (_layoutTransforms.Count > 0)
      {
        _layoutTransforms.Add(matrix.Multiply(_layoutTransforms[_layoutTransforms.Count - 1]));
      }
      else
      {
        _layoutTransforms.Add(matrix);
      }
      UpdateFinalLayoutTransform(_layoutTransforms[_layoutTransforms.Count - 1]);
    }

    /// <summary>
    /// Removes the top transform from the transform stack.
    /// </summary>
    public static void RemoveLayoutTransform()
    {
      if (_layoutTransforms.Count > 0)
      {
        _layoutTransforms.RemoveAt(_layoutTransforms.Count - 1);
      }
      if (_layoutTransforms.Count > 0)
      {
        UpdateFinalLayoutTransform(_layoutTransforms[_layoutTransforms.Count - 1]);
      }
      else
      {
        UpdateFinalLayoutTransform(new ExtendedMatrix());
      }
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
    /// Gets or sets a value indicating whether mouse was used used the last 5 seconds
    /// </summary>
    /// <value><c>true</c> if [mouse used]; otherwise, <c>false</c>.</value>
    public static bool MouseUsed
    {
      get { return (bool)_mouseUsedProperty.GetValue(); }
      set
      {
        if (value != MouseUsed)
        {
          _mouseUsedProperty.SetValue(value);
          if (value)
          {
            _mouseTimer = DateTime.Now;
            if (_mouseHidden)
            {
              ShowCursor(true);
              _mouseHidden = false;
            }
          }
        }
        else if (value)
        {
          _mouseTimer = DateTime.Now;
          _lastAction = DateTime.Now;
        }
      }
    }

    private static void Application_Idle(object sender, EventArgs e)
    {
      if (MouseUsed)
      {
        TimeSpan ts = DateTime.Now - _mouseTimer;
        if (ts.TotalSeconds >= 2)
        {
          MouseUsed = false;
          if (!_mouseHidden)
          {
            if (ServiceScope.Get<IApplication>().IsFullScreen)
            {
              ShowCursor(false);
              _mouseHidden = true;
            }
          }
        }
      }
    }

    public static Property MouseUsedProperty
    {
      get { return _mouseUsedProperty; }
      set { _mouseUsedProperty = value; }
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

    /// <summary>
    /// Gets or sets the current geometry.
    /// </summary>
    /// <value>The geometry.</value>
    public static Geometry Geometry
    {
      get { return _geometry; }
      set { _geometry = value; }
    }


    /// <summary>
    /// Gets or sets the crop settings.
    /// </summary>
    /// <value>The crop settings.</value>
    public static CropSettings CropSettings
    {
      get { return _cropSettings; }
      set
      {
        if (value != null)
          _cropSettings = value;
      }
    }

    public static DateTime Now
    {
      get { return _now; }
      set
      {
        _now = value;
        _timeProperty.SetValue(_now);
      }
    }

    public static Property TimeProperty
    {
      get { return _timeProperty; }
      set { _timeProperty = value; }
    }

    public static bool ScreenSaverActive
    {
      get
      {
        TimeSpan ts = DateTime.Now - _lastAction;
        if (ts.TotalSeconds < 30) return false;
        if (!ServiceScope.Get<IApplication>().IsFullScreen) return false;
        PlayerCollection players = ServiceScope.Get<PlayerCollection>();
        if (players.Count > 0)
        {
          if (players[0].IsVideo) return false;
        }
        return true;
      }
      set
      {
        _lastAction = DateTime.Now;
      }
    }

  }
}
