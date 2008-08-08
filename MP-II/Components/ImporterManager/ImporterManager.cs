#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
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
using System.Timers;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Core;
using MediaPortal.Interfaces.Core.PluginManager;
using MediaPortal.Core.Logging;
using MediaPortal.Core.Settings;
using MediaPortal.Core.Messaging;

using MediaPortal.Media.Importers;
using MediaPortal.Media.MediaManager;

namespace Components.Services.Importers
{
  public class ImporterManager : IImporterManager, IPlugin
  {
    class ImporterContext
    {
      public WatchedFolder Folder;
      public bool Refresh;
      public ImporterContext(WatchedFolder folder)
      {
        Folder = folder;
        Refresh = false;
      }
      public ImporterContext(WatchedFolder folder, bool refresh)
      {
        Folder = folder;
        Refresh = refresh;
      }
    }
    #region variables
    List<ImporterBuilder> _importers;
    List<WatchedFolder> _folders;
    System.Timers.Timer _timer;
    bool _reentrant;
    ImporterManagerSettings _settings;
    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="ImporterManager"/> class.
    /// </summary>
    public ImporterManager()
    {
      _folders = new List<WatchedFolder>();

      //setup a timer 
      _timer = new System.Timers.Timer(500);
      _timer.Elapsed += new ElapsedEventHandler(_timer_Elapsed);
      _timer.Enabled = true;

      //load shares from our settings...
      ServiceScope.Get<ILogger>().Info("importer: loading settings");
      _settings = new ImporterManagerSettings();
      ServiceScope.Get<ISettingsManager>().Load(_settings);
      foreach (Share share in _settings.Shares)
      {
        WatchedFolder newWatch = new WatchedFolder(share.Folder);
        _folders.Add(newWatch);
        ServiceScope.Get<ILogger>().Info("importer: add share {0}", share.Folder);
      }
    }

    #region IPlugin Members
    public void Initialise()
    {
      // Get all Importer plugins
      _importers = ServiceScope.Get<IPluginManager>().GetAllPluginItems<ImporterBuilder>("/Media/Importers");
    }

    public void Dispose()
    {
    }
    #endregion

    #region IImporterManager Members

    /// <summary>
    /// Registers the specified importer.
    /// </summary>
    /// <param name="importer">The importer.</param>
    public void Register(IImporter importer)
    {
      // Use plugin space
      //if (!_importers.Contains(importer))
      //{
      //  _importers.Add(importer);
      //}
    }

    /// <summary>
    /// Unregisters the importer
    /// </summary>
    /// <param name="importer">The importer.</param>
    public void UnRegister(IImporter importer)
    {
      //if (_importers.Contains(importer))
      //{
      //  _importers.Remove(importer);
      //}
    }

    /// <summary>
    /// Gets the registered importers.
    /// </summary>
    /// <value>The registered importers.</value>
    public List<IImporter> Importers
    {
      get
      {
        List<IImporter> importers = new List<IImporter>();
        foreach (ImporterBuilder importerBuilder in _importers)
        {
          importers.Add(importerBuilder.Importer);
        }
        return importers;
      }
    }

    /// <summary>
    /// Gets the importer for the specific name
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns></returns>
    public IImporter GetImporterByName(string name)
    {
      foreach (ImporterBuilder importerBuilder in _importers)
      {
        if (String.Compare(importerBuilder.Name, name, true) == 0)
          return importerBuilder.Importer;
      }
      return null;
    }

    /// <summary>
    /// Returns a list of importers supporting the extension
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns></returns>
    public List<IImporter> GetImporterByExtension(string extension)
    {
      List<IImporter> returnList = new List<IImporter>();
      foreach (ImporterBuilder importerBuilder in _importers)
      {
        if (importerBuilder.Extensions.Contains(extension.ToLower()))
          returnList.Add(importerBuilder.Importer);
      }
      return returnList;
    }

