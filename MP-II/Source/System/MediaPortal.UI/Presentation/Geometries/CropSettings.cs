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

using System.Drawing;

namespace MediaPortal.UI.Presentation.Geometries
{
  public class CropSettings
  {
    #region Protected fields

    private int _top;
    private int _bottom;
    private int _left;
    private int _right;

    #endregion

    #region Ctor

    public CropSettings() : this(0, 0, 0, 0) { }

    public CropSettings(int top, int bottom, int left, int right)
    {
      _top = top;
      _bottom = bottom;
      _left = left;
      _right = right;
    }

    #endregion

    #region Public properties

    /// <summary>
    /// Number of scanlines to remove at the top of the picture.
    /// </summary>
    public int Top
    {
      get { return _top; }
      set { _top = value; }
    }

    /// <summary>
    /// Number of scanlines to remove at the bottom of the picture.
    /// </summary>
    public int Bottom
    {
      get { return _bottom; }
      set { _bottom = value; }
    }

    /// <summary>
    /// Number of columns to remove from the left side of the picture.
    /// </summary>
    public int Left
    {
      get { return _left; }
      set { _left = value; }
    }

    /// <summary>
    /// Number of columns to remove from the right side of the picture.
    /// </summary>
    public int Right
    {
      get { return _right; }
      set { _right = value; }
    }

    #endregion

    /// <summary>
    /// Ensures that the crop settings makes sense, ie, dont crop more than the the available image area.
    /// Also ensures that the crop values are positive.
    /// </summary>
    public CropSettings EnsureSanity(int ImageWidth, int ImageHeight)
    {
      CropSettings S = new CropSettings(_top, _bottom, _left, _right);

      if (S._right < 0)
        S._right = 0;

      if (S._left < 0)
        S._left = 0;

      if (S._top < 0)
        S._top = 0;

      if (S._bottom < 0)
        S._bottom = 0;

      if (S._right + S._left >= ImageWidth)
        S._right = S._left = 0;

      if (S._top + S._bottom >= ImageHeight)
        S._top = S._bottom = 0;

      return S;
    }

    /// <summary>
    /// Applies this cropping data to the specified <paramref name="rectangle"/>.
    /// </summary>
    /// <param name="rectangle">Rectangle to crop.</param>
    public void AdjustSource(ref Rectangle rectangle)
    {
      rectangle.Y += _top;
      rectangle.Height -= _top;
      rectangle.Height -= _bottom;

      rectangle.X += _left;
      rectangle.Width -= _left;
      rectangle.Width -= _right;
    }
  }
}
