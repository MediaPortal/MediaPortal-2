#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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
using MediaPortal.Core.Settings;
using MediaPortal.Core.Messaging;

using MediaPortal.Media.Importers;
using MediaPortal.Media.MediaManagement;
using MediaPortal.Utilities.FileSystem;

namespace Components.Services.ImporterManager
{
  /// <summary>
  /// TODO
  /// </summary>
  public class ImporterManager : IImporterManager, IPluginStateTracker
  {
    public const string IMPORTERSQUEUE_NAME = "Importers";
    public const string IMPORTERS_PLUGINITEMS_LOCATION = "/Media/Importers";

    /// <summary>
    /// Plugin item state tracker for importer items. Importer removal must be synchronized with
    /// currently running import actions.
    /// </summary>
    class ImporterItemStateTracker : IPluginItemStateTracker
    {
      #region Private fields

      private readonly ImporterManager _manager;

      #endregion

      internal ImporterItemStateTracker(ImporterManager manager)
      {
        _manager = manager;
      }

      public string UsageDescription
      {
        get { return "ImporterManager: Importers"; }
      }

      public bool RequestEnd(PluginItemRegistration itemRegistration)
      {
        _manager.LockImporters();
        return true;
      }

      public void Stop(PluginItemRegistration itemRegistration)
      {
        if (!_manager.ImportersLocked())
          throw new InvalidOperationException(string.Format("ImporterManager: Cannot stop importer '{0}', importer collection is not locked", item.Id));
        _manager.RemoveImporter(item.Id);
        _manager.UnlockImporters();
      }

      public void Continue(PluginItemRegistration itemRegistration)
      {
        _manager.UnlockImporters();
      }
    }

    /// <summary>
    /// Listener which dynamically adds importers to the importer manager's importer collection.
    /// </summary>
    class ImporterItemRegistrationChangeListener : IItemRegistrationChangeListener
    {
      #region Private fields

      private readonly ImporterManager _manager;

      #endregion

      internal ImporterItemRegistrationChangeListener(ImporterManager manager)
      {
        _manager = manager;
      }

      #region IItemRegistrationChangeListener

      public void ItemsWereAdded(string location, ICollection<PluginItemMetadata> items)
      {
        IPluginManager pluginManager = ServiceScope.Get<IPluginManager>();
        foreach (PluginItemMetadata item in items)
        {
          LazyImporterWrapper newImporter = pluginManager.RequestPluginItem<LazyImporterWrapper>(
              item.RegistrationLocation, item.Id, new ImporterItemStateTracker(_manager));
          if (newImporter != null)
            _manager.AddImporter(newImporter);
        }
      }

      public void ItemsWereRemoved(string location, ICollection<PluginItemMetadata> items)
      {
        // We don't care about this remove event - the removal of an importer is notified via
        // the ImporterItemStateTracker.EndRequest method
      }

      #endregion
    }

    class ImportFolderAction
    {
      public readonly string Folder;
      public readonly bool Refresh;
      public ImportFolderAction(string folder, bool refresh)
      {
        Folder = folder;
        Refresh = refresh;
      }
    }

    #region Variables

    internal ICollection<LazyImporterWrapper> _importers; // Collection of all currently available importers
    protected readonly IDictionary<string, WatchedFolder> _folders =
        new Dictionary<string, WatchedFolder>(); // Local dictionary of shares - should be always in sync with the shares in our settings
    protected readonly Queue<object> _asyncImportActions = new Queue<object>(); // Holds all import actions to be done asynchronously
    protected readonly ImporterManagerSettings _settings;
    protected System.Timers.Timer _timer = null; // Timer for periodical checking of outstanding actions
    protected int _importersLocked = 0; // If > 0, the importers are being locked outside the _syncObject lock (necessary for importers plugin 2-phase stop procedure)
    protected object _syncObject = new object(); // Threading synchronization object for the access to all local state variables
    protected Thread _importerThread = null; // The thread executing the asynchronous import actions
    protected bool _blockImporterThread = false; // Flag to stop a running importer thread

