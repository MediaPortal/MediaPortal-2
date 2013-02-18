#region Copyright (C) 2007-2013 Team MediaPortal

/*
    Copyright (C) 2007-2013 Team MediaPortal
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
using MediaPortal.Common.Logging;
using MediaPortal.Common.Settings;
using MediaPortal.Common.TaskScheduler;
using MediaPortal.Common.Threading;

namespace MediaPortal.Common.Services.TaskScheduler
{
  /// <summary>
  /// Default implementation of the TaskScheduler. See the <see cref="ITaskScheduler"/> description for details.
  /// </summary>
  public class TaskScheduler : ITaskScheduler
  {
    #region Private fields

    /// <summary>
    /// The persistent settings used by the task scheduler.
    /// </summary>
    protected readonly TaskSchedulerSettings _settings;

    /// <summary>
    /// Interval-based work object to periodically check for due tasks.
    /// </summary>
    protected IntervalWork _work = null;

    /// <summary>
    /// Mutex object to serialize access to the registered tasks and next task id.
    /// </summary>
    protected readonly object _syncObj = new object();

    #endregion

    #region Ctor & dtor

    public TaskScheduler()
    {
      _settings = ServiceRegistration.Get<ISettingsManager>().Load <TaskSchedulerSettings>();
      SaveChanges(false);
    }

    ~TaskScheduler()
    {
      StopWorker();
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Triggers the registered tasks which are registered to fire at startup.
    /// </summary>
    private void DoStartup()
    {
      DateTime now = DateTime.Now;
      // Only use minute precision
      now = now.AddSeconds(-now.Second);
      lock (_syncObj)
      {
        foreach (Task task in _settings.TaskCollection.Tasks)
        {
          if (task.Occurrence == Occurrence.EveryStartUp)
          {
            if (task.IsExpired(now))
              ExpireTask(task);
            else
              ProcessTask(task);
          }
        }
      }
    }

    /// <summary>
    /// Registed as delegate with the <see cref="IntervalWork"/> item to periodically check for due tasks.
    /// </summary>
    private void DoWork()
    {
      bool needSort = false;
      bool saveChanges = false;
      DateTime now = DateTime.Now;
      // Only use minute precision
      now = now.AddSeconds(-now.Second);
      // Exclusively get lock for task collection access
      lock (_syncObj)
      {
        // Enumerate through all tasks
        foreach (Task task in _settings.TaskCollection.Tasks)
        {
          // Only process non-repeat tasks
          if (task.Occurrence == Occurrence.Once || task.Occurrence == Occurrence.Repeat)
          {
            ServiceRegistration.Get<ILogger>().Debug("TaskScheduler: ProcessTask: {0}", task.ToString());
            // Process task if schedule is due
            if (task.IsDue(now))
            {
              ProcessTask(task);
              needSort = true;
              saveChanges = true;
            }
            else
            {
              // Force process task if schedule is due late and ForceRun is set
              if (task.NextRun < now)
              {
                if (task.ForceRun)
                {
                  ProcessTask(task);
                  needSort = true;
                  saveChanges = true;
                }
              }
            }
          }
          // Expire all type of tasks if they are expired
          if (task.IsExpired(now))
          {
            ExpireTask(task);
            saveChanges = true;
          }
        }
        if (saveChanges)
          SaveChanges(needSort);
      }
    }

    /// <summary>
    /// Handles expired tasks: Sends out a message that a particular task is expired, removes the task from the
    /// registered task collection and sends out a message that the task is deleted. This method gets called
    /// from the DoWork() method.
    /// </summary>
    /// <param name="task">Task to expire</param>
    private void ExpireTask(Task task)
    {
      Task tc = (Task) task.Clone();
      TaskSchedulerMessaging.SendTaskSchedulerMessage(TaskSchedulerMessaging.MessageType.EXPIRED, tc);
      _settings.TaskCollection.Remove(task);
      TaskSchedulerMessaging.SendTaskSchedulerMessage(TaskSchedulerMessaging.MessageType.DELETED, tc);
      SaveChanges(false);
    }

    /// <summary>
    /// Handles processing of due tasks. Sends out a message that a task is due, updates the tasks properties and
    /// removes the task if it was scheduled to run only once. This method gets called from the DoWork() method.
    /// </summary>
    /// <param name="task">Task to process.</param>
    private void ProcessTask(Task task)
    {
      Task tc = (Task) task.Clone();
      TaskSchedulerMessaging.SendTaskSchedulerMessage(TaskSchedulerMessaging.MessageType.DUE, tc);
      task.LastRun = task.NextRun;
      if (task.Occurrence == Occurrence.Once)
      {
        _settings.TaskCollection.Remove(task);
        TaskSchedulerMessaging.SendTaskSchedulerMessage(TaskSchedulerMessaging.MessageType.DELETED, tc);
      }
    }

    /// <summary>
    /// Saves any changes to the task configuration.
    /// </summary>
    /// <param name="needSort">Specifies whether or not the task collection needs to be resorted.</param>
    private void SaveChanges(bool needSort)
    {
      if (needSort)
        _settings.TaskCollection.Sort();
      ServiceRegistration.Get<ISettingsManager>().Save(_settings);
    }

    private void StopWorker()
    {
      if (_work != null)
      {
        _work.Cancel();
        ServiceRegistration.Get<IThreadPool>().RemoveIntervalWork(_work);
      }
      _work = null;
    }

    #endregion

    #region ITaskScheduler implementation

    public void Startup()
    {
      DoStartup();
      _work = new IntervalWork(DoWork, new TimeSpan(0, 0, 20));
      ServiceRegistration.Get<IThreadPool>().AddIntervalWork(_work, false);
    }

    public void Shutdown()
    {
      StopWorker();
      ServiceRegistration.Get<ISettingsManager>().Save(_settings);
    }

    public Guid AddTask(Task newTask)
    {
      lock (_syncObj)
      {
        newTask.ID = Guid.NewGuid();
        _settings.TaskCollection.Add(newTask);
        SaveChanges(false);
      }
      return newTask.ID;
    }

    public void UpdateTask(Guid taskId, Task updatedTask)
    {
      lock (_syncObj)
      {
        updatedTask.ID = taskId;
        _settings.TaskCollection.Replace(taskId, updatedTask);
        SaveChanges(false);
        TaskSchedulerMessaging.SendTaskSchedulerMessage(TaskSchedulerMessaging.MessageType.CHANGED, updatedTask);
      }
    }

    public void RemoveTask(Guid taskId)
    {
      lock (_syncObj)
      {
        Task task = null;
        foreach (Task t in _settings.TaskCollection.Tasks)
        {
          if (t.ID == taskId)
            task = t;
        }
        if (task == null)
          return;
        _settings.TaskCollection.Remove(task);
        SaveChanges(false);
        TaskSchedulerMessaging.SendTaskSchedulerMessage(TaskSchedulerMessaging.MessageType.DELETED, task);
      }
    }

    public Task GetTask(Guid taskId)
    {
      lock (_syncObj)
        return _settings.TaskCollection.GetTask(taskId);
    }

    public ICollection<Task> GetTasks(string ownerId)
    {
      lock (_syncObj)
        return _settings.TaskCollection.GetTasks(ownerId);
    }

    #endregion
  }
}
