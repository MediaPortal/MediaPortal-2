#region Copyright (C) 2007-2009 Team MediaPortal

/*
 *  Copyright (C) 2007-2009 Team MediaPortal
 *  http://www.team-mediaportal.com
 *
 *  This file is part of MediaPortal 2
 *
 *  MediaPortal 2 is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  MediaPortal 2 is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
 * 
 */

#endregion

using System;

namespace MediaPortal.Manager
{
  /// <summary>
  /// Position stores all data related to positioning controls.
  /// </summary>
  internal class Position : ICloneable
  {
    #region Variables

    private bool _rightToLeft;
    private int _linePos;
    private int _lineHeight;
    private int _itemHeight;
    private int _margin;
    private int _indent;
    private int _startColOne;
    private int _widthColOne;
    private int _startColTwo;
    private int _widthColTwo;
    private int _tabIndex;

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the current position. [y-axis]
    /// </summary>
    public int LinePosition
    {
      get { return _linePos; }
      set { _linePos = value; }
    }

    /// <summary>
    /// Gets or sets the height of lines.
    /// This value should be larger than ItemHeight.
    /// </summary>
    public int LineHeight
    {
      get { return _lineHeight; }
      set { _lineHeight = value; }
    }

    /// <summary>
    /// Gets or sets the height of items.
    /// This value should be smaller than LineHeight.
    /// </summary>
    public int ItemHeight
    {
      get { return _itemHeight; }
      set { _itemHeight = value; }
    }

    /// <summary>
    /// Gets or sets the margin.
    /// </summary>
    /// <remarks>
    /// Margin can be used to specify a distance between controls.
    /// </remarks>
    public int Margin
    {
      get { return _margin; }
      set { _margin = value; }
    }

    /// <summary>
    /// Gets or sets the start of the first column. [x-axis]
    /// </summary>
    public int StartColumnOne
    {
      get { return _startColOne; }
      set { _startColOne = (value < 0 ? 0 : value); }
    }

    /// <summary>
    /// Gets or sets the start of the second column. [x-axis]
    /// </summary>
    public int StartColumnTwo
    {
      get { return _startColTwo; }
      set { _startColTwo = (value < 0 ? 0 : value); }
    }

    /// <summary>
    /// Gets or sets the width of the first column.
    /// </summary>
    public int WidthColumnOne
    {
      get { return _widthColOne; }
      set { _widthColOne = (value < 1 ? 1 : value); }
      // can't be less than 1: we don't want a DivideByZeroException
    }

    /// <summary>
    /// Gets or sets the width of the second column.
    /// </summary>
    public int WidthColumnTwo
    {
      get { return _widthColTwo; }
      set { _widthColTwo = (value < 1 ? 1 : value); }
      // can't be less than 1: we don't want a DivideByZeroException
    }

    /// <summary>
    /// Gets the width from the start of column one to the end of column two.
    /// </summary>
    public int Width
    {
      get { return _widthColTwo + _startColTwo - _startColOne; }
    }

    /// <summary>
    /// Gets the full width.
    /// </summary>
    public int FullWidth
    {
      get { return _widthColTwo + _startColTwo; }
    }

    /// <summary>
    /// Gets or sets the current tabindex.
    /// </summary>
    public int TabIndex
    {
      get { return _tabIndex; }
      set { _tabIndex = value; }
    }

    /// <summary>
    /// Gets the next tabindex and increments the current tabindex.
    /// </summary>
    public int NextTabIndex
    {
      get { return ++_tabIndex; }
    }

    /// <summary>
    /// Gets if values are calculated for a right to left layout.
    /// </summary>
    public bool RightToLeft
    {
      get { return _rightToLeft; }
    }

    /// <summary>
    /// Gets or sets the indentation of controls.
    /// </summary>
    public int Indent
    {
      get { return _indent; }
      set { _indent = value; }
    }

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of Position, using default values based on fullWidth.
    /// </summary>
    /// <param name="rightToLeft">Calculate values for a right to left layout?</param>
    /// <param name="fullWidth">Width of the full layout.</param>
    public Position(bool rightToLeft, int fullWidth)
    {
      this._margin = 12;
      this._tabIndex = 0;
      this._linePos = 5;  // some controls decrement the lineposition and are clipped if _linePos is zero
      this._rightToLeft = rightToLeft;
      this._lineHeight = 25;
      this._itemHeight = 13;
      this._indent = 4;
      fullWidth -= 22;  // makes sure a vertical scrollbar won't create a horizontal one
      if (!rightToLeft)
      {
        this._startColOne = 0;
        this.WidthColumnOne = (int)(fullWidth * 0.70);
        this._startColTwo = this._widthColOne;
        this.WidthColumnTwo = fullWidth - this._startColTwo;
      }
      else
      {
        this._startColTwo = 0;
        this.WidthColumnTwo = (int)(fullWidth * 0.30);
        this._startColOne = this._widthColTwo;
        this.WidthColumnOne = fullWidth - this._startColOne;
      }
    }

    public Position(bool rightToLeft, int lineHeight, int margin, int indent, int itemHeight, int startColumnOne, int widthColumnOne,
      int startColumnTwo, int widthColumnTwo)
    {
      this._rightToLeft = rightToLeft;
      this._lineHeight = lineHeight;
      this._itemHeight = itemHeight;
      this._margin = margin;
      this._indent = indent;
      this._startColOne = startColumnOne;
      this._startColTwo = startColumnTwo;
      this._widthColOne = widthColumnOne;
      this._widthColTwo = widthColumnTwo;
      this._tabIndex = 0;
      this._linePos = 0;
      
    }

    /// <summary>
    /// Private copy constructor.
    /// </summary>
    /// <param name="other"></param>
    private Position(Position other)
    {
      this._rightToLeft = other._rightToLeft;
      this._linePos = other._linePos;
      this._lineHeight = other._lineHeight;
      this._itemHeight = other._itemHeight;
      this._margin = other._margin;
      this._indent = other._indent;
      this._startColOne = other._startColOne;
      this._widthColOne = other._widthColOne;
      this._startColTwo = other._startColTwo;
      this._widthColTwo = other._widthColTwo;
      this._tabIndex = other._tabIndex;
    }

    #endregion

    #region ICloneable Members

    public object Clone()
    {
      return new Position(this);
    }

    #endregion
  }
}