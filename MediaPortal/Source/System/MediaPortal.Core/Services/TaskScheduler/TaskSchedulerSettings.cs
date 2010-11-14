#region Copyright (C) 2008 Team MediaPortal

/*
    Copyright (C) 2008 Team MediaPortal
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
using MediaPortal.Core.Settings;
using MediaPortal.Core.TaskScheduler;

namespace MediaPortal.Core.Services.TaskScheduler
{
  /// <summary>
  /// The TaskSchedulerSettings class is responsible for storing all settings of the <see cref="TaskScheduler"/>.
  /// It holds the last TaskID to be dealt and all <see cref="Task"/>s registered with the <see cref="TaskScheduler"/>.
  /// All <see cref="Task"/>s are stored in a <see cref="TaskCollection"/>.
  /// </summary>
  [Serializable]
  public class TaskSchedulerSettings
  {
    #region Consts

    public const int FIRST_TASK_ID = 0;

    #endregion

    #region Protected fields

    protected int _lastTaskId = FIRST_TASK_ID;
    protected TaskCollection _taskCollection = new TaskCollection();

    #endregion

    #region Public methods

    /// <summary>
    /// Gets the next LastTaskId to be assigned to a new <see cref="Task"/> and increments the stored LastTaskId.
    /// </summary>
    /// <returns>Next task ID.</returns>
    public int GetNextTaskID()
    {
      if (_lastTaskId == Int32.MaxValue)
        _lastTaskId = FIRST_TASK_ID;
      else
        _lastTaskId++;
      return _lastTaskId;
    }
    #endregion

    #region Properties

    /// <summary>
    /// Property which is used by the <see cref="ISettingsManager"/> to retrieve/set the last dealt LastTaskId.
    /// </summary>
    [Setting(SettingScope.Global, FIRST_TASK_ID)]
    public int LastTaskId
    {
      get { return _lastTaskId; }
      set { _lastTaskId = value; }
    }

    /// <summary>
    /// Property which is used by the <see cref="ISettingsManager"/> to retrieve/set list of registered <see cref="Task"/>s.
    /// Also used by the <see cref="TaskScheduler"/> to access the <see cref="TaskCollection"/>.
    /// </summary>
    [Setting(SettingScope.Global)]
    public TaskCollection TaskCollection
    {
      get { return _taskCollection; }
      set { _taskCollection = value; }
    }

    #endregion
  }
}
