#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using MediaPortal.Core.FileEventNotification;

namespace MediaPortal.Core.Services.FileEventNotification
{

  /// <summary>
  /// Provides data for the FileWatcher events.
  /// </summary>
  public class FileWatchEventArgs : EventArgs, IFileWatchEventArgs
  {

    #region Variables

    /// <summary>
    /// The path that is being watched.
    /// </summary>
    private string _path;
    /// <summary>
    /// The old string representing the path that is being watched.
    /// </summary>
    private string _oldPath;
    /// <summary>
    /// The type of change that occured.
    /// </summary>
    private FileWatchChangeType _changeType;

    #endregion

    #region Constructors

    public FileWatchEventArgs(FileWatchEvent fileWatchEvent)
      : this(fileWatchEvent.ChangeType, fileWatchEvent.Path, fileWatchEvent.OldPath)
    {
    }

    public FileWatchEventArgs(FileWatchChangeType changeType, string path)
      : this(changeType, path, path)
    {
    }

    public FileWatchEventArgs(FileWatchChangeType changeType, string path, string oldPath)
    {
      _path = path;
      _oldPath = oldPath;
      _changeType = changeType;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Returns a string represenation for the current FileWatchEventArgs.
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
      return String.Format("[{0}] {1}", _changeType,
        (_changeType != FileWatchChangeType.Renamed ? _path : _oldPath + " -> " + _path));
    }

    #endregion

    #region IFileWatchEventArgs Members

    /// <summary>
    /// Gets the path that is being watched.
    /// </summary>
    public string Path
    {
      get { return _path; }
      internal set { _path = value; }
    }

    /// <summary>
    /// Gets the old string representing the path that is being watched.
    /// </summary>
    public string OldPath
    {
      get { return _oldPath; }
      internal set { _oldPath = value; }
    }

    /// <summary>
    /// Gets the type of change that occured.
    /// </summary>
    public FileWatchChangeType ChangeType
    {
      get { return _changeType; }
      internal set { _changeType = value; }
    }

    #endregion

  }
}
