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
using System.Diagnostics;
using System.IO;
using MediaPortal.Core;
using MediaPortal.Core.Messaging;

namespace MediaPortal.Services.ThumbnailGenerator.Database
{
  public class ThumbDatabase
  {
    private string _dbName;
    private List<Thumb> _thumbs = new List<Thumb>();
    private bool _changed;
    private DateTime _keepAliveTimer;
    string _folder;

    public ThumbDatabase()
    {
      _thumbs = new List<Thumb>();
      _keepAliveTimer = DateTime.Now;
      IMessageBroker msgBroker = ServiceScope.Get<IMessageBroker>();
      IMessageQueue queue = msgBroker.GetOrCreate("contentmanager");
      queue.OnMessageReceive += new MessageReceivedHandler(queue_OnMessageReceive);
    }

    void queue_OnMessageReceive(QueueMessage message)
    {
      if (message.MessageData.ContainsKey("action") && message.MessageData.ContainsKey("fullpath"))
      {
        string action = (string)message.MessageData["action"];
        if (action == "changed")
        {
          string fileName = (string)message.MessageData["fullpath"];

          for (int i = 0; i < _thumbs.Count; ++i)
          {
            string fname = String.Format(@"{0}\{1}", _folder, _thumbs[i].FileName);
            if (fname == fileName)
            {
              _thumbs.RemoveAt(i);
              _changed = true;
              break;
            }
          }
        }
      }
    }

    public bool Open(string folder)
    {
      _folder = folder;
      _thumbs.Clear();
      _dbName = String.Format(@"{0}\mpThumbs.db", folder);
      if (!File.Exists(_dbName))
      {
        return false;
      }
      Trace.WriteLine("open:" + _dbName);

      while (true)
      {
        try
        {
          //load the database
          using (FileStream stream = new FileStream(_dbName, FileMode.Open, FileAccess.Read))
          {
            FileInfo thumbInfo = new FileInfo(_dbName);
            if (stream.Length > 4)
            {
              using (BinaryReader reader = new BinaryReader(stream))
              {
                int count = reader.ReadInt32();
                for (int i = 0; i < count; ++i)
                {
                  Thumb thumb = new Thumb();
                  thumb.FileName = reader.ReadString();
                  thumb.Offset = reader.ReadInt64();
                  thumb.Size = reader.ReadInt64();
                  if (File.Exists(folder + @"\\" + thumb.FileName))
                  {
                    FileInfo info = new FileInfo(folder + @"\\" + thumb.FileName);
                    if (info.LastWriteTime < thumbInfo.LastWriteTime)
                    {
                      _thumbs.Add(thumb);
                    }
                  }
                }
              }
            }
            break;
          }
        }
        catch (IOException) { }
      }
      _changed = false;
      _keepAliveTimer = DateTime.Now;
      return true;
    }

    public void Close()
    {
      if (!_changed)
      {
        return;
      }

      //ensure all images are loaded
      foreach (Thumb thumb in _thumbs)
      {
        Get(thumb.FileName);
      }

      //save the database

      Trace.WriteLine("save:" + _dbName);
      if (File.Exists(_dbName))
      {
        File.Delete(_dbName);
      }
      using (FileStream stream = new FileStream(_dbName, FileMode.Create, FileAccess.Write))
      {
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
          long offset = sizeof(Int32);
          foreach (Thumb thumb in _thumbs)
          {
            offset += thumb.HeaderSize;
          }

          writer.Write((Int32)_thumbs.Count);
          foreach (Thumb thumb in _thumbs)
          {
            writer.Write(thumb.FileName);
            writer.Write((Int64)offset);
            writer.Write((Int64)thumb.Size);

            offset += thumb.Image.Length;
          }

          foreach (Thumb thumb in _thumbs)
          {
            writer.Write(thumb.Image);
          }
        }
      }
    }

    public void Add(string fileName, Stream image)
    {
      _keepAliveTimer = DateTime.Now;
      if (!Contains(fileName))
      {
        Thumb thumb = new Thumb();
        thumb.FileName = fileName;
        byte[] img = new byte[image.Length];
        image.Seek(0, SeekOrigin.Begin);
        image.Read(img, 0, img.Length);
        thumb.Image = img;
        thumb.Size = image.Length;
        _thumbs.Add(thumb);
        _changed = true;
      }
    }

    public void Add(string fileName, byte[] image)
    {
      _keepAliveTimer = DateTime.Now;
      if (!Contains(fileName))
      {
        Thumb thumb = new Thumb();
        thumb.FileName = fileName;
        thumb.Image = image;
        thumb.Size = image.Length;
        _thumbs.Add(thumb);
        _changed = true;
      }
    }

    public byte[] Get(string fileName)
    {
      _keepAliveTimer = DateTime.Now;
      foreach (Thumb thumb in _thumbs)
      {
        if (thumb.FileName == fileName)
        {
          if (thumb.Image == null)
          {
            if (File.Exists(_dbName))
            {
              using (FileStream stream = new FileStream(_dbName, FileMode.Open, FileAccess.Read))
              {
                thumb.Read(stream);
              }
            }
          }
          return thumb.Image;
        }
      }
      return null;
    }

    public bool Contains(string fileName)
    {
      _keepAliveTimer = DateTime.Now;
      foreach (Thumb thumb in _thumbs)
      {
        if (thumb.FileName == fileName)
        {
          return true;
        }
      }
      return false;
    }

    public bool CanFree
    {
      get
      {
        TimeSpan ts = DateTime.Now - _keepAliveTimer;
        return (ts.TotalSeconds >= 20);
      }
    }
  }
}
