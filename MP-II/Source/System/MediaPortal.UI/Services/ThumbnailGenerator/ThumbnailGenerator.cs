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
using System.Collections.Generic;
using System.IO;
using System.Threading;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.UI.Services.ThumbnailGenerator.Database;
using MediaPortal.UI.Thumbnails;
using MediaPortal.Utilities.FileSystem;

namespace MediaPortal.UI.Services.ThumbnailGenerator
{
  /// <summary>
  /// Represents a thumbnail generator service, which takes thumbnail generation tasks for input
  /// files and folders to execute asynchronously. There are also methods to query the execution
  /// state for input files.
  /// </summary>
  public class ThumbnailGenerator : IAsyncThumbnailGenerator
  {
    public const string FOLDER_THUMB_NAME = "folder.jpg";
    public const int DEFAULT_THUMB_WIDTH = 192;
    public const int DEFAULT_THUMB_HEIGHT = 192;
    public const int DEFAULT_THUMB_QUALITY = 50;
    public const ImageType DEFAULT_THUMB_IMAGE_TYPE = ImageType.Jpeg;

    protected readonly Queue<WorkItem> _workToDo = new Queue<WorkItem>();
    protected Thread _workerThread = null;
    protected WorkItem _currentWorkItem = null;
    protected ThumbDatabaseCache _thumbDatabaseCache;

    public ThumbnailGenerator()
    {
      _thumbDatabaseCache = new ThumbDatabaseCache();
    }

    private bool IsInQueue(string fileOrFolderPath)
    {
      lock (this)
        foreach (WorkItem item in _workToDo)
          if (FileUtils.PathEquals(item.SourcePath, fileOrFolderPath))
            return true;
      return false;
    }

    private void WorkerThreadMethod()
    {
      while (true)
      {
        WorkItem item;
        lock (this)
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

        bool success = false;
        ThumbDatabase database = null;
        string databaseFolderPath = null;
        try
        {
          string sourcePath = item.SourcePath;
          try
          {
            if (Directory.Exists(sourcePath))
            {
              databaseFolderPath = sourcePath;
              database = _thumbDatabaseCache.Acquire(databaseFolderPath);
              success = ThumbnailBuilder.CreateThumbnailForFolder(sourcePath, FOLDER_THUMB_NAME,
                  item.Width, item.Height, item.Quality, DEFAULT_THUMB_IMAGE_TYPE, database);
            }
            else if (File.Exists(sourcePath))
            {
              databaseFolderPath = Path.GetDirectoryName(sourcePath);
              database = _thumbDatabaseCache.Acquire(databaseFolderPath);
              success = ThumbnailBuilder.CreateThumbnailForFile(sourcePath,
                  item.Width, item.Height, item.Quality, DEFAULT_THUMB_IMAGE_TYPE, database);
            }
          }
          catch (Exception ex)
          {
            ServiceScope.Get<ILogger>().Warn("ThumbnailGenerator: Error creating thumbnails", ex);
          }
          if (item.CreatedDelegate != null)
          {
            byte[] imageData = null;
            ImageType imageType = ImageType.Unknown;
            if (success)
              success = database.Get(sourcePath, out imageData, out imageType);
            item.CreatedDelegate(item.SourcePath, success, imageData, imageType);
          }
        }
        finally
        {
          if (databaseFolderPath != null)
            _thumbDatabaseCache.Release(databaseFolderPath);
        }
      }
    }

    protected static bool GetDatabaseFolderAndFileName(string fileOrFolderPath,
        out string folderPath, out string fileName)
    {
      folderPath = null;
      fileName = null;
      if (File.Exists(fileOrFolderPath))
      {
        folderPath = Path.GetDirectoryName(fileOrFolderPath);
        fileName = Path.GetFileName(fileOrFolderPath);
        return true;
      }
      else if (Directory.Exists(fileOrFolderPath))
      {
        folderPath = fileOrFolderPath;
        fileName = FOLDER_THUMB_NAME;
        return true;
      }
      return false;
    }

    #region IAsyncThumbnailGenerator implementation

    public bool Exists(string fileOrFolderPath)
    {
      if (IsCreating(fileOrFolderPath))
        return false;
      string folderPath;
      string fileName;
      if (!GetDatabaseFolderAndFileName(fileOrFolderPath, out folderPath, out fileName))
        return false;
      try
      {
        return _thumbDatabaseCache.Acquire(folderPath).Contains(fileName);
      }
      finally
      {
        _thumbDatabaseCache.Release(folderPath);
      }
    }

    public bool IsCreating(string fileOrFolderPath)
    {
      lock (this)
      {
        if (IsInQueue(fileOrFolderPath))
          return true;
        if (_currentWorkItem != null && FileUtils.PathEquals(_currentWorkItem.SourcePath, fileOrFolderPath))
          return true;
      }
      return false;
    }

    public bool GetThumbnail(string fileOrFolderPath, out byte[] imageData, out ImageType imageType)
    {
      imageData = null;
      imageType = ImageType.Unknown;
      string folderPath;
      string fileName;
      if (!GetDatabaseFolderAndFileName(fileOrFolderPath, out folderPath, out fileName))
        return false;
      try
      {
        return _thumbDatabaseCache.Acquire(folderPath).Get(fileName, out imageData, out imageType);
      }
      finally
      {
        _thumbDatabaseCache.Release(folderPath);
      }
    }

    public void CreateThumbnail(string fileOrFolderPath)
    {
      Create(fileOrFolderPath, DEFAULT_THUMB_WIDTH, DEFAULT_THUMB_HEIGHT, DEFAULT_THUMB_QUALITY, null);
    }

    public void Create(string fileOrFolderPath, int width, int height, int quality, CreatedDelegate createdDelegate)
    {
      if (IsCreating(fileOrFolderPath))
        return;
      WorkItem newItem = new WorkItem(fileOrFolderPath, width, height, quality, createdDelegate);
      lock (this)
      {
        _workToDo.Enqueue(newItem);
        if (_workerThread != null)
          return;
        _workerThread = new Thread(WorkerThreadMethod);
        _workerThread.IsBackground = true;
        _workerThread.Name = "Thumbnail Generator Thread";
        _workerThread.Start();
      }
    }

    #endregion
  }
}