    #endregion

    public ImporterManager()
    {
      // Load the shares from our settings...
      ServiceScope.Get<ILogger>().Info("ImporterManager: Loading settings");
      _settings = ServiceScope.Get<ISettingsManager>().Load<ImporterManagerSettings>();
      foreach (Share share in _settings.Shares)
      {
        WatchedFolder newWatch = new WatchedFolder(share.Folder);
        _folders.Add(newWatch.Folder, newWatch);
        ServiceScope.Get<ILogger>().Info("ImporterManager: Adding share '{0}'", newWatch.Folder);
      }
    }

    internal void AddImporter(LazyImporterWrapper importer)
    {
      _importers.Add(importer);
      ForceImport(true);
    }

    internal void RemoveImporter(string importerName)
    {
      LazyImporterWrapper importer = GetImporterWrapperByName(importerName);
      if (importer != null)
      {
        _importers.Remove(importer);
        // TODO Albert78: clean library from the importer's information
      }
    }

    internal LazyImporterWrapper GetImporterWrapperByName(string name)
    {
      foreach (LazyImporterWrapper importerWrapper in _importers)
        if (importerWrapper.Name == name)
          return importerWrapper;
      return null;
    }

    #region IPluginStateTracker implementation

    public void Activated(PluginRuntime pluginRuntime)
    {
      IPluginManager pluginManager = ServiceScope.Get<IPluginManager>();
      // Get all Importer plugins
      lock (_syncObject)
      {
        _importers = pluginManager.RequestAllPluginItems<LazyImporterWrapper>(
            IMPORTERS_PLUGINITEMS_LOCATION, new ImporterItemStateTracker(this));
        pluginManager.AddItemRegistrationChangeListener(
            IMPORTERS_PLUGINITEMS_LOCATION, new ImporterItemRegistrationChangeListener(this));
      }

      // Setup the timer to periodically check our watch folders
      _timer = new System.Timers.Timer(500);
      _timer.Elapsed += _timer_Elapsed;
      _timer.Enabled = true;
    }

    public bool RequestEnd()
    {
      lock (_syncObject)
      {
        // Block importer thread and wait for it to terminate. When it is terminated, we
        // are able to stop. In the meantime, the changes will be collected further on.
        _blockImporterThread = true;
        if (_importerThread != null)
          _importerThread.Join();
      }
      return true;
    }

    public void Stop()
    {
      lock (_syncObject)
        _importers.Clear();
    }

    public void Continue()
    {
      lock (_syncObject)
      {
        _blockImporterThread = false;
        CheckImportThread();
      }
    }

    public void Shutdown() { }

    #endregion

    #region IImporterManager Members

    public void AddShare(string folderPath)
    {
      // Sanity checks
      if (folderPath == null) return;

      lock (_syncObject)
      {
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
      }
      SendImporterQueueMessage("shareadded", folderPath);
    }

    public void RemoveShare(string folderPath)
    {
      // Sanity checks
      if (folderPath == null) return;

      lock (_syncObject)
      {
        // Check if folder is monitored
        if (!_folders.ContainsKey(folderPath))
          return;
        WatchedFolder toRemove = _folders[folderPath];
        toRemove.Dispose();
        _folders.Remove(folderPath);
      }
      // Save settings...
      _settings.RemoveShare(folderPath);
      ServiceScope.Get<ISettingsManager>().Save(_settings);
      SendImporterQueueMessage("shareremoved", folderPath);
    }

    public bool ContainsShare(string folderPath)
    {
      lock (_syncObject)
        return _folders.ContainsKey(folderPath);
    }

    public ICollection<string> Shares
    {
      get
      {
        lock (_syncObject)
          return _folders.Keys;
      }
    }

