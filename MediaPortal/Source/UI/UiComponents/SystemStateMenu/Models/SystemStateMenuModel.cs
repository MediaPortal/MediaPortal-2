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

using MediaPortal.Common.General;
using System;

namespace MediaPortal.Plugins.SystemStateMenu.Models
{
  public class SystemStateMenuModel : BaseSystemStateModel
  {
    public const string SYSTEM_STATE_MENU_MODEL_ID_STR = "B348CBF3-ABCF-4D3D-9397-DBA2FFA49FD6";
    public static readonly Guid SYSTEM_STATE_MENU_MODEL_ID = new Guid(SYSTEM_STATE_MENU_MODEL_ID_STR);

    protected AbstractProperty _isMenuOpenProperty = new WProperty(typeof(bool), true);

    public AbstractProperty IsMenuOpenProperty
    {
      get { return _isMenuOpenProperty; }
    }

    /// <summary>
    /// Gets or sets an indicator if the menu is open (<c>true</c>) or closed (<c>false</c>).
    /// </summary>
    public bool IsMenuOpen
    {
      get { return (bool)_isMenuOpenProperty.GetValue(); }
      set { _isMenuOpenProperty.SetValue(value); }
    }

    /// <summary>
    /// Toggles the menu state from open to close and back.
    /// </summary>
    public void ToggleMenu()
    {
      IsMenuOpen = !IsMenuOpen;
    }

    /// <summary>
    /// Opens the menu by setting the <see cref="IsMenuOpen"/> to <c>true</c>.
    /// </summary>
    public void OpenMenu()
    {
      IsMenuOpen = true;
    }

    /// <summary>
    /// Closes the menu by setting the <see cref="IsMenuOpen"/> to <c>false</c>.
    /// </summary>
    public void CloseMenu()
    {
      IsMenuOpen = false;
    }
  }
}