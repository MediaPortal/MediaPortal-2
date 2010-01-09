#region Copyright (C) 2007-2010 Team MediaPortal

/*
 *  Copyright (C) 2007-2010 Team MediaPortal
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

namespace MediaPortal.Core.Configuration.ConfigurationClasses
{
  /// <summary>
  /// Base class for configuration setting classes for configuring a single string value.
  /// </summary>
  public abstract class Entry : ConfigSetting
  {
    #region Variables

    /// <summary>
    /// Value of the entry.
    /// </summary>
    protected string _value;

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the value of the current entry.
    /// </summary>
    public string Value
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
    /// Returns the width in characters, the GUI should use for this setting.
    /// </summary>
    public abstract int DisplayLength { get; }

    #endregion
  }
}
