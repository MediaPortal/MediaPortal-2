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
using MediaPortal.Thumbnails;
using MediaPortal.Utilities.FileSystem;

namespace MediaPortal.Services.ThumbnailGenerator.Database
{
  /// <summary>
  /// Database for image and video thumbnails of one folder.
  /// </summary>
  /// <remarks>
  /// This class manages reading and writing a thumbnail database file which is located in the
  /// thumb database folder. Its name is the name stored in <see cref="ThumbDatabase.THUMB_DB_FILENAME"/>.
  /// The thumb database file is independent from its location; it doesn't contain the path of the folder
  /// it was created for, so it still works if the folder gets moved.
  /// The file is built lazily; for a given time not all contents of the folder must be stored.
  /// If a change to a source file for a thumbnail has been detected, the thumbnail image gets
  /// deleted from the db.
  /// </remarks>
  public class ThumbDatabase
  {
    public const string THUMB_DB_FILENAME = "Thumbs.db";

    protected readonly string _folderPath;
    protected readonly string _dbFilePath;
    protected DateTime _lastUsed;

    protected IDictionary<string, Thumb> _thumbs = new Dictionary<string, Thumb>(
        WindowsFilesystemPathEqualityComparer.Instance);
    protected bool _changed = false;

    public ThumbDatabase(string folderPath) : this(folderPath, Path.Combine(folderPath, THUMB_DB_FILENAME)) { }

    public ThumbDatabase(string folderPath, string dbFilePath)
    {
      _folderPath = folderPath;
      _dbFilePath = dbFilePath;
      // FIXME: Don't observe the contentmanager queue here
      IMessageBroker msgBroker = ServiceScope.Get<IMessageBroker>();
      msgBroker.Register_Async("contentmanager", queue_OnMessageReceived);
      Load();
    }

    void queue_OnMessageReceived(QueueMessage message)
    {
      if (message.MessageData.ContainsKey("action") && message.MessageData.ContainsKey("fullpath"))
      {
        string action = (string)message.MessageData["action"];
        if (action == "changed")
        {
          string filePath = (string) message.MessageData["fullpath"];
          if (FileUtils.PathEquals(Path.GetDirectoryName(filePath), _folderPath))
            lock (this)
            {
              if (_thumbs.Remove(Path.GetFileName(filePath)))
                _changed = true;
            }
        }
      }
    }

    /// <summary>
    /// Returns the time this thumbnail db was used the last time.
    /// This value can be used as a timeout to release the db.
    /// </summary>
    public DateTime LastUsed
    {
      get { lock (this) return _lastUsed; }
    }

    /// <summary>
    /// Returns the folder path this db works on.
    /// </summary>
    public string FolderPath
    {
      get { return _folderPath; }
    }

    /// <summary>
    /// Updates the last usage time of this thumbnail database to the current
    /// system time.
    /// </summary>
    public void NotifyUsage()
    {
      _lastUsed = DateTime.Now;
    }

    /// <summary>
    /// Loads or reloads this thumbnail db from the db file on disk. If the db file
    /// doesn't exist, the database is cleared.
    /// </summary>
    public bool Load()
    {
      lock (this) {
        _thumbs.Clear();
        if (!File.Exists(_dbFilePath))
          return false;

        _changed = false;
        try
        {
          // Load the database
          using (FileStream stream = new FileStream(_dbFilePath, FileMode.Open))
          using (BinaryReader reader = new BinaryReader(stream))
          {
            int count = reader.ReadInt32();
            for (int i = 0; i < count; ++i)
            {
              Thumb thumb = new Thumb();
              thumb.Name = reader.ReadString();
              thumb.ImageType = (ImageType)reader.ReadInt32();
              thumb.Offset = reader.ReadInt64();
              thumb.Size = reader.ReadInt64();
              string thumbPath = Path.Combine(_folderPath, thumb.Name);
              if (File.Exists(thumbPath) && File.GetLastWriteTime(thumbPath) < File.GetLastWriteTime(_dbFilePath))
                _thumbs.Add(thumb.Name, thumb);
              else
                _changed = true;
            }
          }
        }
        catch (IOException) { }
        NotifyUsage();
        return true;
      }
    }

