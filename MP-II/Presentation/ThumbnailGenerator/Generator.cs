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

using Presentation.ThumbnailGenerator.Database;

namespace Presentation.ThumbnailGenerator
{
  public class Generator
  {
    private List<WorkItem> _workToDo;
    private Thread _workerThread;
    private static Generator _instance;
    private WorkItem _currentWorkItem;

    public delegate void CreatedHandler(string source, string destination);

    public event CreatedHandler OnCreated;

    public Generator()
    {
      _workToDo = new List<WorkItem>();
    }

    public static Generator Instance
    {
      get
      {
        if (_instance == null)
        {
          _instance = new Generator();
        }
        return _instance;
      }
    }

    private ThumbDatabase GetDatabase(string fileName)
    {
      string path = Path.GetDirectoryName(fileName);
      ThumbDatabase dbs = ThumbDatabaseCache.Instance.Get(path);
      return dbs;
    }

    public bool Exists(string fileName)
    {
      if (IsCreating(fileName))
      {
        return false;
      }
      string file = Path.GetFileName(fileName);
      return GetDatabase(fileName).Contains(file);
    }

    public bool IsCreating(string fileName)
    {
      bool isBusy = Contains(fileName);
      if (_currentWorkItem != null && _currentWorkItem.Source == fileName)
      {
        isBusy = true;
      }
      return isBusy;
    }

    public byte[] GetThumbNail(string fileName)
    {
      string file = Path.GetFileName(fileName);
      return GetDatabase(fileName).Get(file);
    }

    public void CreateThumbnail(string fileName)
    {
      Create(fileName, 192, 192, 50);
    }


    private bool Contains(string fileName)
    {
      lock (_workToDo)
      {
        foreach (WorkItem item in _workToDo)
        {
          if (item.Source == fileName)
          {
            return true;
          }
        }
      }
      return false;
    }

    public void Create(string fileName, int width, int height, int quality)
    {
      if (Contains(fileName))
      {
        return;
      }

      string destination = Path.ChangeExtension(fileName, ".jpg");
      WorkItem newItem = new WorkItem(fileName, destination, width, height, quality);
      lock (_workToDo)
      {
        _workToDo.Add(newItem);
      }
      if (_workerThread == null)
      {
        _workerThread = new Thread(new ThreadStart(workerThread));
        _workerThread.IsBackground = true;
        _workerThread.Name = "Thumbnail Generator Thread";
        _workerThread.Start();
      }
    }

    private void workerThread()
    {
      while (true)
      {
        WorkItem item = null;
        lock (_workToDo)
        {
          int count = _workToDo.Count;
          if (count == 0)
          {
            _workerThread = null;
            _currentWorkItem = null;
            return;
          }
          item = _workToDo[0];
          _workToDo.RemoveAt(0);
        }
        _currentWorkItem = item;

        try
        {
          ThumbnailBuilder builder = new ThumbnailBuilder();
          if (item.IsFolder)
          {
            builder.CreateThumbnailForFolder(item);
          }
          else
          {
            builder.CreateThumbnailForFile(item);
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
        if (OnCreated != null)
        {
          OnCreated(item.Source, item.Destination);
        }
      }
    }
  }
}
