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

using System;
using System.Collections.Generic;
using MediaPortal.Common.Settings;
using MediaPortal.Common.TaskScheduler;

namespace MediaPortal.Common.Services.MediaManagement
{
  public class ImporterWorkerSettings
  {
    #region Protected properties

    protected List<ImportJob> _pendingImportJobs = new List<ImportJob>();

    #endregion

    /// <summary>
    /// Indicates whether the NewGen ImporterWorker should be used or the old one
    /// Todo: Remove this setting once the NewGen ImporterWorker actually works
    /// </summary>
    [Setting(SettingScope.Global, true)]
    public bool UseNewImporterWorker { get; set; }

    /// <summary>
    /// Gets or sets the Guid of the <see cref="Task"/> that is used for regular share imports.
    /// </summary>
    [Setting(SettingScope.Global)]
    public Guid ImporterScheduleId { get; set; }

    /// <summary>
    /// Gets or sets the start hour on current day for the importer task execution.
    /// </summary>
    [Setting(SettingScope.Global, 2.0d)]
    public double ImporterStartTime { get; set; }

    /// <summary>
    /// Gets or sets an indicator if the automatic importer should be enabled.
    /// </summary>
    [Setting(SettingScope.Global, true)]
    public bool EnableAutoRefresh { get; set; }

    [Setting(SettingScope.Global)]
    public List<ImportJob> PendingImportJobs
    {
      get { return _pendingImportJobs; }
      set { _pendingImportJobs = new List<ImportJob>(value); }
    }
  }
}
