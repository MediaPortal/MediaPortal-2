#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.ComponentModel;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.UI.SkinEngine.MpfElements.Converters;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Panels
{
  public enum GridUnitType
  {
    Auto,
    AutoStretch,
    Pixel,
    Star,
  }

  [TypeConverter(typeof(MPFConverter<GridLength>))]
  public class GridLength : IDeepCopyable, ISkinEngineManagedObject
  {
    #region Private fields

    GridUnitType _unitType;
    double _value = 0;
    double _finalValue = 0;
    double _desiredLength = 0;

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
      _unitType = gl._unitType;
      _value = gl._value;
      _finalValue = gl._finalValue;
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
    /// Gets a value indicating whether length is determined by the child control size.
    /// </summary>
    public bool IsAuto
    {
      get { return _unitType == GridUnitType.Auto; }
    }

    /// <summary>
    /// Gets a value indicating whether length is determined by the child control size but stretched automatically
    /// if there is more space available.
    /// </summary>
    public bool IsAutoStretch
    {
      get { return _unitType == GridUnitType.AutoStretch; }
    }

    /// <summary>
    /// Returns the information indicating whether <see cref="Length"/> is a percentage
    /// of the parent control size.
    /// </summary>
    public bool IsStar
    {
      get { return _unitType == GridUnitType.Star; }
    }

    /// <summary>
    /// Gets the raw value given by the user.
    /// </summary>
    public double Value
    {
      get { return _value; }
    }

    /// <summary>
    /// Gets the type of the grid unit.
    /// </summary>
    public GridUnitType GridUnitType
    {
      get { return _unitType; }
    }

    /// <summary>
    /// Gets or sets the desirec length for the current cell.
    /// </summary>
    public double DesiredLength
    {
      get { return _desiredLength; }
      set { _desiredLength = value; }
    }

    /// <summary>
    /// Gets or sets the final calculated length for the current cell.
    /// </summary>
    public double Length
    {
      get { return _finalValue; }
      internal set { _finalValue = value; }
    }

    public override string ToString()
    {
      return GetType().Name + ", Value=" + _value + ", UnitType=" + _unitType + ", FinalValue=" + _finalValue;
    }
  }
}
