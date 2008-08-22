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

using System;
using System.Collections.Generic;
using System.Text;

namespace MediaPortal.Configuration
{

  /// <summary>
  /// Specifies the type of the setting. Settings are rendered according to their type.
  /// </summary>
  public enum SettingType
  {
    /// <summary>
    /// Specifies that the type isn't known.
    /// </summary>
    Unknown,
    /// <summary>
    /// Specifies that the setting is a section. Sections can be rendered as treenodes, pages, ...
    /// </summary>
    Section,
    /// <summary>
    /// Specifies that the setting is a group. A group should always be seen as a member of a Section.
    /// </summary>
    Group,
    /// <summary>
    /// Specifies that the setting is limited to 2 options: yes or no. Likely to be rendered as a checkbox.
    /// </summary>
    YesNo,
    /// <summary>
    /// Specifies that only one member of the setting can be selected, likely to be rendered as a ComboBox or a list of RadioButtons.
    /// </summary>
    SingleSelectionList,
    /// <summary>
    /// Specifies that multiple members of the setting can be selected, likely to be rendered as a CheckedListBox.
    /// </summary>
    MultipleSelectionList,
    /// <summary>
    /// Specifies that the setting is a value/string, likely to be rendered as a TextBox.
    /// </summary>
    Entry,
    /// <summary>
    /// Specifies that the setting contains multiple entries, likely to be rendered as a multiline TextBox.
    /// </summary>
    MultipleEntryList,
    /// <summary>
    /// Specifies that the setting is a list of items which can be sorted by the user.
    /// </summary>
    PreferenceList,
    /// <summary>
    /// Specifies that the setting is a path.
    /// </summary>
    Path,
    /// <summary>
    /// Specifies that the setting is a number, likely to be rendered as a NumericUpDown or a TextBox.
    /// </summary>
    NumberSelect,
    /// <summary>
    /// Specifies that the setting is a number which can only be set between specific bounds,
    /// likely to be rendered as a Slider, a NumericUpDown, or a TextBox.
    /// </summary>
    LimitedNumberSelect
  }
}
