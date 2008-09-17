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
using System.Collections.Generic;
using System.IO;
using System.Threading;

using MediaPortal.Services.ThumbnailGenerator.Database;
using MediaPortal.Thumbnails;
using MediaPortal.Utilities.FileSystem;

namespace MediaPortal.Services.ThumbnailGenerator
{
  /// <summary>
  /// Represents a thumbnail generator service, which takes thumbnail generation tasks for input
  /// files and folders to execute asynchronously. There are also methods to query the execution
  /// state for input files.
  /// </summary>
  public class ThumbnailGenerator : IAsyncThumbnailGenerator
  {
    public const string FOLDER_THUMB_NAME = "folder.jpg";

    protected readonly Queue<WorkItem> _workToDo = new Queue<WorkItem>();
    protected Thread _workerThread = null;
    protected WorkItem _currentWorkItem = null;
    protected ThumbDatabaseCache _thumbDatabaseCache;


    public ThumbnailGenerator()
    {
      _thumbDatabaseCache = new ThumbDatabaseCache();
    }

    /// <summary>
    /// Returns the thumbnail database for the specified directory.
    /// </summary>
    /// <param name="directory">The directory to search the thumbnail database.</param>
    /// <returns>Thumbnail database for the specified <paramref name="directory"/>. If no thumbnail
    /// database exists for the specified directory, it will be created.</returns>
    private ThumbDatabase GetDatabase(DirectoryInfo directory)
    {
      return _thumbDatabaseCache.Get(directory);
    }

    private bool Contains(FileSystemInfo fileOrFolder)
    {
      lock (_workToDo)
        foreach (WorkItem item in _workToDo)
          if (FileUtils.PathEquals(item.Source.FullName, fileOrFolder.FullName))
            return true;
      return false;
    }

    private void WorkerThreadMethod()
    {
      while (true)
      {
        WorkItem item;
        lock (_workToDo)
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
        try
        {
          if (item.Source is DirectoryInfo)
          {
            DirectoryInfo soure = (DirectoryInfo) item.Source;
            success = ThumbnailBuilder.CreateThumbnailForFolder(soure, item.Destination,
                item.Width, item.Height, item.Quality,
                _thumbDatabaseCache.Get(soure));
          }
          else
          {
            FileInfo source = (FileInfo) item.Source;
            success = ThumbnailBuilder.CreateThumbnailForFile(source, item.Destination,
                item.Width, item.Height, item.Quality, _thumbDatabaseCache.Get(source.Directory));
          }

          /*
          BitmapImage myBitmapImage = new BitmapImage();

          myBitmapImage.CacheOption = BitmapCacheOption.None;
          myBitmapImage.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
          // BitmapImage.UriSource must be in a BeginInit/EndInit block
          myBitmapImage.BeginInit();
          myBitmapImage.UriSource = new Uri(item.Source);
          myBitmapImage.DecodePixelWidth = item.Width;
          myBitmapImage.EndInit();

          using (FileStream stream = new FileStream(item.Destination, FileMode.Create))
          {
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            encoder.QualityLevel = item.Quality;
            encoder.Frames.Add(BitmapFrame.Create(myBitmapImage));
            encoder.Save(stream);
          }*/
        }
        catch (Exception) {}
        if (item.CreatedDelegate != null)
          item.CreatedDelegate(item.Source, item.Destination, success);
      }
    }

    #region IAsyncThumbnailGenerator implementation

    public bool Exists(FileSystemInfo fileOrFolder)
    {
      if (IsCreating(fileOrFolder))
        return false;
      return fileOrFolder is FileInfo ?
          GetDatabase(((FileInfo)fileOrFolder).Directory).Contains((FileInfo)fileOrFolder) :
          GetDatabase((DirectoryInfo)fileOrFolder).Contains(new FileInfo(Path.Combine(fileOrFolder.FullName, FOLDER_THUMB_NAME)));

    }

    public bool IsCreating(FileSystemInfo fileOrFolder)
    {
      bool isBusy = Contains(fileOrFolder);
      if (_currentWorkItem != null && FileUtils.PathEquals(_currentWorkItem.Source.FullName, fileOrFolder.FullName))
        isBusy = true;
      return isBusy;
    }

    public byte[] GetThumbnail(FileSystemInfo fileOrFolder)
    {
      return fileOrFolder is FileInfo ?
          GetDatabase(((FileInfo)fileOrFolder).Directory).Get((FileInfo)fileOrFolder) :
          GetDatabase((DirectoryInfo)fileOrFolder).Get(new FileInfo(Path.Combine(fileOrFolder.FullName, FOLDER_THUMB_NAME)));
    }

    public void CreateThumbnail(FileSystemInfo fileOrFolder)
    {
      Create(fileOrFolder, 192, 192, 50, null);
    }

    public void Create(FileSystemInfo fileOrFolder, int width, int height, int quality, CreatedDelegate createdDelegate)
    {
      if (Contains(fileOrFolder))
        return;
      FileInfo destination;
      if (fileOrFolder is FileInfo)
        destination = new FileInfo(Path.ChangeExtension(fileOrFolder.FullName, ".jpg"));
      else
        destination = new FileInfo(Path.Combine(fileOrFolder.FullName, FOLDER_THUMB_NAME));
      WorkItem newItem = new WorkItem(fileOrFolder, destination, width, height, quality, createdDelegate);
      lock (_workToDo)
        _workToDo.Enqueue(newItem);
      if (_workerThread != null)
        return;
      _workerThread = new Thread(WorkerThreadMethod);
      _workerThread.IsBackground = true;
      _workerThread.Name = "Thumbnail Generator Thread";
      _workerThread.Start();
    }

    #endregion
  }
}
