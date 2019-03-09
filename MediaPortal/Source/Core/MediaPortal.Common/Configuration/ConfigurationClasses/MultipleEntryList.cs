#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

using System.Collections.Generic;

namespace MediaPortal.Common.Configuration.ConfigurationClasses
{
  /// <summary>
  /// Base class for configuration setting classes for configuring a list of string entries.
  /// </summary>
  public abstract class MultipleEntryList : ConfigSetting
  {
    #region Variables

    /// <summary>
    /// The content of the MultipleEntryList.
    /// </summary>
    protected IList<string> _lines = new List<string>();

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the lines.
    /// </summary>
    public IList<string> Lines
    {
      get { return _lines; }
      set
      {
        _lines = value;
        NotifyChange();
      }
    }

    /// <summary>
    /// Returns the width in characters, the GUI should use for this setting.
    /// </summary>
    public abstract int DisplayLength { get; }

    /// <summary>
    /// Returns the height in lines, the GUI should use for this setting.
    /// </summary>
    public abstract int DisplayHeight { get; }

    #endregion
  }
}
