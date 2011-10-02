#region Copyright (C) 2007-2011 Team MediaPortal

/*
 *  Copyright (C) 2007-2011 Team MediaPortal
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
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
 * 
 */

#endregion

using MediaPortal.Common.Configuration.ConfigurationClasses;

namespace MediaPortal.UI.SkinEngine.Settings.Configuration.Appearance
{
  /// <summary>
  /// Configuration setting class to change the screen saver timeouts.
  /// </summary>
  public class ScreenSaverTimeout : LimitedNumberSelect
  {
    #region Public Methods

    public override void Load()
    {
      _type = NumberType.Integer;
      _step = 1.0;
      _lowerLimit = 1;
      _upperLimit = 30;
      _value = SettingsManager.Load<ScreenSaverSettings>().ScreenSaverTimeoutMin;
    }

    public override void Save()
    {
      base.Save();
      ScreenSaverSettings settings = SettingsManager.Load<ScreenSaverSettings>();
      settings.ScreenSaverTimeoutMin = (int)_value;
      SettingsManager.Save(settings);
    }

    #endregion
  }
}