    /// <summary>
    /// Adds a new share which should be imported & watched.
    /// </summary>
    /// <param name="folder">The folder.</param>
    public void AddShare(string folder)
    {
      //sanity checks
      if (folder == null) return;
      if (folder.Length == 0) return;

      string[] drives = Directory.GetLogicalDrives();
      bool isDrive = false;
      for (int i = 0; i < drives.Length; ++i)
      {
        if (String.Compare(folder, drives[i], true) == 0)
        {
          isDrive = true;
          break;
        }
      }
      if (!Directory.Exists(folder) && !isDrive)
      {
        //we cannot import a folder which does not exist
        return;
      }

      //check if folder is already monitored.
      foreach (WatchedFolder watchedFolder in _folders)
      {
        if (String.Compare(folder, watchedFolder.Folder, true) == 0)
        {
          //we are already watching this folder..
          return;
        }
      }

      ServiceScope.Get<ILogger>().Info("importer: add new share {0}", folder);
      WatchedFolder newWatch = new WatchedFolder(folder);
      _folders.Add(newWatch);

      //add new shared-folder to our settings..
      Share share = new Share();
      share.Folder = folder;
      share.LastImport = new DateTime(1500, 1, 1);
      _settings.Shares.Add(share);
      ServiceScope.Get<ISettingsManager>().Save(_settings);

      ForceImport(folder, false);
      IMessageQueue queue = ServiceScope.Get<IMessageBroker>().GetOrCreate("importers");
      QueueMessage msg = new QueueMessage();
      msg.MessageData["action"] = "shareadded";
      msg.MessageData["folder"] = folder;
      queue.Send(msg);
    }

    /// <summary>
    /// Removes a share
    /// </summary>
    /// <param name="folder">The folder.</param>
    public void RemoveShare(string folder)
    {
      //sanity checks
      if (folder == null) return;
      if (folder.Length == 0) return;
      //check if folder is already monitored.
      for (int i = 0; i < _folders.Count; ++i)
      {
        if (String.Compare(folder, _folders[i].Folder, true) == 0)
        {
          ServiceScope.Get<ILogger>().Info("importer: remove share {0}", folder);
          //we are already watching this folder..
          _folders[i].Dispose();
          _folders.RemoveAt(i);
        }
      }
      //save settings..
      for (int i = 0; i < _settings.Shares.Count; ++i)
      {
        if (_settings.Shares[i].Folder == folder)
        {
          _settings.Shares.RemoveAt(i);

          ServiceScope.Get<ISettingsManager>().Save(_settings);
          IMessageQueue queue = ServiceScope.Get<IMessageBroker>().GetOrCreate("importers");
          QueueMessage msg = new QueueMessage();
          msg.MessageData["action"] = "shareremoved";
          msg.MessageData["folder"] = folder;
          queue.Send(msg);
          break;
        }
      }
      return;



    }

    /// <summary>
    /// Returns a list of all share being watched.
    /// </summary>
    /// <value>List containing all shares.</value>
    public List<string> Shares
    {
      get
      {
        List<string> shares = new List<string>();
        foreach (WatchedFolder watch in _folders)
        {
          shares.Add(watch.Folder);
        }
        return shares;
      }
    }

    /// <summary>
    /// Forces a complete import to be done on the folder
    /// </summary>
    /// <param name="folder">The folder.</param>
    public void ForceImport(string folder, bool refresh)
    {
      //check if folder is already monitored.
      for (int i = 0; i < _folders.Count; ++i)
      {
        WatchedFolder watchedFolder = _folders[i];
        if (String.Compare(folder, watchedFolder.Folder, true) == 0)
        {
          ServiceScope.Get<ILogger>().Info("importer: force import from {0}", folder);
          ImporterContext context = new ImporterContext(watchedFolder, refresh);
          //do a complete fresh import of the new folder
          Thread t = new Thread(new ParameterizedThreadStart(DoImportFolder));
          t.IsBackground = true;
          t.Priority = ThreadPriority.BelowNormal;
          t.Start(context);
          return;
        }
      }
    }
    #endregion

