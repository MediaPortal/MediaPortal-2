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

using MediaPortal.Core.Configuration.ConfigurationClasses;
using MediaPortal.UI.Services.Players.Settings;

namespace MediaPortal.UI.SkinEngine.Settings.Configuration.Players
{
  public class CrossFadingEnabled : YesNo
  {
    #region Public Methods

    public override void Load()
    {
      _yes = SettingsManager.Load<FadingSettings>().CrossFadingEnabled;
    }

    public override void Save()
    {
      FadingSettings settings = SettingsManager.Load<FadingSettings>();
      settings.CrossFadingEnabled = _yes;
      SettingsManager.Save(settings);
    }

    #endregion
  }
}