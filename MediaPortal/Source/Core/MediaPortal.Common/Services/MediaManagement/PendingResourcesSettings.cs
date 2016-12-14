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
using MediaPortal.Common.Settings;

namespace MediaPortal.Common.Services.MediaManagement
{
  /// <summary>
  /// Stores the PendingResources of pending ImportJobs, if any
  /// </summary>
  /// <remarks>
  /// We do not store this in <see cref="ImporterWorkerSettings"/> because storing and loading these settings can
  /// take several seconds which may block other parts of the code that only want to access simple properties
  /// of the <see cref="ImporterWorkerSettings"/>
  /// </remarks>
  public class PendingResourcesSettings
  {
    #region Protected properties

    private List<ImportJobNewGen> _pendingImportJobs = new List<ImportJobNewGen>();

    #endregion

    [Setting(SettingScope.Global)]
    public List<ImportJobNewGen> PendingImportJobs
    {
      get { return _pendingImportJobs; }
      set { _pendingImportJobs = new List<ImportJobNewGen>(value); }
    }
  }
}

