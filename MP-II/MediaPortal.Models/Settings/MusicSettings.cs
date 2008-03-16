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

using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using MediaPortal.Core;
using MediaPortal.Core.Collections;
using MediaPortal.Core.Localisation;
using MediaPortal.Presentation.WindowManager;
using MediaPortal.Presentation.MenuManager;
using MediaPortal.Core.Messaging;
using MediaPortal.Core.Settings;

using BassPlayer;

namespace Settings
{
  public class MusicSettings
  {
    ItemsCollection _mainMenu;
    BassPlayerSettings _settings;

    public MusicSettings()
    {
      // Load the Settings
      _settings = new BassPlayerSettings();
      ServiceScope.Get<ISettingsManager>().Load(_settings);
    }

    /// <summary>
    /// Exposes the main Music-settings menu to the skin
    /// </summary>
    /// <value>The main menu.</value>
    public ItemsCollection MainMenu
    {
      get
      {
        if (_mainMenu == null)
        {
          IMenuCollection menuCollect = ServiceScope.Get<IMenuCollection>();
          _mainMenu = new ItemsCollection(menuCollect.GetMenu("settings-music-main"));
        }
        return _mainMenu;
      }
    }

    /// <summary>
    /// Sends a Notification that the setting has changed
    /// </summary>
    private void NotifySettingsChanged()
    {
      MPMessage msg = new MPMessage();
      msg.MetaData["action"] = "settingschanged";
      IQueue queue = ServiceScope.Get<IMessageBroker>().Get("bass");
      queue.Send(msg);
    }

    public bool IsEnabledFadeOut
    {
      get { return _settings.SoftStop; }
    }

    public void EnableFadeOut()
    {
      _settings.SoftStop = true;
      ServiceScope.Get<ISettingsManager>().Save(_settings);
      NotifySettingsChanged();
    }
    public void DisableFadeOut()
    {
      _settings.SoftStop = false;
      ServiceScope.Get<ISettingsManager>().Save(_settings);
      NotifySettingsChanged();
    }

    public bool IsGaplessPlayback
    {
      get { return _settings.GaplessPlayback; }
    }

    public void DisableGapless()
    {
      _settings.GaplessPlayback = false;
      ServiceScope.Get<ISettingsManager>().Save(_settings);
      NotifySettingsChanged();
    }
    public void EnableGapless()
    {
      _settings.GaplessPlayback = true;
      ServiceScope.Get<ISettingsManager>().Save(_settings);
      NotifySettingsChanged();
    }
  }
}
