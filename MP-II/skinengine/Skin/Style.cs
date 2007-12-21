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

using MediaPortal.Core.Properties;
using Microsoft.DirectX;
using SkinEngine.Controls;

namespace SkinEngine.Skin
{
  public class Style : Group
  {
    #region variables

    private Vector4 _move = new Vector4(0, 1, 0, 1);
    private Property _wrap;
    private Property _displayName;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="Style"/> class.
    /// </summary>
    /// <param name="parent">The parent.</param>
    public Style(Control parent)
      : base(parent)
    {
      _wrap = new Property(true);
    }


    /// <summary>
    /// Gets or sets a value indicating whether wrapping is enabled or disabled on the container 
    /// </summary>
    /// <value><c>true</c> if wrap; otherwise, <c>false</c>.</value>
    public bool Wrap
    {
      get { return (bool) _wrap.GetValue(); }
      set { _wrap.SetValue(value); }
    }

    public Property WrapProperty
    {
      get { return _wrap; }
      set { _wrap = value; }
    }

    /// <summary>
    /// Gets or sets the Display name used for this Style
    /// </summary>
    public string DisplayName
    {
      get { return (string)_displayName.GetValue(); }
      set { _displayName.SetValue(value); }
    }

    public Property DisplayNameProperty
    {
      get { return _displayName; }
      set { _displayName = value; }
    }

    /// <summary>
    /// Gets or sets the amount of listitems to skip when user reached up,down,left,right edge of the conatiner.
    /// </summary>
    /// <value>The move.</value>
    public Vector4 Move
    {
      get { return _move; }
      set { _move = value; }
    }

    /// <summary>
    /// Checks if a control is positioned at coordinates (x,y) 
    /// </summary>
    /// <param name="x">The x coordinate.</param>
    /// <param name="y">The y coordinate.</param>
    /// <returns></returns>
    public override bool HitTest(float x, float y)
    {
      bool success = false;
      for (int i = 0; i < _controls.Count; ++i)
      {
        success |= _controls[i].HitTest(x, y);
      }
      return success;
    }
  }
}