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

using MediaPortal.Common;
using MediaPortal.Common.Commands;
using MediaPortal.Common.General;
using MediaPortal.Common.Runtime;
using MediaPortal.Common.Services.Settings;
using MediaPortal.Plugins.SystemStateMenu;
using MediaPortal.Plugins.SystemStateMenu.Settings;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Screens;
using System;
using System.Collections.Generic;

namespace MediaPortal.UiComponents.BlueVision.Models
{
  public class PowerMenuModel
  {
    public static readonly Guid MODEL_ID = new Guid("54F798AF-03E1-4A82-938E-D0D0DC608B1A");
    public const string KEY_SHUTDOWN_ACTION = "ShutdownAction";

    protected AbstractProperty _isMenuOpenProperty;
    protected SettingsChangeWatcher<SystemStateDialogSettings> _settings;
    protected ItemsList _menuItems;
    protected bool _needsUpdate;

    public PowerMenuModel()
    {
      _isMenuOpenProperty = new WProperty(typeof(bool), true);
      _menuItems = new ItemsList();
      _settings = new SettingsChangeWatcher<SystemStateDialogSettings>();
      _settings.SettingsChanged = OnSettingsChanged;
      _needsUpdate = true;
    }

    protected void OnSettingsChanged(object sender, EventArgs e)
    {
      _needsUpdate = true;
    }

    public ItemsList MenuItems
    {
      get
      {
        if (_needsUpdate)
        {
          UpdateMenuItems();
          _needsUpdate = false;
        }
        return _menuItems;
      }
    }

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

    public void SelectItem(ListItem item)
    {
      if (item == null)
        return;

      object systemStateActionOb;
      if (item.AdditionalProperties.TryGetValue(KEY_SHUTDOWN_ACTION, out systemStateActionOb))
        DoAction((SystemStateAction)systemStateActionOb);
    }

    protected void UpdateMenuItems()
    {
      _menuItems.Clear();
      List<SystemStateItem> shutdownItems = _settings.Settings.ShutdownItemList;
      if (shutdownItems != null)
      {
        foreach (SystemStateItem shutdownItem in shutdownItems)
        {
          if (!shutdownItem.Enabled)
            continue;
          ListItem item = new ListItem(Consts.KEY_NAME, Consts.GetResourceIdentifierForMenuItem(shutdownItem.Action));
          item.AdditionalProperties[KEY_SHUTDOWN_ACTION] = shutdownItem.Action;
          item.Command = new MethodDelegateCommand(() => SelectItem(item));
          _menuItems.Add(item);
        }
      }
      _menuItems.FireChange();
    }

    protected void DoAction(SystemStateAction action)
    {
      switch (action)
      {
        case SystemStateAction.Suspend:
          ServiceRegistration.Get<ISystemStateService>().Suspend();
          return;
        case SystemStateAction.Hibernate:
          ServiceRegistration.Get<ISystemStateService>().Hibernate();
          return;
        case SystemStateAction.Shutdown:
          ServiceRegistration.Get<ISystemStateService>().Shutdown();
          return;
        case SystemStateAction.Restart:
          ServiceRegistration.Get<ISystemStateService>().Restart();
          return;
        case SystemStateAction.Logoff:
          ServiceRegistration.Get<ISystemStateService>().Logoff();
          return;
        case SystemStateAction.CloseMP:
          ServiceRegistration.Get<IScreenControl>().Shutdown();
          return;
        case SystemStateAction.MinimizeMP:
          ServiceRegistration.Get<IScreenControl>().Minimize();
          return;
      }
    }
  }
}
