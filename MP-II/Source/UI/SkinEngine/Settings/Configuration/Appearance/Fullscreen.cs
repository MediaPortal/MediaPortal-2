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

using MediaPortal.Configuration.ConfigurationClasses;
using MediaPortal.Core;
using MediaPortal.Presentation.Screens;

namespace MediaPortal.SkinEngine.Settings.Configuration.Appearance
{
  /// <summary>
  /// Configuration setting class to change the fullscreen setting.
  /// </summary>
  public class Fullscreen : YesNo
  {
    #region Public Methods

    public override void Load()
    {
      _yes = SettingsManager.Load<AppSettings>().FullScreen;
    }

    public override void Save()
    {
      AppSettings settings = SettingsManager.Load<AppSettings>();
      settings.FullScreen = _yes;
      SettingsManager.Save(settings);
    }

    public override void Apply()
    {
      IScreenControl sc = ServiceScope.Get<IScreenControl>();
      if (_yes)
        sc.SwitchMode(ScreenMode.FullScreenWindowed, FPS.None);
      else
        sc.SwitchMode(ScreenMode.NormalWindowed, FPS.None);
    }

    #endregion
  }
}
