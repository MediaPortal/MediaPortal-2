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

#region Copyright (C) 2005-2008 Team MediaPortal

/* 
 *	Copyright (C) 2005-2008 Team MediaPortal
 *	http://www.team-mediaportal.com
 *
 *  This Program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2, or (at your option)
 *  any later version.
 *   
 *  This Program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *   
 *  You should have received a copy of the GNU General Public License
 *  along with GNU Make; see the file COPYING.  If not, write to
 *  the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA. 
 *  http://www.gnu.org/copyleft/gpl.html
 *
 */

#endregion

using System;

namespace MediaPortal.Core.Settings
{
  /// <summary>
  /// Enumerator for a setting's scope
  /// </summary>
  public enum SettingScope
  {
    Global = 1, // global setting, doesn't allow per user/per plugin override
    User = 2 // per user setting : allows per user storage
  }

  [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
  public sealed class SettingAttribute : Attribute
  {
    private SettingScope _settingScope;
    private object _DefaultValue;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="settingScope">Setting's scope</param>
    public SettingAttribute(SettingScope settingScope)
    {
      _settingScope = settingScope;
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="settingScope">Setting's scope</param>
    /// <param name="defaultValue">Default value</param>
    public SettingAttribute(SettingScope settingScope, object defaultValue)
    {
      _settingScope = settingScope;
      _DefaultValue = defaultValue;
    }

    /// <summary>
    /// Get/Set setting's scope (User/Global)
    /// </summary>
    public SettingScope SettingScope
    {
      get { return _settingScope; }
      set { _settingScope = value; }
    }

    /// <summary>
    /// Get/Set the default value
    /// </summary>
    public object DefaultValue
    {
      get { return _DefaultValue; }
      set { _DefaultValue = value; }
    }
  }
}
