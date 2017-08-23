#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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

using MediaPortal.Common.Configuration.ConfigurationClasses;
using MediaPortal.Common.Logging;
using MediaPortal.UiComponents.Diagnostics.Service;

namespace MediaPortal.UiComponents.Diagnostics.Settings.Configuration
{
  public class DiagnosticsSettingsLogLevelConfiguration : YesNo
  {

    #region Public Methods

    public override void Load()
    {
      LogLevel activeLevel = DiagnosticsHandler.GetLogLevel();
      _yes = activeLevel == LogLevel.All;
    }

    public override void Save()
    {
      LogLevel desired = _yes ? LogLevel.All : LogLevel.Information;
      DiagnosticsHandler.SetLogLevel(desired);
    }

    #endregion Public Methods

  }
}
