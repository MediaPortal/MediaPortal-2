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
using MediaPortal.UI.Presentation.Screens;

namespace MediaPortal.UI.SkinEngine.Settings.Configuration.Appearance
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
      IScreenControl sc = ServiceScope.Get<IScreenControl>();
      if (_yes)
        sc.SwitchMode(ScreenMode.FullScreenWindowed);
      else
        sc.SwitchMode(ScreenMode.NormalWindowed);
    }

    #endregion
  }
}
