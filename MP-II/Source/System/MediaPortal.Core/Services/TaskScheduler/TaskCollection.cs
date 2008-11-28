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

#region usings
using System;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;

using MediaPortal.Core.Settings;
using MediaPortal.Core.TaskScheduler;
#endregion

namespace MediaPortal.Core.Services.TaskScheduler
{
  /// <summary>
  /// Maintains a collection of scheduled <see cref="Task"/>s for the <see cref="TaskScheduler"/>.
  /// It always keeps the list of tasks sorted by next due datetime.
  /// </summary>
  public class TaskCollection
  {
    #region Protected fields

    protected List<Task> _tasks;
    protected TaskComparer _comparer;

    #endregion

    #region Ctor

    public TaskCollection()
    {
      _tasks = new List<Task>();
      _comparer = new TaskComparer();
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Adds a task to the TaskCollection. Maintains sorting order while adding.
    /// </summary>
    /// <param name="task">Task to add to the TaskCollection</param>
    public void Add(Task task)
    {
      if (_tasks.Contains(task))
        throw new ArgumentException("Task is already in task list!");
      else
      {
        int index = _tasks.BinarySearch(task, _comparer);
        if (index < 0)
          _tasks.Insert(~index, task);
        else
          _tasks.Insert(index, task);
      }
    }

    /// <summary>
    /// Removes a task from the TaskCollection
    /// </summary>
    /// <param name="task">Task to remove from the TaskCollection</param>
    public void Remove(Task task)
    {
      if (_tasks.Contains(task))
        _tasks.Remove(task);
    }

    /// <summary>
    /// Replaces a task in the TaskCollection
    /// </summary>
    /// <param name="taskID">ID of the task to replace</param>
    /// <param name="task">new task to replace the old one with</param>
    public void Replace(int taskID, Task task)
    {
      Task oldTask = null;
      foreach (Task t in _tasks)
      {
        if (t.ID == taskID)
        {
          oldTask = t;
          break;
        }
      }
      if (oldTask != null)
        Remove(oldTask);
      Add(task);
    }

    /// <summary>
    /// Explicitly sorts the TaskCollection.
    /// </summary>
    public void Sort()
    {
      _tasks.Sort(_comparer);
    }

    /// <summary>
    /// Creates a clone of the TaskCollection.
    /// </summary>
    /// <returns>a list of tasks currently in the TaskCollection</returns>
    public IList<Task> Clone()
    {
      List<Task> tasks = new List<Task>();
      foreach (Task t in _tasks)
        tasks.Add(t.Clone() as Task);
      return tasks;
    }
    #endregion

    #region Properties
    /// <summary>
    /// Property returning a list of tasks currently in the TaskCollection.
    /// </summary>
    public IList<Task> Tasks
    {
      get { return new List<Task>(_tasks); }
    }

    #endregion
  }

  /// <summary>
  /// Compares two <see cref="Task"/>s with each other.
  /// </summary>
  public class TaskComparer : IComparer<Task>
  {
    #region Public methods
    /// <summary>
    /// Compares two <see cref="Task"/>s with each other. See <see cref="IComparer<T>"/> for more details.
    /// </summary>
    /// <param name="x">first Task</param>
    /// <param name="y">second Task</param>
    /// <returns>comparisation result</returns>
    public int Compare(Task x, Task y)
    {
      if (x == null && y == null)
      {
        return 0;
      }
      else if (x == null)
      {
        return -1;
      }
      else if (y == null)
      {
        return 1;
      }
      else
      {
        if (x.NextRun < y.NextRun)
        {
          return -1;
        }
        else if (x.NextRun > y.NextRun)
        {
          return 1;
        }
        else
        {
          return 0;
        }
      }
    }
    #endregion
  }
}
