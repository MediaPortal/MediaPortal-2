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

using log4net.Core;
using MediaPortal.Common.Configuration.ConfigurationClasses;
using MediaPortal.UiComponents.Diagnostics.Service;

namespace MediaPortal.UiComponents.Diagnostics.Settings.Configuration
{
  public class DiagnosticsSettingsLogLevelConfiguration : YesNo
  {

    #region Public Methods

    public override void Load()
    {
      Level activeLevel = DiagnosticsHandler.GetLogLevel();
      _yes = activeLevel == Level.All;
    }

    public override void Save()
    {
      Level desired = _yes ? Level.All : Level.Info;
      DiagnosticsHandler.SetLogLevel(desired);
    }

    #endregion Public Methods

  }
}
