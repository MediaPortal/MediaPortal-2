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

using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.SkinEngine.Controls.Panels
{
  public enum GridUnitType
  {
    Auto = 0,
    Pixel = 1,
    Star = 2,
  }

  public class GridLength: IDeepCopyable
  {
    #region Private fields

    GridUnitType _unitType;
    double _value = 0;
    double _finalValue = 0;

    #endregion

    #region Ctor

    public GridLength(double value)
    {
      _unitType = GridUnitType.Pixel;
      _value = value;
    }

    public GridLength()
    {
      _unitType = GridUnitType.Auto;
    }

    public GridLength(GridUnitType unitType, double value)
    {
      _unitType = unitType;
      _value = value;
    }

    public virtual void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      GridLength gl = (GridLength) source;
      _unitType = copyManager.GetCopy(gl._unitType);
      _value = copyManager.GetCopy(gl._value);
      _finalValue = copyManager.GetCopy(gl._finalValue);
    }

    #endregion

    /// <summary>
    /// Gets a value indicating whether length is in pixels.
    /// </summary>
    public bool IsAbsolute
    {
      get { return _unitType == GridUnitType.Pixel; }
    }

    /// <summary>
    /// Gets a value indicating whether length is determined by control size.
    /// </summary>
    public bool IsAuto
    {
      get { return _unitType == GridUnitType.Auto; }
    }

    /// <summary>
    /// Gets a value indicating whether length is a percentage of the control size
    /// </summary>
    public bool IsStar
    {
      get { return _unitType == GridUnitType.Star; }
    }

    public double Value
    {
      get { return _value; }
    }

    /// <summary>
    /// Gets the type of the grid unit.
    /// </summary>
    /// <value>The type of the grid unit.</value>
    public GridUnitType GridUnitType
    {
      get { return _unitType; }
    }

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

    public double Length
    {
      get { return _finalValue; }
      set { _finalValue = value; }
    }
  }
}
