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
using System.IO;
using System.Linq;
using System.Threading;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.PluginManager.Exceptions;
using MediaPortal.Utilities.FileSystem;

namespace MediaPortal.Common.Services.ThumbnailGenerator
{
  /// <summary>
  /// Represents a thumbnail generator service, which takes thumbnail generation tasks for input
  /// files and folders to execute asynchronously. There are also methods to query the execution
  /// state for input files.
  /// </summary>
  public class ThumbnailGenerator : IThumbnailGenerator, IDisposable
  {
    public const int DEFAULT_THUMB_WIDTH = 512;
    public const int DEFAULT_THUMB_HEIGHT = 512;
    public const ImageType DEFAULT_THUMB_IMAGE_TYPE = ImageType.Jpeg;

    protected List<SortedThumbnailCreator> _providerList = null;
    protected IPluginItemStateTracker _thumbnailProviderPluginItemStateTracker;
    protected readonly Queue<WorkItem> _workToDo = new Queue<WorkItem>();
    protected Thread _workerThread = null;
    protected WorkItem _currentWorkItem = null;
    protected readonly object _syncObj = new object();

    #region Internal class
    protected class SortedThumbnailCreator : IDisposable
    {
      public int Priority;
      public IThumbnailProvider Provider;
      public void Dispose()
      {
        IDisposable disp = Provider as IDisposable;
        if (disp != null)
          disp.Dispose();
      }
    }
    #endregion

    public void InitProviders()
    {
      lock (_syncObj)
      {
        if (_providerList != null)
          return;

        var providerList = new List<SortedThumbnailCreator>();

        _thumbnailProviderPluginItemStateTracker = new FixedItemStateTracker("ThumbnailGenerator Service - Provider registration");

        IPluginManager pluginManager = ServiceRegistration.Get<IPluginManager>();
        foreach (PluginItemMetadata itemMetadata in pluginManager.GetAllPluginItemMetadata(ThumbnailProviderBuilder.THUMBNAIL_PROVIDER_PATH))
        {
          try
          {
            ThumbnailProviderRegistration thumbnailProviderRegistration = pluginManager.RequestPluginItem<ThumbnailProviderRegistration>(ThumbnailProviderBuilder.THUMBNAIL_PROVIDER_PATH, itemMetadata.Id, _thumbnailProviderPluginItemStateTracker);
            if (thumbnailProviderRegistration == null || thumbnailProviderRegistration.ProviderClass == null)
              ServiceRegistration.Get<ILogger>().Warn("Could not instantiate IThumbnailProvider with id '{0}'", itemMetadata.Id);
            else
            {
              IThumbnailProvider provider = Activator.CreateInstance(thumbnailProviderRegistration.ProviderClass) as IThumbnailProvider;
              if (provider == null)
                throw new PluginInvalidStateException("Could not create IThumbnailProvider instance of class {0}", thumbnailProviderRegistration.ProviderClass);
              providerList.Add(new SortedThumbnailCreator { Priority = thumbnailProviderRegistration.Priority, Provider = provider });
            }
          }
          catch (PluginInvalidStateException e)
          {
            ServiceRegistration.Get<ILogger>().Warn("Cannot add IThumbnailProvider with id '{0}'", e, itemMetadata.Id);
          }
        }
        providerList.Sort((p1, p2) => p1.Priority.CompareTo(p2.Priority));
        _providerList = providerList;
      }
    }

    public void Dispose()
    {
      if (_providerList != null)
        foreach (IDisposable result in _providerList.OfType<IDisposable>())
          result.Dispose();
    }

    protected bool IsInQueue(string fileOrFolderPath)
    {
      lock (_syncObj)
        return _workToDo.Any(item => FileUtils.PathEquals(item.SourcePath, fileOrFolderPath));
    }

    protected void Worker()
    {
      while (true)
      {
        WorkItem item;
        lock (_syncObj)
        {
          int count = _workToDo.Count;
          if (count == 0)
          {
            _workerThread = null;
            _currentWorkItem = null;
            return;
          }
          item = _workToDo.Dequeue();
        }
        _currentWorkItem = item;

        CreateThumbCallback(item.SourcePath, item.Width, item.Height, item.CreatedDelegate);
      }
    }

    protected bool CreateThumbCallback(string sourcePath, int width, int height, CreatedDelegate createdDelegate)
    {
      bool success = false;
      byte[] imageData = null;
      ImageType imageType = ImageType.Unknown;
      try
      {
        if (Directory.Exists(sourcePath) || File.Exists(sourcePath))
          success = GetThumbnailInternal(sourcePath, width, height, false, out imageData, out imageType);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Warn("ThumbnailGenerator: Error creating thumbnails", ex);
      }
      if (createdDelegate != null)
        createdDelegate(sourcePath, success, imageData, imageType);
      return success;
    }

    protected bool GetThumbnailInternal(string fileOrFolderPath, int width, int height, bool cachedOnly, out byte[] imageData, out ImageType imageType)
    {
      InitProviders();
      imageType = ImageType.Jpeg;
      foreach (var thumbnailProvider in _providerList)
      {
        try
        {
          if (thumbnailProvider.Provider.GetThumbnail(fileOrFolderPath, width, height, cachedOnly, out imageData, out imageType))
            return true;
        }
        catch (Exception ex)
        {
          ServiceRegistration.Get<ILogger>().Error("Error creating thumbnail for '{0}' using provider '{1}", ex, fileOrFolderPath, thumbnailProvider.GetType().Name);
        }
      }
      imageData = null;
      return false;
    }

    public bool IsCreating(string fileOrFolderPath)
    {
      lock (_syncObj)
      {
        if (IsInQueue(fileOrFolderPath))
          return true;
        if (_currentWorkItem != null && FileUtils.PathEquals(_currentWorkItem.SourcePath, fileOrFolderPath))
          return true;
      }
      return false;
    }

    #region IThumbnailGenerator implementation

    public bool GetThumbnail(string fileOrFolderPath, bool cacheOnly, out byte[] imageData, out ImageType imageType)
    {
      return GetThumbnail(fileOrFolderPath, DEFAULT_THUMB_WIDTH, DEFAULT_THUMB_HEIGHT, cacheOnly, out imageData, out imageType);
    }

    public bool GetThumbnail(string fileOrFolderPath, int width, int height, bool cacheOnly, out byte[] imageData, out ImageType imageType)
    {
      return GetThumbnailInternal(fileOrFolderPath, width, height, cacheOnly, out imageData, out imageType);
    }

    public void GetThumbnail_Async(string fileOrFolderPath, CreatedDelegate createdDelegate)
    {
      GetThumbnail_Async(fileOrFolderPath, DEFAULT_THUMB_WIDTH, DEFAULT_THUMB_HEIGHT, createdDelegate);
    }

    public void GetThumbnail_Async(string fileOrFolderPath, int width, int height, CreatedDelegate createdDelegate)
    {
      InitProviders();
      if (IsCreating(fileOrFolderPath))
        return;
      if (width == 0)
        width = DEFAULT_THUMB_WIDTH;
      if (height == 0)
        height = DEFAULT_THUMB_WIDTH;
      WorkItem newItem = new WorkItem(fileOrFolderPath, width, height, createdDelegate);
      lock (this)
      {
        _workToDo.Enqueue(newItem);
        if (_workerThread != null)
          return;
        _workerThread = new Thread(Worker) { IsBackground = true, Name = "ThumbGen" };
        _workerThread.Start();
      }
    }

    #endregion
  }
}
