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
using System.Globalization;

namespace Presentation.SkinEngine.Controls.Visuals
{
  public class Thickness
  {
    protected static readonly NumberFormatInfo NUMBERFORMATINFO = CultureInfo.InvariantCulture.NumberFormat;

    float _left;
    float _top;
    float _right;
    float _bottom;

    public Thickness(string thicknessString)
    {
      string[] coords = thicknessString.Split(new char[] { ',' });
      if (coords.Length == 1)
      {
        float.TryParse(coords[0], NumberStyles.Any, NUMBERFORMATINFO, out _left);
        _top = _left;
        _right = _left;
        _bottom = _left;
      }
      if (coords.Length == 2)
      {
        float.TryParse(coords[0], NumberStyles.Any, NUMBERFORMATINFO, out _left);
        float.TryParse(coords[1], NumberStyles.Any, NUMBERFORMATINFO, out _top);
        _right = _left;
        _bottom = _top;
      }
      if (coords.Length == 4)
      {
        float.TryParse(coords[0], NumberStyles.Any, NUMBERFORMATINFO, out _left);
        float.TryParse(coords[1], NumberStyles.Any, NUMBERFORMATINFO, out _top);
        float.TryParse(coords[2], NumberStyles.Any, NUMBERFORMATINFO, out _right);
        float.TryParse(coords[3], NumberStyles.Any, NUMBERFORMATINFO, out _bottom);
      }
    }

    public Thickness(float left, float top, float right, float bottom)
    {
      _left = left;
      _top = top;
      _right = right;
      _bottom = bottom;
    }

    public float Left 
    {
      get { return _left; }
      set { _left = value; } 
    }
    public float Top 
    {
      get { return _top; }
      set { _top = value; } 
    }
    
    public float Right
    {
      get { return _right; }
      set { _right = value; }
    }
    public float Bottom 
    {
      get { return _bottom; }
      set { _bottom = value; } 
    }
  }
}
