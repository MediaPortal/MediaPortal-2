#region Copyright (C) 2007 Team MediaPortal

/*
    Copyright (C) 2007 Team MediaPortal
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
using MediaPortal.Core.Importers;
using MediaPortal.Core.Logging;
using MediaPortal.Core.Settings;
using MediaPortal.Core.MediaManager;
using MediaPortal.Core.Messaging;

namespace MediaPortal.Services.Importers
{
  public class ImporterManager : IImporterManager
  {
    #region variables
    List<IImporter> _importers;
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
      _importers = new List<IImporter>();
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


    #region IImporterManager Members

    /// <summary>
    /// Registers the specified importer.
    /// </summary>
    /// <param name="importer">The importer.</param>
    public void Register(IImporter importer)
    {
      if (!_importers.Contains(importer))
      {
        _importers.Add(importer);
      }
    }

    /// <summary>
    /// Unregisters the importer
    /// </summary>
    /// <param name="importer">The importer.</param>
    public void UnRegister(IImporter importer)
    {
      if (_importers.Contains(importer))
      {
        _importers.Remove(importer);
      }
    }

    /// <summary>
    /// Gets the registered importers.
    /// </summary>
    /// <value>The registered importers.</value>
    public List<IImporter> Importers
    {
      get
      {
        return _importers;
      }
    }

    /// <summary>
    /// Gets the importer for the specific name
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns></returns>
    public IImporter GetImporterByName(string name)
    {
      foreach (IImporter importer in _importers)
      {
        if (String.Compare(importer.Name, name, true) == 0)
          return importer;
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
      foreach (IImporter importer in _importers)
      {
        bool add = false;
        foreach (string ex in importer.Extensions)
        {
          if (String.Compare(ex, extension, true) == 0)
            add = true;
        }
        if (add)
        {
          returnList.Add(importer);
        }
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

      ForceImport(folder);
      IQueue queue = ServiceScope.Get<IMessageBroker>().Get("importers");
      MPMessage msg = new MPMessage();
      msg.MetaData["action"] = "shareadded";
      msg.MetaData["folder"] = folder;
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
          IQueue queue = ServiceScope.Get<IMessageBroker>().Get("importers");
          MPMessage msg = new MPMessage();
          msg.MetaData["action"] = "shareremoved";
          msg.MetaData["folder"] = folder;
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
    public void ForceImport(string folder)
    {
      //check if folder is already monitored.
      foreach (WatchedFolder watchedFolder in _folders)
      {
        if (String.Compare(folder, watchedFolder.Folder, true) == 0)
        {
          ServiceScope.Get<ILogger>().Info("importer: force import from {0}", folder);
          //do a complete fresh import of the new folder
          Thread t = new Thread(new ParameterizedThreadStart(DoImportFolder));
          t.IsBackground = true;
          t.Priority = ThreadPriority.BelowNormal;
          t.Start(watchedFolder);
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
        WatchedFolder watchedFolder = (WatchedFolder)obj;
        ServiceScope.Get<ILogger>().Info("importer:import {0}", watchedFolder.Folder);
        for (int i = 0; i < _settings.Shares.Count; ++i)
        {
          Share share = _settings.Shares[i];
          if (share.Folder == watchedFolder.Folder)
          {
            foreach (IImporter importer in _importers)
            {
              importer.ImportFolder(watchedFolder.Folder, share.LastImport);
            }
            share.LastImport = DateTime.Now;
            ServiceScope.Get<ISettingsManager>().Save(_settings);
          }
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

      foreach (IImporter importer in _importers)
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
  }
}