    /// <summary>
    /// Closes this database.
    /// </summary>
    public void Close()
    {
      Flush();
    }

    /// <summary>
    /// Writes all thumbnail data to the database file.
    /// </summary>
    public void Flush()
    {
      lock (this)
      {
        if (!_changed)
          return;

        ICollection<Thumb> thumbs = _thumbs.Values;

        // Ensure all images are loaded
        foreach (Thumb thumb in thumbs)
        {
          byte[] imageData;
          ImageType imageType;
          Get(thumb.Name, out imageData, out imageType);
        }

        // Save the database
        if (File.Exists(_dbFilePath))
          File.Delete(_dbFilePath);
        using (FileStream stream = new FileStream(_dbFilePath, FileMode.Create))
        {
          using (BinaryWriter writer = new BinaryWriter(stream))
          {
            long offset = sizeof(Int32);
            foreach (Thumb thumb in thumbs) // Calculate offset of the cummulated headers
              offset += thumb.Name.Length + 1 + sizeof(Int32) + sizeof(Int64) + sizeof(Int64);

            writer.Write((Int32) _thumbs.Count);
            foreach (Thumb thumb in thumbs)
            {
              writer.Write(thumb.Name);
              writer.Write((Int32) thumb.ImageType);
              writer.Write((Int64) offset);
              writer.Write((Int64) thumb.Size);

              offset += thumb.Image.Length;
            }

            foreach (Thumb thumb in thumbs)
              writer.Write(thumb.Image);
          }
        }
      }
    }

    public bool Contains(string fileName)
    {
      lock (this)
      {
        NotifyUsage();
        return _thumbs.ContainsKey(Path.GetFileName(fileName));
      }
    }

    public void Add(string fileName, ImageType imageType, Stream image)
    {
      byte[] img = new byte[image.Length];
      image.Seek(0, SeekOrigin.Begin);
      image.Read(img, 0, img.Length);
      Add(fileName, imageType, img);
    }

    public void Add(string fileName, ImageType imageType, byte[] image)
    {
      lock (this)
      {
        NotifyUsage();
        Thumb thumb = new Thumb();
        thumb.Name = Path.GetFileName(fileName);
        thumb.ImageType = imageType;
        thumb.Image = image;
        thumb.Size = image.Length;
        _thumbs.Add(thumb.Name, thumb);
        _changed = true;
      }
    }

    /// <summary>
    /// Returns the image data of the thumbnail for the file of the specified <paramref name="fileName"/>.
    /// </summary>
    /// <param name="fileName">Name of a file in the folder of this thumbnail db.</param>
    /// <returns><c>true, if the specified thumbnail is present in this db, else <c>false</c>.
    /// If the return value is <c>true</c>, the parameters <paramref name="imageData"/> and <paramref name="imageType"/> are
    /// set, else they are undefined.</returns>
    public bool Get(string fileName, out byte[] imageData, out ImageType imageType)
    {
      lock (this)
      {
        NotifyUsage();
        fileName = Path.GetFileName(fileName);
        imageData = null;
        imageType = ImageType.Unknown;
        if (!_thumbs.ContainsKey(fileName))
          return false;
        Thumb thumb = _thumbs[fileName];
        if (thumb.Image == null && File.Exists(_dbFilePath))
          using (FileStream stream = new FileStream(_dbFilePath, FileMode.Open))
          {
            thumb.Image = new byte[thumb.Size];
            stream.Seek(thumb.Offset, SeekOrigin.Begin);
            stream.Read(thumb.Image, 0, (int) thumb.Size);
          }
        imageData = thumb.Image;
        imageType = thumb.ImageType;
        return true;
      }
    }
  }
}
