#region Copyright (C) 2008 Team MediaPortal

/*
    Copyright (C) 2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

namespace MediaPortal.Core.TaskScheduler
{
  /// <summary>
  /// Specifies the type of <see cref="TaskMessage"/>
  /// </summary>
  public enum TaskMessageType
  {
    DUE,
    CHANGED,
    DELETED,
    EXPIRED
  }

  /// <summary>
  /// Messages to be sent by the <see cref="TaskScheduler"/>.
  /// </summary>
  public class TaskMessage
  {
    #region Private fields

    private Task _task;
    private TaskMessageType _type;

    #endregion

    #region Ctor

    public TaskMessage(Task task, TaskMessageType type)
    {
      _task = task;
      _type = type;
    }

    #endregion

    #region Properties

    /// <summary>
    /// The <see cref="Task"/> which triggered this message.
    /// </summary>
    public Task Task
    {
      get { return _task; }
      set { _task = value; }
    }

    /// <summary>
    /// Type of message.
    /// </summary>
    public TaskMessageType Type
    {
      get { return _type; }
      set { _type = value; }
    }

    #endregion
  }
}
