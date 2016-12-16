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
using System.Linq;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.Runtime;
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

    /// <summary>
    /// Timer to wake up system from standby if a task is due.
    /// </summary>
    protected TaskWaitableTimer _wakeUpTimer;

    protected AsynchronousMessageQueue _messageQueue;

    #endregion

    #region Ctor & dtor

    public TaskScheduler()
    {
      _settings = ServiceRegistration.Get<ISettingsManager>().Load<TaskSchedulerSettings>();
      SaveChanges(false);
      _wakeUpTimer = new TaskWaitableTimer();
      _wakeUpTimer.OnTimerExpired += OnResume;
      _messageQueue = new AsynchronousMessageQueue(this, new[] { SystemMessaging.CHANNEL });
      _messageQueue.MessageReceived += OnMessageReceived;
      // Message queue will be started in Startup()
    }

    ~TaskScheduler()
    {
      StopWorker();
    }

    #endregion

    #region Private methods

    private void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == SystemMessaging.CHANNEL)
      {
        SystemMessaging.MessageType messageType = (SystemMessaging.MessageType) message.MessageType;
        switch (messageType)
        {
          case SystemMessaging.MessageType.SystemStateChanged:
            SystemState newState = (SystemState) message.MessageData[SystemMessaging.NEW_STATE];
            if (newState == SystemState.Resuming)
              DoResume();
            if (newState == SystemState.Suspending)
              DoSuspend();
            break;
        }
      }
    }

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
        foreach (Task task in _settings.TaskCollection.Tasks.Where(task => task.Occurrence == Occurrence.EveryStartUp))
        {
          if (task.IsExpired(now))
            ExpireTask(task);
          else
            ProcessTask(task);
        }
      }
    }

    /// <summary>
    /// Triggers the registered tasks which are registered to fire at resume.
    /// </summary>
    private void DoResume()
    {
      DateTime now = DateTime.Now;
      // Only use minute precision
      now = now.AddSeconds(-now.Second);
      lock (_syncObj)
      {
        foreach (Task task in _settings.TaskCollection.Tasks.Where(task => task.Occurrence == Occurrence.EveryWakeUp))
        {
          if (task.IsExpired(now))
            ExpireTask(task);
          else
            ProcessTask(task);
        }
      }
    }

    /// <summary>
    /// Executed when the system woke up for a task.
    /// </summary>
    private void OnResume()
    {
      ServiceRegistration.Get<ILogger>().Debug("TaskScheduler: Wake up");
    }

    /// <summary>
    /// Get executed when the system is being suspended and takes care for setting a wakeup time for the next due task.
    /// </summary>
    private void DoSuspend()
    {
      DateTime now = DateTime.Now;
      // Only use minute precision
      now = now.AddSeconds(-now.Second);
      lock (_syncObj)
      {
        var wakeupTasks = _settings.TaskCollection.Tasks.Where(task => !task.IsExpired(now) && task.WakeupSystem).ToList();
        DateTime nextTaskRun = wakeupTasks.Any() ? wakeupTasks.Min(t => t.NextRun) : DateTime.MinValue;
        if (nextTaskRun != DateTime.MinValue)
        {
          double secondsToWait = (nextTaskRun - now).TotalSeconds;

          ServiceRegistration.Get<ILogger>().Debug("TaskScheduler: Schedule next wake up: {0} (in {1} minutes)", nextTaskRun, (int) secondsToWait / 60);
          _wakeUpTimer.SecondsToWait = secondsToWait;
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
      ServiceRegistration.Get<ILogger>().Debug("TaskScheduler: ExpireTask: {0}", task.ToString());
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
      ServiceRegistration.Get<ILogger>().Debug("TaskScheduler: ProcessTask: {0}", task.ToString());
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
      _messageQueue.Start();
      DoStartup();
      _work = new IntervalWork(DoWork, new TimeSpan(0, 0, 20));
      ServiceRegistration.Get<IThreadPool>().AddIntervalWork(_work, false);
    }

    public void Shutdown()
    {
      _messageQueue.Shutdown();
      _wakeUpTimer.Close();
      StopWorker();
      ServiceRegistration.Get<ISettingsManager>().Save(_settings);
    }

    public Guid AddTask(Task newTask)
    {
      lock (_syncObj)
      {
        newTask.ID = Guid.NewGuid();
        ServiceRegistration.Get<ILogger>().Debug("TaskScheduler: AddTask: {0}", newTask);
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
        ServiceRegistration.Get<ILogger>().Debug("TaskScheduler: UpdateTask: {0}", updatedTask);
        _settings.TaskCollection.Replace(taskId, updatedTask);
        SaveChanges(false);
        TaskSchedulerMessaging.SendTaskSchedulerMessage(TaskSchedulerMessaging.MessageType.CHANGED, updatedTask);
      }
    }

    public void RemoveTask(Guid taskId)
    {
      lock (_syncObj)
      {
        Task task = GetTask(taskId);
        if (task == null)
          return;
        ServiceRegistration.Get<ILogger>().Debug("TaskScheduler: RemoveTask: {0}", task);
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
