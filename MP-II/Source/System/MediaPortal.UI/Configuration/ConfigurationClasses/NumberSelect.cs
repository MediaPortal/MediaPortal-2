#region Copyright (C) 2007-2008 Team MediaPortal

/*
 *  Copyright (C) 2007-2008 Team MediaPortal
 *  http://www.team-mediaportal.com
 *
 *  This file is part of MediaPortal II
 *
 *  MediaPortal II is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  MediaPortal II is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
 * 
 */

#endregion

namespace MediaPortal.Configuration.ConfigurationClasses
{
  /// <summary>
  /// Base class for configuration setting classes for configuring a single number
  /// (floating point or integer).
  /// </summary>
  public abstract class NumberSelect : ConfigSetting
  {
    #region Enums

    /// <summary>
    /// Specifies the type of the number, fixed point or floating point.
    /// </summary>
    public enum NumberType
    {
      /// <summary>
      /// Specifies that the number is an integer.
      /// </summary>
      Integer,

      /// <summary>
      /// Specifies that the number is a double.
      /// </summary>
      FloatingPoint
    }

    #endregion

    #region Variables

    protected double _value;
    protected NumberType _type;

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the value.
    /// </summary>
    public double Value
    {
      get { return _value; }
      set
      {
        if (_value != value)
        {
          _value = value;
          NotifyChange();
        }
      }
    }

    /// <summary>
    /// Gets the type of the number.
    /// </summary>
    public NumberType ValueType
    {
      get { return _type; }
      protected set { _type = value; }
    }

    #endregion
  }
}
