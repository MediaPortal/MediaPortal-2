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
  /// Base class for configuration setting classes for configuring a single selection in a list of
  /// predefined (localizable) string items.
  /// </summary>
  public abstract class SingleSelectionList : ConfigItemList
  {
    #region Variables

    // Private because we want to make sure NotifyChange() is called on a change.
    private int _selected;

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the index of the selected item.
    /// </summary>
    public int Selected
    {
      get
      {
        if (_selected == -1 && _items.Count > 0)
          _selected = 0;
        return _selected;
      }
      set
      {
        if (_selected != value)
        {
          _selected = value;
          NotifyChange();
        }
      }
    }

    #endregion
  }
}
