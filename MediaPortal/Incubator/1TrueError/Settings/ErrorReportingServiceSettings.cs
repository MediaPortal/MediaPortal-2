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

using System.Collections.Generic;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Settings;

namespace MediaPortal.Plugins.OneTrueError.Settings
{
  public class ErrorReportingServiceSettings
  {
    #region Exclusion filters

    /// <summary>
    /// Defines a list of exception that are expected and appearing very often. Such exceptions only cause a lot of false reports, so exclude them.
    /// </summary>
    private static readonly List<string> EXCEPTION_EXCLUDE_FILTERS = new List<string>
    {
      // TVE 3.5 names
      "Mediaportal.TV.Server.TVLibrary.Interfaces.TvExceptionNoPMT",
      "Mediaportal.TV.Server.TVLibrary.Interfaces.TvExceptionNoSignal",
      "Mediaportal.TV.Server.TVLibrary.Interfaces.TvExceptionSWEncoderMissing",
      "Mediaportal.TV.Server.TVLibrary.Interfaces.TvExceptionServiceNotRunning",
      "Mediaportal.TV.Server.TVLibrary.Interfaces.TvExceptionTuneCancelled",
      "Mediaportal.TV.Server.TVLibrary.Interfaces.TvExceptionTunerLoadFailed",

      // TVE 3 names
      "TvLibrary.TvExceptionNoPMT",
      "TvLibrary.TvExceptionNoSignal",
      "TvLibrary.TvExceptionSWEncoderMissing",
      "TvLibrary.TvExceptionTuneCancelled",
    };

    private List<string> _exceptionExcludedList = new List<string>();

    #endregion

    public ErrorReportingServiceSettings()
    {
      ExceptionExcludedList = EXCEPTION_EXCLUDE_FILTERS;
    }

    [Setting(SettingScope.Global, LogLevel.Error)]
    public LogLevel MinReportLevel { get; set; }

    [Setting(SettingScope.Global)]
    public List<string> ExceptionExcludedList
    {
      get { return _exceptionExcludedList; }
      set { _exceptionExcludedList = new List<string>(value); }
    }
  }
}
