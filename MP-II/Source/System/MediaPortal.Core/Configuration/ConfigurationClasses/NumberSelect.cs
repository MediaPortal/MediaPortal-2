#region Copyright (C) 2007-2010 Team MediaPortal

/*
 *  Copyright (C) 2007-2010 Team MediaPortal
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

namespace MediaPortal.Core.Configuration.ConfigurationClasses
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

    protected double _value = 0;
    protected NumberType _type = NumberType.Integer;
    protected double _step = 1;
    protected int _maxNumDigits = 2;

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the value to be edited in this configuration object.
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

    /// <summary>
    /// Gets or sets the step value the UI will add or subtract to/from the <see cref="Value"/> when the buttons
    /// Up or Down are pressed.
    /// </summary>
    public double Step
    {
      get { return _step; }
      set { _step = value; }
    }

    public int MaxNumDigits
    {
      get { return _maxNumDigits; }
      set { _maxNumDigits = value; }
    }

    #endregion
  }
}
