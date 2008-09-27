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
using MediaPortal.Core;
using MediaPortal.Core.PluginManager;
using MediaPortal.Core.Logging;
using MediaPortal.Core.Services.PluginManager;
using MediaPortal.Core.Settings;
using MediaPortal.Core.Messaging;

using MediaPortal.Media.Importers;
using MediaPortal.Media.MediaManager;
using MediaPortal.Utilities.FileSystem;

namespace Components.Services.ImporterManager
{
  public class ImporterManager : IImporterManager, IPluginStateTracker
  {
    public const string IMPORTERSQUEUE_NAME = "Importers";

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

    #region Variables

    ICollection<LazyImporterWrapper> _importers;
    protected readonly IDictionary<string, WatchedFolder> _folders =
        new Dictionary<string, WatchedFolder>();
    System.Timers.Timer _timer;
    bool _isProcessing;
    ImporterManagerSettings _settings;

    #endregion

    public ImporterManager()
    {
      // Setup the timer to periodically check our watch folders
      _timer = new System.Timers.Timer(500);
      _timer.Elapsed += _timer_Elapsed;
      _timer.Enabled = true;

      // Load the shares from our settings...
      ServiceScope.Get<ILogger>().Info("ImporterManager: Loading settings");
      _settings = new ImporterManagerSettings();
      ServiceScope.Get<ISettingsManager>().Load(_settings);
      foreach (Share share in _settings.Shares)
      {
        WatchedFolder newWatch = new WatchedFolder(share.Folder);
        _folders.Add(newWatch.Folder, newWatch);
        ServiceScope.Get<ILogger>().Info("ImporterManager: Adding share '{0}'", newWatch.Folder);
      }
    }

    #region IPluginStateTracker implementation

    public void Activated()
    {
      // Get all Importer plugins
      _importers = ServiceScope.Get<IPluginManager>().RequestAllPluginItems<LazyImporterWrapper>(
          "/Media/Importers", new FixedItemStateTracker()); // FIXME: make importers able to be disabled again
      // TODO: Add a change listener to this plugin item path
    }

    public bool RequestEnd()
    {
      return false; // FIXME: The importer manager plugin should be able to be disabled
    }

    public void Stop() { }

    public void Continue() { }

    public void Shutdown() { }

    #endregion

    #region IImporterManager Members

    public ICollection<IImporter> Importers
    {
      get
      {
        ICollection<IImporter> result = new List<IImporter>();
        foreach (LazyImporterWrapper importerBuilder in _importers)
          result.Add(importerBuilder.Importer);
        return result;
      }
    }

    public IImporter GetImporterByName(string name)
    {
      foreach (LazyImporterWrapper importerBuilder in _importers)
        if (String.Compare(importerBuilder.Name, name, true) == 0)
          return importerBuilder.Importer;
      return null;
    }

    public ICollection<IImporter> GetImporterByExtension(string extension)
    {
      extension = extension.ToLower();
      IList<IImporter> returnList = new List<IImporter>();
      foreach (LazyImporterWrapper importerBuilder in _importers)
        if (importerBuilder.Extensions.Contains(extension))
          returnList.Add(importerBuilder.Importer);
      return returnList;
    }

    public void AddShare(string folderPath)
    {
      // Sanity checks
      if (folderPath == null) return;

      // Check if the folder is already monitored.
      foreach (WatchedFolder watchedFolder in _folders.Values)
        if (FileUtils.IsContainedIn(folderPath, watchedFolder.Folder))
          // We are already watching this folder...
          return;

      ServiceScope.Get<ILogger>().Info("ImporterManager: Adding new share '{0}'", folderPath);
      WatchedFolder newWatch = new WatchedFolder(folderPath);
      _folders.Add(folderPath, newWatch);

      //add new shared-folder to our settings..
      _settings.AddShare(folderPath);
      ServiceScope.Get<ISettingsManager>().Save(_settings);

      ForceImport(folderPath, false);
      SendImporterQueueMessage("shareadded", folderPath);
    }

    public void RemoveShare(string folderPath)
    {
      // Sanity checks
      if (folderPath == null) return;

      // Check if folder is monitored
      if (_folders.ContainsKey(folderPath))
      {
        WatchedFolder toRemove = _folders[folderPath];
        toRemove.Dispose();
        _folders.Remove(folderPath);

        SendImporterQueueMessage("shareremoved", folderPath);
      }
      // Save settings...
      _settings.RemoveShare(folderPath);
      ServiceScope.Get<ISettingsManager>().Save(_settings);
      return;
    }

    public bool ContainsShare(string folderPath)
    {
      return _folders.ContainsKey(folderPath);
    }

    public ICollection<string> Shares
    {
      get { return _folders.Keys; }
    }

