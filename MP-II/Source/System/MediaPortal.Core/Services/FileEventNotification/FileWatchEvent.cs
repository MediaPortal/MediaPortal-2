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
using System.IO;
using MediaPortal.Core.FileEventNotification;

namespace MediaPortal.Core.Services.FileEventNotification
{

  /// <summary>
  /// FileWatchEvent represents an event caused by a watched file.
  /// This class is able to detect duplicate events by calling the IsDuplicate() method.
  /// </summary>
  public class FileWatchEvent
  {

    #region Variables

    /// <summary>
    /// The path to the affected file or directory.
    /// </summary>
    protected readonly string _path;
    /// <summary>
    /// The old path, will be different to _path if we're handling a rename event.
    /// </summary>
    protected readonly string _oldPath;
    /// <summary>
    /// The type of the change.
    /// </summary>
    protected readonly FileWatchChangeType _type;
    /// <summary>
    /// Indicates whether the curren FileWatchEvent is delayed already.
    /// </summary>
    protected bool _delayed;
    /// <summary>
    /// The file's size at the moment the event occured.
    /// </summary>
    protected long _fileSize;
    /// <summary>
    /// The moment the event is created.
    /// </summary>
    protected DateTime _eventMoment;

    #endregion

    #region Properties

    /// <summary>
    /// Gets the affected path.
    /// </summary>
    public string Path
    {
      get { return _path; }
    }

    /// <summary>
    /// Gets the old path of the affected path.
    /// Will be different to Path if a Rename event is handled.
    /// </summary>
    public string OldPath
    {
      get { return _oldPath; }
    }

    /// <summary>
    /// Gets the type of change.
    /// </summary>
    public FileWatchChangeType ChangeType
    {
      get { return _type; }
    }

    /// <summary>
    /// Gets or sets whether this event has been delayed.
    /// If true, the event is allowed to be raised.
    /// </summary>
    public bool Delayed
    {
      get { return _delayed; }
      set { _delayed = value; }
    }

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the FileWatchEvent class
    /// based on the specified FileSystemEventArgs.
    /// </summary>
    /// <param name="args">FileSystemEventArgs to base the current FileWatchEvent on.</param>
    public FileWatchEvent(FileSystemEventArgs args)
    {
      _eventMoment = DateTime.Now;
      _path = args.FullPath;
      if (args is RenamedEventArgs)
        _oldPath = ((RenamedEventArgs)args).OldFullPath;
      else
        _oldPath = _path;
      switch (args.ChangeType)
      {
        case WatcherChangeTypes.All:
          _type = FileWatchChangeType.All;
          break;
        case WatcherChangeTypes.Changed:
          _type = FileWatchChangeType.Changed;
          break;
        case WatcherChangeTypes.Created :
          _type = FileWatchChangeType.Created;
          break;
        case WatcherChangeTypes.Deleted:
          _type = FileWatchChangeType.Deleted;
          break;
        case WatcherChangeTypes.Renamed:
          _type = FileWatchChangeType.Renamed;
          break;
      }
      _fileSize = GetFileSize(_path);
    }

    /// <summary>
    /// Initializes a new instance of the FileWatchEvent class.
    /// Rename events are not allowed with this constructor.
    /// </summary>
    /// <param name="type">Type of event.</param>
    /// <param name="path">Affected path.</param>
    public FileWatchEvent(FileWatchChangeType type, string path)
    {
      _eventMoment = DateTime.Now;
      if (type == FileWatchChangeType.Renamed)
        throw new ArgumentException("The specified type indicates a Rename event. Rename events need the extra parameter \"oldPath\".");
      _path = path;
      _oldPath = path;
      _type = type;
      _fileSize = GetFileSize(_path);
    }

    /// <summary>
    /// Initializes a new instance of the FileWatchEvent class.
    /// </summary>
    /// <param name="type">Type of event.</param>
    /// <param name="path">Affected path.</param>
    /// <param name="oldPath">The old path to the affected path.</param>
    public FileWatchEvent(FileWatchChangeType type, string path, string oldPath)
    {
      _eventMoment = DateTime.Now;
      _path = path;
      _oldPath = oldPath;
      _type = type;
      _fileSize = GetFileSize(_path);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Determines whether the other FileWatchEvent is a duplicate to the current FileWatchEvent.
    /// </summary>
    /// <param name="other">The other FileWatchEvent, to compare with the current FileWatchEvent.</param>
    /// <returns>True, if the other FileWatchEvent is a duplicate. False, if not.</returns>
    public virtual bool IsDuplicate(FileWatchEvent other)
    {
      // This is not null, but is other null?
      if (other == null)
        return false;
      // Are the affected paths the same?
      if (_path != other._path)
        return false;
      if (_oldPath != other._oldPath)
        return false;
      // Figure out which event was created first.
      FileWatchEvent firstEvent;
      FileWatchEvent lastEvent;
      if (_eventMoment < other._eventMoment)
      {
        firstEvent = this;
        lastEvent = other;
      }
      else
      {
        firstEvent = other;
        lastEvent = this;
      }
      // Now we know that the paths are the same.
      // Lets check if the file is growing/shrinking? If true, make sure to delay both events again.
      if (firstEvent._fileSize != lastEvent._fileSize)
      {
        // If the filesize is not the same, delay the events again.
        firstEvent._delayed = lastEvent._delayed = false;
        // Update the filesize to avoid an endless delay.
        firstEvent._fileSize = lastEvent._fileSize = GetFileSize(firstEvent._path);
      }
      // Are the types the same,
      // OR could the last event be a Changed-event caused by a the first event, which is a Created-event?
      if (firstEvent._type == lastEvent._type
          || (firstEvent._type == FileWatchChangeType.Created && lastEvent._type == FileWatchChangeType.Changed))
      {
        // The events are a duplicate.
        return true;
      }
      // Both events are different.
      return false;
    }

    /// <summary>
    /// Returns a string representation of the current FileWatchEvent.
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
       return String.Format("{0} - {1}", _type,
         (_type != FileWatchChangeType.Renamed ? _path : _oldPath + " -> " + _path));
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Gets the filesize of the specified path.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    private long GetFileSize(string path)
    {
      long fileSize;
      try
      {
        if ((File.GetAttributes(path) & FileAttributes.Directory) == FileAttributes.Directory)
          fileSize = 0;
        else
          fileSize = new FileInfo(path).Length;      
      }
      // Catch all exceptions.
      catch (Exception)
      {
        return 0;
      }
      return fileSize;
    }

    #endregion

  }
}
