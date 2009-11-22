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

using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals
{
  public class Thickness : IDeepCopyable
  {
    float _left = 0;
    float _top = 0;
    float _right = 0;
    float _bottom = 0;

    public Thickness()
    {
    }

    public Thickness(float thickness)
    {
      _left = thickness;
      _top = thickness;
      _right = thickness;
      _bottom = thickness;
    }

    public Thickness(float left, float top)
    {
      _left = left;
      _top = top;
      _right = left;
      _bottom = top;
    }

    public Thickness(float left, float top, float right, float bottom)
    {
      _left = left;
      _top = top;
      _right = right;
      _bottom = bottom;
    }

    public void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Thickness b = (Thickness) source;
      _left = b._left;
      _top = b._top;
      _right = b._right;
      _bottom = b._bottom;
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
