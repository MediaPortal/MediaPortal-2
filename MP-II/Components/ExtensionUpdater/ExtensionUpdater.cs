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

using System;
using System.ComponentModel;
using System.Net;
using System.IO;
using Components.ExtensionUpdater.ExtensionManager;
using MediaPortal.Core;
using MediaPortal.Core.PluginManager;
using MediaPortal.Core.Logging;
using MediaPortal.Core.Settings;
using MediaPortal.Core.Messaging;
using MediaPortal.Core.TaskScheduler;
using MediaPortal.Core.ExtensionManager;
using MediaPortal.Core.PathManager;
using MediaPortal.Core.Threading;

namespace Components.ExtensionUpdater
{
  public class ExtensionUpdater : IPluginStateTracker
  {
    #region variables

    private ExtensionUpdaterSettings _settings ;
    ExtensionInstaller _installer;
    WebClient _client;
    WebClient _updaterClient;
    string _tempfile;
    string _finalfile;
    string _listFile;
    private IMessageQueue _queue;

    #endregion

    public ExtensionUpdater()
    {
      //MPInstaller - for testing only 
      _installer = new ExtensionInstaller();
      ServiceScope.Add<IExtensionInstaller>(_installer);
      _installer.LoadQueue();
      _installer.ExecuteQueue(false);

      _installer = (ExtensionInstaller)ServiceScope.Get<IExtensionInstaller>();
      _listFile = String.Format(@"{0}\Mpilist.xml", ServiceScope.Get<IPathManager>().GetPath("<MPINSTALLER>"));
      _settings = new ExtensionUpdaterSettings();
      _queue = ServiceScope.Get<IMessageBroker>().GetOrCreate("extensionupdater");
      _client = new WebClient();
      _updaterClient = new WebClient();
      _client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DownloadProgressCallback);
      _client.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadEnd);
      _updaterClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(UpdaterDownloadProgressCallback);
      _updaterClient.DownloadFileCompleted += new AsyncCompletedEventHandler(UpdaterDownloadEnd);
    }

    #region IPluginStateTracker Members

    public void Activated()
    {
      Task task;
      Task updatertask;
      ServiceScope.Get<ISettingsManager>().Load(_settings);
      task = ServiceScope.Get<ITaskScheduler>().GetTask(_settings.TaskId);
      if (task == null)
      {
        task = new Task("ExtensionUpdater",new TimeSpan(0, 0, 40), DateTime.MaxValue, true, false);
        ServiceScope.Get<ITaskScheduler>().AddTask(task);
      }

      updatertask = ServiceScope.Get<ITaskScheduler>().GetTask(_settings.UpdaterTaskId);
      if (updatertask == null)
      {
        updatertask = new Task("ExtensionUpdater", new TimeSpan(0, 0, 40), DateTime.MaxValue, true, false);
        ServiceScope.Get<ITaskScheduler>().AddTask(updatertask);
      }

      ServiceScope.Get<IMessageBroker>().GetOrCreate("taskscheduler").OnMessageReceive +=
          new MessageReceivedHandler(OnMessageReceive);
      _settings.TaskId = task.ID;
      _settings.UpdaterTaskId = updatertask.ID;
      ServiceScope.Get<ISettingsManager>().Save(_settings);
      ServiceScope.Get<ILogger>().Info("Extension Updater Started");
      _installer.Settings.UpdateUrl = _settings.UpdateUrl;
      if (!File.Exists(_listFile))
      {
        StartUpdate();
      }

    }

    public bool RequestEnd()
    {
      return false; // FIXME: The extension updater should be able to be disabled
    }

    public void Stop() { }

    public void Continue() { }

    public void Shutdown() { }

    #endregion

    void OnMessageReceive(QueueMessage message)
    {
      TaskMessage msg = message.MessageData["taskmessage"] as TaskMessage;
      if (msg.Task.Owner.Equals("ExtensionUpdater"))
      {
        switch (msg.Type)
        {
          case TaskMessageType.DUE:
            if (msg.Task.ID == _settings.TaskId)
            {
              // test if download is in progress, if not try to download the next needed extension
              if (!_client.IsBusy)
              {
                GetNextPendingExtension();
              }
            }
            if (msg.Task.ID == _settings.UpdaterTaskId)
            {
              // test if download is in progress, if not try to download the next needed extension
              if (!_updaterClient.IsBusy)
              {
                StartUpdate();
              }
            }
            break;
          case TaskMessageType.CHANGED:
            // act on changes to any of your scheduled tasks
            break;
          case TaskMessageType.DELETED:
            // act on deletion of any of your scheduled tasks
            break;
          case TaskMessageType.EXPIRED:
            // act on expiry of any of your scheduled tasks
            break;
        }
      }
    }

    private void DownloadProgressCallback(object sender, DownloadProgressChangedEventArgs e)
    {
    }

    private void DownloadEnd(object sender, AsyncCompletedEventArgs e)
    {
      if (e.Error == null)
      {

        if (File.Exists(_tempfile))
        {
          try
          {
            File.Copy(_tempfile, _finalfile);
            File.Delete(_tempfile);
          }
          catch (Exception)
          {
          }
        }
      }
    }

    private void UpdaterDownloadProgressCallback(object sender, DownloadProgressChangedEventArgs e)
    {
    }

    /// <summary>
    /// Updaters the download end.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="System.ComponentModel.AsyncCompletedEventArgs"/> instance containing the event data.</param>
    private void UpdaterDownloadEnd(object sender, AsyncCompletedEventArgs e)
    {
      if (e.Error == null)
      {

        if (File.Exists(_listFile))
        {
          try
          {
            _installer.Enumerator.UpdateList(_listFile);
            _installer.Enumerator.Save();
            _installer.UpdateAll();
            ServiceScope.Get<ILogger>().Info("Updating all extensions");
            SendMessage("listupdated");
          }
          catch (Exception)
          {
          }
        }
      }
    }
    /// <summary>
    /// Gets the next pending extension from installation queue.
    /// </summary>
    private void GetNextPendingExtension()
    {
      ExtensionQueue Queue = (ExtensionQueue)_installer.GetQueue();
      foreach (ExtensionQueueObject item in Queue.Items)
      {
        if (!File.Exists(item.FileName))
        {
          ExtensionEnumeratorObject obj = _installer.Enumerator.GetItem(item.PackageId);
          if (obj != null && !string.IsNullOrEmpty(obj.DownloadUrl))
          {
            _finalfile = item.FileName;
            _tempfile = Path.GetTempFileName();
            DownloadFile(_client, obj.DownloadUrl, _tempfile);
            ServiceScope.Get<ILogger>().Info("Download started for {0}", obj.Name);
          }
        }
      }
    }

    private void StartUpdate()
    {
      string url = _settings.UpdateUrl;
      ServiceScope.Get<ILogger>().Info("Get updates from : {0}", url);
      DownloadFile(_updaterClient,url, _listFile);
    }

    /// <summary>
    /// Downloads the file.
    /// </summary>
    /// <param name="_client">The web client</param>
    /// <param name="source">The source file</param>
    /// <param name="dest">The destination file</param>
    private void DownloadFile(WebClient _client, string source, string dest)
    {
      _client.Credentials = new NetworkCredential("test", "testmaid5");
      _client.DownloadFileAsync(new Uri(source), dest);
    }

    private void SendMessage(string action)
    {
      // create message
      QueueMessage msg = new QueueMessage();
      msg.MessageData["message"] = action;
      // asynchronously send message through queue
      ServiceScope.Get<IThreadPool>().Add(new DoWorkHandler(delegate { _queue.Send(msg); }));
    }
  }
}
