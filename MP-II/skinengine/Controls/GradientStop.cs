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

namespace SkinEngine.Controls
{
  public class GradientStop
  {
    #region variables

    private float _offset;
    private float _color;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="GradientStop"/> class.
    /// </summary>
    /// <param name="offset">The offset.</param>
    /// <param name="color">The color.</param>
    public GradientStop(float offset, float color)
    {
      _offset = offset;
      _color = color;
    }

    /// <summary>
    /// Gets or sets the offset.
    /// </summary>
    /// <value>The offset.</value>
    public float Offset
    {
      get { return _offset; }
      set { _offset = value; }
    }

    /// <summary>
    /// Gets or sets the color.
    /// </summary>
    /// <value>The color.</value>
    public float Color
    {
      get { return _color; }
      set { _color = value; }
    }
  }
}