    public void ForceImport(string folderPath, bool refresh)
    {
      if (!_folders.ContainsKey(folderPath))
        throw new ArgumentException(string.Format("Share '{0}' is not watched by the importer manager", folderPath));
      WatchedFolder watchedFolder = _folders[folderPath];
      ServiceScope.Get<ILogger>().Info("ImporterManager: Force import of '{0}'", folderPath);
      ImporterContext context = new ImporterContext(watchedFolder, refresh);
      Thread t = new Thread(DoImportFolder);
      t.IsBackground = true;
      t.Priority = ThreadPriority.BelowNormal;
      t.Start(context);
    }

    #endregion

    protected static void SendImporterQueueMessage(string action, string folder)
    {
      IMessageQueue queue = ServiceScope.Get<IMessageBroker>().GetOrCreate(IMPORTERSQUEUE_NAME);
      QueueMessage msg = new QueueMessage();
      msg.MessageData["action"] = action;
      msg.MessageData["folder"] = folder;
      queue.Send(msg);
    }

    /// <summary>
    /// Handles the Elapsed event of the _timer control.
    /// The timer will check if any of the watched folders detected changes
    /// and if so process these changes.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="System.Timers.ElapsedEventArgs"/> instance containing the
    /// event data.</param>
    void _timer_Elapsed(object sender, ElapsedEventArgs e)
    {
      if (_isProcessing) return;
      _isProcessing = true;
      try
      {
        // Check all watched-folders
        foreach (WatchedFolder watchedFolder in _folders.Values)
        {
          // Did it detect any changes to the file system?
          IList<FileChangeEvent> changes = watchedFolder.Changes;
          if (changes != null)
          {
            // Yep, then we process those
            ServiceScope.Get<ILogger>().Info("ImporterManager: Detected changes in folder '{0}'", watchedFolder.Folder);
            DoProcessChanges(changes);
          }
        }
      }
      catch (Exception ex)
      {
        ServiceScope.Get<ILogger>().Error("ImporterManager: Error processing changed folders", ex);
      }
      finally
      {
        _isProcessing = false;
      }
    }

    /// <summary>
    /// Processes any changes to the filesystem.
    /// </summary>
    /// <param name="changes">Enumeration of detected changes.</param>
    void DoProcessChanges(IEnumerable<FileChangeEvent> changes)
    {
      foreach (FileChangeEvent change in changes)
      {
        foreach (IImporter importer in _importers)
        {
          if (change.Type == FileChangeEvent.FileChangeType.Created)
            importer.FileCreated(change.FullPath);
          if (change.Type == FileChangeEvent.FileChangeType.Deleted)
            importer.FileDeleted(change.FullPath);
          if (change.Type == FileChangeEvent.FileChangeType.Changed)
            importer.FileChanged(change.FullPath);
          if (change.Type == FileChangeEvent.FileChangeType.Renamed)
            importer.FileRenamed(change.FullPath, change.OldFullPath);
          if (change.Type == FileChangeEvent.FileChangeType.DirectoryDeleted)
            importer.DirectoryDeleted(change.FullPath);
        }
      }
    }

    /// <summary>
    /// Imports a complete folder & any subfolders.
    /// </summary>
    /// <param name="importerContext">The current importer context. This object can be casted to
    /// <c>ImporterContext</c>.</param>
    void DoImportFolder(object importerContext)
    {
      // This is not good - A better solution would be to use mutexes for parallel imports
      lock (this)
      {
        try
        {
          ImporterContext context = (ImporterContext) importerContext;
          ServiceScope.Get<ILogger>().Info("ImporterManager: Import '{0}'", context.Folder.Folder);
          for (int i = 0; i < _settings.Shares.Count; ++i)
          {
            Share share = _settings.Shares[i];
            if (FileUtils.PathEquals(share.Folder, context.Folder.Folder))
            {
              DateTime dt = share.LastImport;
              if (context.Refresh)
                dt = DateTime.MinValue;
              foreach (LazyImporterWrapper importer in _importers)
                importer.ImportFolder(context.Folder.Folder, dt);
              share.LastImport = DateTime.Now;
              ServiceScope.Get<ISettingsManager>().Save(_settings);
            }
          }
        }
        catch (Exception ex)
        {
          ServiceScope.Get<ILogger>().Error("ImporterManager: Import failed", ex);
        }
      }
    }

    public void GetMetaDataFor(string folder, ref IList<IAbstractMediaItem> items)
    {
      try
      {
        ICollection<string> extensions = new List<string>();
        foreach (IAbstractMediaItem item in items)
          if (item.ContentUri.IsFile)
          {
            string ext = Path.GetExtension(item.ContentUri.LocalPath).ToLower();
            if (!extensions.Contains(ext))
              extensions.Add(ext);
          }

        foreach (LazyImporterWrapper importer in _importers)
          foreach (string ext in extensions)
            if (importer.Extensions.Contains(ext))
            {
              importer.GetMetaDataFor(folder, ref items);
              break;
            }
      }
      catch (Exception ex)
      {
        ServiceScope.Get<ILogger>().Error("ImporterManager: Error getting metadata for media items", ex);
      }
    }
  }
}