    /// <summary>
    /// Handles the Elapsed event of the _timer control.
    /// The timer will check if any of the watched folders detected changes
    /// and ifso process these changes..
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="System.Timers.ElapsedEventArgs"/> instance containing the event data.</param>
    void _timer_Elapsed(object sender, ElapsedEventArgs e)
    {
      //check for re-entrancy. 
      if (_reentrant) return;
      try
      {
        _reentrant = true;
        //check all watched-folders
        foreach (WatchedFolder watchedFolder in _folders)
        {
          //did it detect any changes to the file system
          List<FileChangeEvent> changes = watchedFolder.Changes;
          if (changes != null)
          {
            //yep then we process those
            ServiceScope.Get<ILogger>().Info("importer: detected changes in {0}", watchedFolder.Folder);
            DoProcessChanges(changes);
          }
        }
      }
      catch (Exception ex)
      {
        ServiceScope.Get<ILogger>().Info("importer:error _timer_Elapsed");
        ServiceScope.Get<ILogger>().Error(ex);
      }
      finally
      {
        _reentrant = false;
      }
    }

    /// <summary>
    /// Processes any changes to the filesystem.
    /// </summary>
    /// <param name="changes">List of detected changes.</param>
    void DoProcessChanges(List<FileChangeEvent> changes)
    {
      foreach (FileChangeEvent change in changes)
      {
        foreach (IImporter importer in _importers)
        {
          if (change.Type == FileChangeEvent.FileChangeType.Created)
          {
            importer.FileCreated(change.FileName);
          }
          if (change.Type == FileChangeEvent.FileChangeType.Deleted)
          {
            importer.FileDeleted(change.FileName);
          }
          if (change.Type == FileChangeEvent.FileChangeType.Changed)
          {
            importer.FileChanged(change.FileName);
          }
          if (change.Type == FileChangeEvent.FileChangeType.Renamed)
          {
            importer.FileRenamed(change.FileName, change.OldFileName);
          }
          if (change.Type == FileChangeEvent.FileChangeType.DirectoryDeleted)
          {
            importer.DirectoryDeleted(change.FileName);
          }
        }
      }
    }

    /// <summary>
    /// Imports a complete folder & any subfolders.
    /// </summary>
    /// <param name="obj">The obj.</param>
    void DoImportFolder(object obj)
    {
      // this is not recommended - the better solution would be to use mutexes for parallel imports
      lock (this)
      {
        try
        {
          ImporterContext context = (ImporterContext)obj;
          ServiceScope.Get<ILogger>().Info("importer:import {0}", context.Folder.Folder);
          for (int i = 0; i < _settings.Shares.Count; ++i)
          {
            Share share = _settings.Shares[i];
            if (share.Folder == context.Folder.Folder)
            {
              DateTime dt = share.LastImport;
              if (context.Refresh)
                dt = DateTime.MinValue;
              foreach (ImporterBuilder importer in _importers)
              {
                importer.ImportFolder(context.Folder.Folder, dt);
              }
              share.LastImport = DateTime.Now;
              ServiceScope.Get<ISettingsManager>().Save(_settings);
            }
          }
        }
        catch (Exception ex)
        {
          ServiceScope.Get<ILogger>().Info("importers:import failed");
          ServiceScope.Get<ILogger>().Error(ex);
        }
      }
    }

    /// <summary>
    /// Gets the meta data for a folder
    /// </summary>
    /// <param name="folde">The folder.</param>
    /// <param name="items">The items.</param>
    public void GetMetaDataFor(string folder, ref List<IAbstractMediaItem> items)
    {
      try
      {
        List<string> extensions = new List<string>();
        foreach (IAbstractMediaItem item in items)
        {
          if (item.ContentUri.IsFile)
          {
            string ext = Path.GetExtension(item.ContentUri.LocalPath).ToLower();
            if (!extensions.Contains(ext))
              extensions.Add(ext);
          }
        }

        foreach (ImporterBuilder importer in _importers)
        {
          foreach (string ext in extensions)
          {
            if (importer.Extensions.Contains(ext))
            {
              importer.GetMetaDataFor(folder, ref items);
              break;
            }
          }
        }
      }
      catch (Exception ex)
      {
        ServiceScope.Get<ILogger>().Info("importers:GetMetadataFor:{0} failed", folder);
        ServiceScope.Get<ILogger>().Error(ex);
      }
    }
  }
}
