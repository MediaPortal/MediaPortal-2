#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using MediaPortal.Core.Logging;
using MediaPortal.Utilities.FileSystem;

namespace MediaPortal.Core.Services.ThumbnailGenerator
{
  /// <summary>
  /// Represents a thumbnail generator service, which takes thumbnail generation tasks for input
  /// files and folders to execute asynchronously. There are also methods to query the execution
  /// state for input files.
  /// </summary>
  public class ThumbnailGenerator : IThumbnailGenerator, IDisposable
  {
    public const string FOLDER_THUMB_NAME = "folder.jpg";
    public const int DEFAULT_THUMB_WIDTH = 192;
    public const int DEFAULT_THUMB_HEIGHT = 192;
    public const ImageType DEFAULT_THUMB_IMAGE_TYPE = ImageType.Jpeg;

    protected readonly Queue<WorkItem> _workToDo = new Queue<WorkItem>();
    protected Thread _workerThread = null;
    protected WorkItem _currentWorkItem = null;

    public void Dispose()
    {
    }

    protected bool IsInQueue(string fileOrFolderPath)
    {
      lock (this)
        return _workToDo.Any(item => FileUtils.PathEquals(item.SourcePath, fileOrFolderPath));
    }

    protected void Worker()
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

        CreateThumbCallback(item.SourcePath, item.Width, item.Height, item.CreatedDelegate);
      }
    }

    protected static bool CreateThumbCallback(string sourcePath, int width, int height, CreatedDelegate createdDelegate)
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

    protected static bool GetThumbnailInternal(string fileOrFolderPath, int width, int height, bool cachedOnly, out byte[] imageData, out ImageType imageType)
    {
      imageType = ImageType.Jpeg;
      ShellThumbnailBuilder shellThumbnailBuilder = new ShellThumbnailBuilder();
      return shellThumbnailBuilder.GetThumbnail(fileOrFolderPath, width, height, cachedOnly, ImageFormat.Jpeg, out imageData);
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

    #region IThumbnailGenerator implementation

    public bool GetThumbnail(string fileOrFolderPath, out byte[] imageData, out ImageType imageType)
    {
      return GetThumbnail(fileOrFolderPath, DEFAULT_THUMB_WIDTH, DEFAULT_THUMB_HEIGHT, out imageData, out imageType);
    }

    public bool GetThumbnail(string fileOrFolderPath, int width, int height, out byte[] imageData, out ImageType imageType)
    {
      return GetThumbnailInternal(fileOrFolderPath, width, height, true, out imageData, out imageType);
    }

    public void GetThumbnail_Async(string fileOrFolderPath, CreatedDelegate createdDelegate)
    {
      GetThumbnail_Async(fileOrFolderPath, DEFAULT_THUMB_WIDTH, DEFAULT_THUMB_HEIGHT, createdDelegate);
    }

    public void GetThumbnail_Async(string fileOrFolderPath, int width, int height, CreatedDelegate createdDelegate)
    {
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
        _workerThread = new Thread(Worker) {IsBackground = true, Name = "ThumbGen"};
        _workerThread.Start();
      }
    }

    #endregion
  }
}
