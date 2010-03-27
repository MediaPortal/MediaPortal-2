#region Copyright (C) 2007-2010 Team MediaPortal

/*
 *  Copyright (C) 2007-2010 Team MediaPortal
 *  http://www.team-mediaportal.com
 *
 *  This file is part of MediaPortal 2
 *
 *  MediaPortal 2 is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  MediaPortal 2 is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
 * 
 */

#endregion

using MediaPortal.Core.Configuration.ConfigurationClasses;
using MediaPortal.Core;
using MediaPortal.Core.Settings;

namespace UiComponents.SkinBase.Settings.Configuration.General
{
  public class ServerListener : YesNo
  {
    #region Base overrides

    public override void Load()
    {
      ISettingsManager settingsManager = ServiceScope.Get<ISettingsManager>();
      SkinBaseSettings settings = settingsManager.Load<SkinBaseSettings>();
      _yes = settings.DisableServerListener;
    }

    public override void Save()
    {
      ISettingsManager settingsManager = ServiceScope.Get<ISettingsManager>();
      SkinBaseSettings settings = settingsManager.Load<SkinBaseSettings>();
      settings.DisableServerListener = _yes;
      settingsManager.Save(settings);
    }

    #endregion
  }
}
