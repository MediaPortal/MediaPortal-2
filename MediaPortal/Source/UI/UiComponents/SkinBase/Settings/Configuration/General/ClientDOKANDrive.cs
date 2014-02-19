#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Configuration.ConfigurationClasses;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Services.ResourceAccess.Settings;
using MediaPortal.Common.Settings;

namespace MediaPortal.UiComponents.SkinBase.Settings.Configuration.General
{
  public class ClientDOKANDrive : SingleSelectionList
  {
    public override void Load()
    {
      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
      char? currentDriveLetter = settingsManager.Load<ResourceMountingSettings>().DriveLetter;
      List<char> availableDriveLetters = settingsManager.Load<AvailableDriveLettersSettings>().AvailableDriveLetters.Where(d => d > 'C').ToList();
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
      base.Save();
      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
      ResourceMountingSettings settings = settingsManager.Load<ResourceMountingSettings>();
      settings.DriveLetter = _items[Selected].ToString()[0];
      settingsManager.Save(settings);
    }
  }
}
