#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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

using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Configuration.ConfigurationClasses;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Services.ResourceAccess.Settings;

namespace MediaPortal.Plugins.ServerSettings.Settings.Configuration
{
  public class ServerDOKANDrive : SingleSelectionList, IDisposable
  {
    public ServerDOKANDrive()
    {
      Enabled = false;
      ConnectionMonitor.Instance.RegisterConfiguration(this);
    }

    public override void Load()
    {
      if (!Enabled)
        return;
      IServerSettingsClient serverSettings = ServiceRegistration.Get<IServerSettingsClient>();
      char? currentDriveLetter = serverSettings.Load<ResourceMountingSettings>().DriveLetter;
      List<char> availableDriveLetters = serverSettings.Load<AvailableDriveLettersSettings>().AvailableDriveLetters.Where(d => d > 'C').ToList();
      // The list of available drive letters won't contain the current DOKAN drive, so add it manually here.
      if (currentDriveLetter.HasValue)
      {
        if (!availableDriveLetters.Contains(currentDriveLetter.Value))
        {
          availableDriveLetters.Add(currentDriveLetter.Value);
          availableDriveLetters.Sort();
        }
        Selected = availableDriveLetters.IndexOf(currentDriveLetter.Value);
      }
      _items = availableDriveLetters.Select(d => LocalizationHelper.CreateStaticString(d.ToString())).ToList();
    }

    public override void Save()
    {
      if (!Enabled)
        return;

      base.Save();
      IServerSettingsClient serverSettings = ServiceRegistration.Get<IServerSettingsClient>();
      ResourceMountingSettings settings = serverSettings.Load<ResourceMountingSettings>();
      settings.DriveLetter = _items[Selected].ToString()[0];
      serverSettings.Save(settings);
    }

    public void Dispose()
    {
      ConnectionMonitor.Instance.UnregisterConfiguration(this);
    }
  }
}
