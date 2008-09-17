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
using MediaPortal.Core;
using MediaPortal.Core.Messaging;
using MediaPortal.Utilities.FileSystem;

namespace MediaPortal.Services.ThumbnailGenerator.Database
{
  public class ThumbDatabase
  {
    public const string THUMB_DB_FILENAME = "Thumbs.db";

    protected DirectoryInfo _folder;
    protected FileInfo _dbFile;
    protected IDictionary<string, Thumb> _thumbs = new Dictionary<string, Thumb>();
    protected bool _changed;
    protected DateTime _keepAliveTimer;

    public ThumbDatabase(DirectoryInfo folder)
    {
      _folder = folder;
      _keepAliveTimer = DateTime.Now;
      IMessageBroker msgBroker = ServiceScope.Get<IMessageBroker>();
      IMessageQueue queue = msgBroker.GetOrCreate("contentmanager");
      queue.OnMessageReceive += queue_OnMessageReceive;
      Open();
    }

    void queue_OnMessageReceive(QueueMessage message)
    {
      if (message.MessageData.ContainsKey("action") && message.MessageData.ContainsKey("fullpath"))
      {
        string action = (string)message.MessageData["action"];
        if (action == "changed")
        {
          FileInfo file = new FileInfo((string) message.MessageData["fullpath"]);
          if (FileUtils.PathEquals(file.Directory, _folder))
            if (_thumbs.Remove(file.Name))
              _changed = true;
        }
      }
    }

    public bool Open()
    {
      _thumbs.Clear();
      _dbFile = new FileInfo(Path.Combine(_folder.FullName, THUMB_DB_FILENAME));
      if (!_dbFile.Exists)
        return false;
      //Trace.WriteLine("ThumbDatabase: open " + _dbFile.FullName);

      while (true)
      {
        try
        {
          // Load the database
          using (FileStream stream = _dbFile.Open(FileMode.Open, FileAccess.ReadWrite))
          {
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
                  FileInfo thumbFile = new FileInfo(Path.Combine(_folder.FullName, thumb.FileName));
                  if (thumbFile.Exists && thumbFile.LastWriteTime < _dbFile.LastWriteTime)
                    _thumbs.Add(thumb.FileName, thumb);
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
        return;

      ICollection<Thumb> thumbs = _thumbs.Values;

      // Ensure all images are loaded
      foreach (Thumb thumb in thumbs)
        Get(new FileInfo(thumb.FileName));

      // Save the database

      //Trace.WriteLine("ThumbDatabase: save " + _dbFile.FullName);
      if (_dbFile.Exists)
        _dbFile.Delete();
      using (FileStream stream = _dbFile.Open(FileMode.Create, FileAccess.Write))
      {
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
          long offset = sizeof(Int32);
          foreach (Thumb thumb in thumbs)
            offset += sizeof(Int64) * 2 + thumb.FileName.Length + 1;

          writer.Write((Int32) _thumbs.Count);
          foreach (Thumb thumb in thumbs)
          {
            writer.Write(thumb.FileName);
            writer.Write((Int64) offset);
            writer.Write((Int64) thumb.Size);

            offset += thumb.Image.Length;
          }

          foreach (Thumb thumb in thumbs)
            writer.Write(thumb.Image);
        }
      }
    }

    public void Add(FileInfo file, Stream image)
    {
      byte[] img = new byte[image.Length];
      image.Seek(0, SeekOrigin.Begin);
      image.Read(img, 0, img.Length);
      Add(file, img);
    }

    public void Add(FileInfo file, byte[] image)
    {
      _keepAliveTimer = DateTime.Now;
      if (Contains(file))
        return;
      Thumb thumb = new Thumb();
      thumb.FileName = file.Name;
      thumb.Image = image;
      thumb.Size = image.Length;
      _thumbs.Add(file.Name, thumb);
      _changed = true;
    }

    public byte[] Get(FileInfo file)
    {
      _keepAliveTimer = DateTime.Now;
      if (!_thumbs.ContainsKey(file.Name))
        return null;
      Thumb thumb = _thumbs[file.Name];
      if (thumb.Image == null && _dbFile.Exists)
        using (FileStream stream = _dbFile.Open(FileMode.Open, FileAccess.Read))
          thumb.Read(stream);
      return thumb.Image;
    }

    public bool Contains(FileInfo file)
    {
      if (!FileUtils.PathEquals(file.Directory, _folder))
        return false;
      _keepAliveTimer = DateTime.Now;
      return _thumbs.ContainsKey(file.Name);
    }

    public bool CanFree
    {
      get
      {
        TimeSpan ts = DateTime.Now - _keepAliveTimer;
        return (ts.TotalSeconds >= 20);
      }
    }

    public DirectoryInfo Folder
    {
      get { return _folder; }
    }
  }
}
