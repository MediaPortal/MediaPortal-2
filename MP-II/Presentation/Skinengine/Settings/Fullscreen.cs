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

using System;
using System.Collections.Generic;
using System.Text;

using MediaPortal.Core;
using MediaPortal.Core.Settings;
using MediaPortal.Configuration.Settings;

namespace SkinEngine.Settings
{
  public class Fullscreen : YesNo
  {
    AppSettings _settings;

    public Fullscreen()
    {
      _settings = new AppSettings();
      ServiceScope.Get<ISettingsManager>().Load(_settings);
      _yes = _settings.FullScreen;
    }

    public override void Save()
    {
      _settings.FullScreen = _yes;
      ServiceScope.Get<ISettingsManager>().Save(_settings);
    }
  }
}