    public void ForceImport(string folderPath, bool refresh)
    {
      lock (_syncObject)
      {
        if (!_folders.ContainsKey(folderPath))
          throw new ArgumentException(string.Format("Share '{0}' is not watched by the importer manager", folderPath));
        WatchedFolder watchedFolder = _folders[folderPath];
        ServiceScope.Get<ILogger>().Info("ImporterManager: Force import of '{0}'", folderPath);
        ImportFolderAction action = new ImportFolderAction(watchedFolder.Folder, refresh);
        AddAsyncImportAction(action);
      }
    }

    public void ForceImport(bool refresh)
    {
      lock (_syncObject)
      {
        foreach (string folderPath in _folders.Keys)
        {
          ServiceScope.Get<ILogger>().Info("ImporterManager: Force import of '{0}'", folderPath);
          ImportFolderAction action = new ImportFolderAction(folderPath, refresh);
          AddAsyncImportAction(action);
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
        lock (_syncObject)
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

    #endregion

    protected static void SendImporterQueueMessage(string action, string folder)
    {
      QueueMessage msg = new QueueMessage();
      msg.MessageData["action"] = action;
      msg.MessageData["folder"] = folder;
      ServiceScope.Get<IMessageBroker>().Send(IMPORTERSQUEUE_NAME, msg);
    }

    /// <summary>
    /// Will add an import action to be processed asynchronically by our importer worker thread.
    /// </summary>
    /// <param name="action">Import action to be scheduled for being processed asynchronously.
    /// For a list of supported action types, see method <see cref="ProcessImportAction"/>.</param>
    protected void AddAsyncImportAction(object action)
    {
      lock (_syncObject)
        _asyncImportActions.Enqueue(action);
    }

    /// <summary>
    /// Will lock our importers until method <see cref="UnlockImporters"/> is called.
    /// While the importers are locked, none of the methods on any importers will be invoked.
    /// This method is only for temporary use. It MUST be assured that <see cref="UnlockImporters"/>
    /// is called soon after a call to this method.
    /// </summary>
    /// <remarks>
    /// This method can be called multiple times. For each call of <see cref="LockImporters"/>,
    /// the method <see cref="UnlockImporters"/> has to be called.
    /// The importers are finally unlocked when the <see cref="UnlockImporters"/> call belonging
    /// to the first <see cref="LockImporters"/> is done.
    /// </remarks>
    protected void LockImporters()
    {
      lock (_syncObject)
        _importersLocked++;
    }

    /// <summary>
    /// Will unlock our importers. This method MUST be called in succession to a call to
    /// <see cref="LockImporters"/>, and only if <see cref="LockImporters"/> has been called before.
    /// The system needs exactly as many calls of <see cref="UnlockImporters"/> as calls of
    /// <see cref="LockImporters"/>.
    /// </summary>
    protected void UnlockImporters()
    {
      lock (_syncObject)
        _importersLocked--;
    }

    /// <summary>
    /// Returns the information if our importers are locked. This method has to be called within
    /// a lock to our <see cref="_syncObject"/>, because the returned information only has pertinence
    /// while the lock is set.
    /// </summary>
    /// <returns><c>true</c>, if at least one call to <see cref="LockImporters"/> was done without
    /// a call to <see cref="UnlockImporters"/> at the current time.</returns>
    protected bool ImportersLocked()
    {
      return _importersLocked > 0;
    }

    /// <summary>
    /// Periodical check of outstanding import actions, which will be processed as event handler for
    /// our timer <see cref="System.Timers.Timer.Elapsed"/> event.
    /// This method will check if any of the watched folders detected changes, and it will
    /// check the import thread state (and eventually start it).
    /// </summary>
    /// <param name="sender">Not used.</param>
    /// <param name="e">Not used.</param>
    void _timer_Elapsed(object sender, ElapsedEventArgs e)
    {
      lock (_syncObject)
      {
        // Check all watched-folders
        foreach (WatchedFolder watchedFolder in _folders.Values)
        {
          // Did it detect any changes to the file system?
          IList<FileChangeEvent> changes = watchedFolder.GatherChanges();
          if (changes == null) continue;
          // Yep, then we process those
          ServiceScope.Get<ILogger>().Info("ImporterManager: Detected changes in folder '{0}'", watchedFolder.Folder);
          foreach (FileChangeEvent change in changes)
            AddAsyncImportAction(change);
        }
        CheckImportThread();
      }
    }

    /// <summary>
    /// Will check if the import thread is running and eventually start it. This method will
    /// check all relevant state variables for its decision if the thread is started.
    /// </summary>
    void CheckImportThread()
    {
      lock (_syncObject)
      {
        if (_importerThread != null)
          return;
        if (_blockImporterThread)
          return;
        if (_asyncImportActions.Count == 0)
          return;
        _importerThread = new Thread(ExecuteImport);
        _importerThread.IsBackground = true;
        _importerThread.Priority = ThreadPriority.BelowNormal;
        _importerThread.Start();
      }
    }

    /// <summary>
    /// Worker method for the importer thread. This method will process all outstanding
    /// import actions. This method will react to all state variable settings as specified
    /// for the state variables on top.
    /// </summary>
    void ExecuteImport()
    {
      try
      {
        while (true)
        {
          lock (_syncObject)
          {
            if (ImportersLocked())
              return;
            if (_asyncImportActions.Count == 0)
              return;
            object currentImportAction = _asyncImportActions.Dequeue();
            ProcessImportAction(currentImportAction);
            if (_blockImporterThread || ImportersLocked())
              return;
            Thread.Sleep(0); // Yield the rest of the time slice
          }
        }
      }
      finally
      {
        lock (_syncObject)
          _importerThread = null;
      }
    }

    /// <summary>
    /// Processes an import action in the current thread.
    /// </summary>
    /// <remarks>
    /// For convenience, the method signature only supports an <paramref name="action"/>
    /// parameter of type <see cref="object"/>, which will be used for any asynchronous import
    /// action we are supporting. See the code for a list of supported actions.
    /// </remarks>
    /// <param name="action">Import action to process.</param>
    void ProcessImportAction(object action)
    {
      lock (_syncObject)
      {
        FileChangeEvent change = action as FileChangeEvent;
        if (change != null)
        {
          foreach (LazyImporterWrapper importer in _importers)
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
        ImportFolderAction folderAction = action as ImportFolderAction;
        if (folderAction != null)
          DoImportFolder(folderAction.Folder, folderAction.Refresh);
      }
    }

    /// <summary>
    /// Imports a complete folder & any subfolders in the current thread.
    /// </summary>
    /// <param name="folder">The folder to be imported.</param>
    /// <param name="refresh">If set to <c>true</c>, the folder will be refreshed (this means
    /// all objects will be (re-)imported. If set to <c>false</c>, only new or changed objects
    /// will be imported.</param>
    void DoImportFolder(string folder, bool refresh)
    {
      lock (_syncObject)
      {
        try
        {
          ServiceScope.Get<ILogger>().Info("ImporterManager: Importing folder '{0}'", folder);
          for (int i = 0; i < _settings.Shares.Count; ++i)
          {
            Share share = _settings.Shares[i];
            if (FileUtils.PathEquals(share.Folder, folder))
            {
              DateTime dt = share.LastImport;
              if (refresh)
                dt = DateTime.MinValue;
              foreach (LazyImporterWrapper importer in _importers)
                importer.ImportFolder(folder, dt);
              share.LastImport = DateTime.Now;
              ServiceScope.Get<ISettingsManager>().Save(_settings);
            }
          }
        }
        catch (Exception ex)
        {
          ServiceScope.Get<ILogger>().Error("ImporterManager: Import of folder '{0}' failed", ex, folder);
        }
      }
    }
  }
}
