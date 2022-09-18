#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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

using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PluginManager;
using MediaPortal.Plugins.SlimTv.SlimTvResources.FanartProvider;
using System;
using System.Threading.Tasks;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.Settings;
using MediaPortal.Common.TaskScheduler;
using MediaPortal.Plugins.SlimTv.Interfaces.Settings;
using Task = System.Threading.Tasks.Task;

namespace MediaPortal.Plugins.SlimTv.SlimTvResources
{
  public class SlimTvFanartPlugin : IPluginStateTracker
  {
    private AsynchronousMessageQueue _messageQueue;
    private SlimTvLogoSettings _settings;
    private ISettingsManager _settingsManager;

    public void Activated(PluginRuntime pluginRuntime)
    {
      var meta = pluginRuntime.Metadata;
      Logger.Info(string.Format("{0} v{1} [{2}] by {3}", meta.Name, meta.PluginVersion, meta.Description, meta.Author));

      _settingsManager = ServiceRegistration.Get<ISettingsManager>();
      _settings = _settingsManager.Load<SlimTvLogoSettings>();

      if (AssureCreatedTask())
      {
        _messageQueue = new AsynchronousMessageQueue(this, new[] { TaskSchedulerMessaging.CHANNEL });
        _messageQueue.MessageReceived += OnMessageReceived;
        _messageQueue.Start();
      }
    }

    private void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == TaskSchedulerMessaging.CHANNEL)
      {
        var messageType = (TaskSchedulerMessaging.MessageType)message.MessageType;
        switch (messageType)
        {
          case TaskSchedulerMessaging.MessageType.DUE:
            var dueTask = (Common.TaskScheduler.Task)message.MessageData[TaskSchedulerMessaging.TASK];
            if (dueTask.ID == _settings.UpdateJobId)
            {
              StartUpdateAsync().Wait();
            }
            break;
        }
      }
    }

    /// <summary>
    /// Depending on the settings <see cref="SlimTvLogoSettings.EnableAutoUpdate"/>, this method checks for existing Task or creates a new Task if value is <c>true</c> or does nothing if value is <c>false</c>.
    /// </summary>
    /// <returns><c>true</c> if Auto Update is enabled and task is active</returns>
    private bool AssureCreatedTask()
    {
      var scheduler = ServiceRegistration.Get<ITaskScheduler>();
      if (_settings.UpdateJobId != Guid.Empty && scheduler.GetTask(_settings.UpdateJobId) != null)
      {
        if (!_settings.EnableAutoUpdate)
        {
          scheduler.RemoveTask(_settings.UpdateJobId);
          _settings.UpdateJobId = Guid.Empty;
          _settingsManager.Save(_settings);
          return false;
        }
        return true;
      }

      if (!_settings.EnableAutoUpdate)
        return false;

      Common.TaskScheduler.Task updaterTask = new Common.TaskScheduler.Task("SlimTv Logo Updater", 0, 5, 1, Occurrence.Repeat, DateTime.MaxValue, true, true);
      _settings.UpdateJobId = scheduler.AddTask(updaterTask);
      _settingsManager.Save(_settings);

      StartUpdateDelayed(); // Run once
      return true;
    }

    private static void StartUpdateDelayed(int retryCount = 2)
    {
      Task.Run(async () =>
      {
        await Task.Delay(TimeSpan.FromSeconds(20));
        if (!await StartUpdateAsync() && retryCount > 0)
          StartUpdateDelayed(--retryCount);
      });
    }

    private static async Task<bool> StartUpdateAsync()
    {
      using (var provider = new SlimTvFanartProvider())
        return await provider.UpdateLogosAsync();
    }

    public bool RequestEnd()
    {
      return true;
    }

    public void Stop()
    {
    }

    public void Continue()
    {
    }

    public void Shutdown()
    {
      _messageQueue?.Shutdown();
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
