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
using System;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Core.Properties;
#endregion

namespace SkinEngine.Controls.Panels
{
  public enum GridUnitType
  {
    Auto = 0,
    Pixel = 1,
    Star = 2,
  }

  public class GridLength
  {
    GridUnitType _unitType;
    double _value;

    public GridLength(double value)
    {
      _unitType = GridUnitType.Pixel;
      _value = value;
    }

    public GridLength()
    {
      _unitType = GridUnitType.Auto;
      _value = 0;
    }

    public GridLength(GridUnitType unitType, double value)
    {
      _unitType = unitType;
      _value = value;
    }

    /// <summary>
    /// Gets a value indicating whether the length is absolute.
    /// </summary>
    /// <value>
    /// 	<c>true</c> if this instance is absolute; otherwise, <c>false</c>.
    /// </value>
    public bool IsAbsolute
    {
      get
      {
        return _unitType == GridUnitType.Pixel;
      }
    }

    /// <summary>
    /// Gets a value indicating whether length is determined by control size.
    /// </summary>
    /// <value><c>true</c> if this instance is auto; otherwise, <c>false</c>.</value>
    public bool IsAuto
    {
      get
      {
        return _unitType == GridUnitType.Auto;
      }
    }

    /// <summary>
    /// Gets a value indicating whether length is a percentage of the control size
    /// </summary>
    /// <value><c>true</c> if this instance is star; otherwise, <c>false</c>.</value>
    public bool IsStar
    {
      get
      {
        return _unitType == GridUnitType.Star;
      }
    }

    /// <summary>
    /// Gets the value.
    /// </summary>
    /// <value>The value.</value>
    public double Value
    {
      get
      {
        return _value;
      }
    }

    /// <summary>
    /// Gets the type of the grid unit.
    /// </summary>
    /// <value>The type of the grid unit.</value>
    public GridUnitType GridUnitType
    {
      get
      {
        return _unitType;
      }
    }

    /// <summary>
    /// Gets the length.
    /// </summary>
    /// <param name="totalLength">The total length.</param>
    /// <returns></returns>
    public double GetLength(double totalLength,  ColumnDefinitionsCollection collection, float scale)
    {
      if (IsAbsolute) return _value * scale;
      if (IsStar) return ((_value / 100.0) * totalLength);
      double lenLeft=totalLength;
      int countLeft=0;
      for (int i = 0; i < collection.Count; ++i)
      {
        ColumnDefinition def = (ColumnDefinition)collection[i];
        if (def.Width.IsAbsolute)
        {
          lenLeft -= (def.Width.Value * scale);
        }
        else if (def.Width.IsStar)
        {
          lenLeft -=  ((_value / 100.0) * totalLength);
        }
        else countLeft++;
      }
      if (lenLeft <= 0) return 0.0;
      return (lenLeft/ ((double)countLeft));
    }

    
    public double GetLength(double totalLength,  RowDefinitionsCollection collection, float scale)
    {
      if (IsAbsolute) return _value * scale;
      if (IsStar) return ((_value / 100.0) * totalLength);
      double lenLeft=totalLength;
      int countLeft=0;
      for (int i = 0; i < collection.Count; ++i)
      {
        RowDefinition def = (RowDefinition)collection[i];
        if (def.Height.IsAbsolute)
        {
          lenLeft -= (def.Height.Value * scale);
        }
        else if (def.Height.IsStar)
        {
          lenLeft -=  ((_value / 100.0) * totalLength);
        }
        else countLeft++;
      }
      if (lenLeft <= 0) return 0.0;
      return (lenLeft/ ((double)countLeft));
    }
  }
}
