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
using System.Collections.Generic;
using System.IO;
using MediaPortal.Core.FileEventNotification;
using MediaPortal.Utilities;

namespace MediaPortal.Core.FileEventNotification
{

  /// <summary>
  /// Represents all data regarding a watched file,
  /// <see cref="FileWatchInfo"/> is needed to subscribe and unsubscribe a watch
  /// to a <see cref="IFileEventNotifier"/>.
  /// </summary>
  public class FileWatchInfo : IEquatable<FileWatchInfo>
  {

    #region Constants

    /// <summary>
    /// The wildcard for all characters.
    /// </summary>
    protected const char CharWildcard = '*';
    /// <summary>
    /// The wildcard for numeric characters.
    /// </summary>
    protected const char NumWildcard = '?';

    #endregion

    #region Variables

    /// <summary>
    /// The path to watch.
    /// </summary>
    protected string _path;
    /// <summary>
    /// The FileWatchChangeTypes to raise events for.
    /// </summary>
    protected ICollection<FileWatchChangeType> _changeTypes;
    /// <summary>
    /// The strings to filter events on, using the wildcards.
    /// </summary>
    protected ICollection<string> _filter;
    /// <summary>
    /// Indicates whether subdirectories should be watched too.
    /// </summary>
    protected bool _includeSubdirectories;
    /// <summary>
    /// Reference to the method handling events.
    /// </summary>
    protected FileEventHandler _eventHandler;

    #endregion

    #region Properties

    /// <summary>
    /// Gets the path to the directory to watch, in lower casing.
    /// </summary>
    public string Path
    {
      get { return _path; }
    }

    /// <summary>
    /// Gets the FileWatchChangeTypes to filter events on.
    /// </summary>
    public ICollection<FileWatchChangeType> ChangeTypes
    {
      get { return _changeTypes; }
    }
    
    /// <summary>
    /// Gets whether subdirectories are watched too.
    /// </summary>
    public bool IncludeSubdirectories
    {
      get { return _includeSubdirectories; }
    }

    /// <summary>
    /// Gets or sets the reference to the method handling all events.
    /// </summary>
    public FileEventHandler EventHandler
    {
      get { return _eventHandler; }
      set { _eventHandler = value; }
    }

    #endregion

    #region Constructors

    /// <summary>
    /// Default constructor, doesn't initialize anything.
    /// </summary>
    protected FileWatchInfo()
    {
      
    }

    /// <summary>
    /// Copies all variables specified in <paramref cref="fileWatchInfo"/> to the new instance.
    /// </summary>
    /// <param name="fileWatchInfo"></param>
    protected FileWatchInfo(FileWatchInfo fileWatchInfo)
    {
      _changeTypes = new List<FileWatchChangeType>(fileWatchInfo.ChangeTypes);
      _filter = new List<string>(fileWatchInfo._filter);
      _eventHandler = fileWatchInfo.EventHandler;
      _path = fileWatchInfo.Path.ToLowerInvariant();
      _includeSubdirectories = fileWatchInfo.IncludeSubdirectories;
    }

    /// <summary>
    /// Initializes a new instance of the FileWatchInfo class,
    /// which can be used to subscribe to watch the specified path.
    /// Events will be filtered by the given strings and the given changetypes.
    /// </summary>
    /// <param name="path">The path to watch. Must end with '\' for directories.</param>
    /// <param name="includeSubDirectories">Specifies whether to also report changes in subdirectories.</param>
    /// <param name="eventHandler">Reference to the method handling all events.</param>
    /// <param name="filter">Filter strings.</param>
    /// <param name="changeTypes">Changetypes to report events for.</param>
    protected FileWatchInfo(string path, bool includeSubDirectories, FileEventHandler eventHandler,
        IEnumerable<string> filter, IEnumerable<FileWatchChangeType> changeTypes)
    {
      if (path == null)
        throw new ArgumentNullException("path", "The specified path is a null reference.");
      if (filter == null)
        filter = new List<string>();
      if (changeTypes == null)
        throw new ArgumentNullException("changeTypes", "The specified changeTypes is a null reference.");
      _includeSubdirectories = includeSubDirectories;
      _eventHandler = eventHandler;
      _changeTypes = new List<FileWatchChangeType>(changeTypes);
      _filter = new List<string>(filter);
      SetPath(path);        // Makes sure the path is set as a directory
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Adds a new string to filter events on,
    /// the filter strings are matched against the filename of the file causing the event.
    /// Filter strings contain of characters and wildcards ('*').
    /// A filter string like "*.mkv" will match all filenames ending with the .mkv extension.
    /// </summary>
    /// <remarks>
    /// The filterstrings behave the same as in <see cref="FileSystemWatcher.Filter"/>,
    /// see it's documentation for more information.
    /// 
    /// Filter strings matching all filenames:
    /// <ul>
    ///   <li>null</li>
    ///   <li>""</li>
    ///   <li>"*"</li>
    /// </ul>
    /// </remarks>
    public void AddFilterString(string filterString)
    {
      lock (_filter)
      {
        if (!_filter.Contains(filterString))
          _filter.Add(filterString);
      }
    }

    /// <summary>
    /// Removes a string from the filterstring collection.
    /// </summary>
    /// <param name="filterString"></param>
    /// <returns></returns>
    public bool RemoveFilterString(string filterString)
    {
      lock (_filter)
        return _filter.Remove(filterString);
    }

    /// <summary>
    /// Returns whether the specified string is used to filter events on.
    /// </summary>
    /// <param name="filterString"></param>
    /// <returns></returns>
    public bool IsFilterString(string filterString)
    {
      lock (_filter)
        return _filter.Contains(filterString);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Sets the directory from the given path to the _path variable.
    /// If the specified path links to a file, the file is set to the filter.
    /// </summary>
    /// <param name="path">Path to set.</param>
    private void SetPath(string path)
    {
      // The code on the next line locks up for unavailable network shares.
      //  bool isDirectory = ((File.GetAttributes(path) & FileAttributes.Directory) == FileAttributes.Directory);
      // So we'll have to guess if we're working with a file or directory.
      // This shouldn't be a problem if the user has read the documentation, which says:
      //  - A path pointing to a directory must always end with '\'
      //  - A path pointing to a file must never end with '\'
      bool isDirectory = (path.EndsWith(@"\")       // Files will never end with '\'
                          || !path.Contains(@"\")); // It's a root
      // We can't check for extensions, some files might not have one.
      if (!isDirectory)
      {
        // We expect it to be a file
        int index = path.LastIndexOf('\\');
        _filter.Clear();
        _filter.Add(path.Substring(index));
        path = path.Substring(0, index + 1);
      }
      else if (!path.EndsWith(@"\"))
      {
        path = path + @"\";
      }
      _path = path.ToLowerInvariant();
    }

    #endregion

    #region IEquatable<FileWatchInfo> Members

    /// <summary>
    /// Returns whether the current <see cref="FileWatchInfo"/> is equal
    /// to another object of type <see cref="FileWatchInfo"/>.
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public virtual bool Equals(FileWatchInfo other)
    {
      return _path == other._path
             && _includeSubdirectories == other._includeSubdirectories
             && _eventHandler == other._eventHandler
             && CollectionUtils.CompareObjectCollections(_filter, other._filter)
             && CollectionUtils.CompareCollections(_changeTypes, other._changeTypes,
                                                   (ct1, ct2) => ct1.ToString().CompareTo(ct2.ToString()));
    }

    #endregion

  }
}
