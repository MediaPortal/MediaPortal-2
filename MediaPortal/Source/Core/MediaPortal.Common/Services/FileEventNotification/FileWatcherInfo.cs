#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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

using System.Collections.Generic;
using System.Text.RegularExpressions;
using MediaPortal.Common.FileEventNotification;

namespace MediaPortal.Common.Services.FileEventNotification
{

  /// <summary>
  /// Extends <see cref="FileWatchInfo"/> with an ID,
  /// this property is used by <see cref="FileEventNotifier"/>.
  /// </summary>
  internal sealed class FileWatcherInfo : FileWatchInfo
  {

    #region Variables

    /// <summary>
    /// Unique ID of the current <see cref="FileWatcherInfo"/>.
    /// </summary>
    private int _id;

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the unique ID.
    /// -1 indicates that the current <see cref="FileWatcherInfo"/> isn't subscribed to a <see cref="FileWatcher"/>.
    /// </summary>
    public int Id
    {
      get { return _id; }
      set { _id = value; }
    }

    #endregion

    #region Constructors

    public FileWatcherInfo(int id, string path, bool includeSubdirectories, FileEventHandler eventHandler,
        IEnumerable<string> filter, IEnumerable<FileWatchChangeType> changeTypes) :
        base(path, includeSubdirectories, eventHandler, filter, changeTypes)
    {
      _id = id;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Returns whether an event may be raised for the given path and changetype.
    /// </summary>
    /// <param name="args">
    /// Arguments to compare to the requirements set by
    /// the current <see cref="FileWatcherInfo"/> for raising events.
    /// </param>
    /// <returns></returns>
    public bool MayRaiseEventFor(IFileWatchEventArgs args)
    {
      // Don't raise an event if no FileEventHandler is specified.
      if (_eventHandler == null)
        return false;
      // Do changes in the specified path need to be reported by the current FileWatcherInfo?
      if (!IsWatchedDirectory(args.Path))
        return false;
      // Check if the specified changetype must be reported.
      if (!CompliesToChangeType(args.ChangeType))
        return false;
      // Event may be raised if the old path matches the filter.
      return CompliesToFilterStrings(args.OldPath)
             // Event may also be raised if the new path matches the filter.
             || (args.Path != args.OldPath && CompliesToFilterStrings(args.Path));
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Returns whether the directory of the specified path
    /// is a directory for which the current <see cref="FileWatcherInfo"/> needs to report changes.
    /// </summary>
    /// <param name="path">Path to compare to the specified watched path.</param>
    /// <returns></returns>
    private bool IsWatchedDirectory(string path)
    {
      var directoryName = System.IO.Path.GetDirectoryName(path).ToLowerInvariant() + @"\";
      return (string.Equals(directoryName, _path, System.StringComparison.InvariantCultureIgnoreCase)
              || (_includeSubdirectories && directoryName.StartsWith(_path, System.StringComparison.InvariantCultureIgnoreCase)));
    }

    /// <summary>
    /// Returns whether events may be raised for the given <see cref="FileWatchChangeType"/>.
    /// </summary>
    /// <param name="changeType"><see cref="FileWatchChangeType"/> to test.</param>
    /// <returns></returns>
    private bool CompliesToChangeType(FileWatchChangeType changeType)
    {
      return _changeTypes.Count == 0 || (_changeTypes.Contains(changeType));
    }

    /// <summary>
    /// Returns whether events may be raised for the given path.
    /// </summary>
    /// <param name="path">Path to compare to filter.</param>
    /// <returns></returns>
    private bool CompliesToFilterStrings(string path)
    {
      if (string.Equals(path, _path, System.StringComparison.InvariantCultureIgnoreCase) || _filter.Count == 0)
        return true;
      var filename = System.IO.Path.GetFileName(path);
      lock (_filter)
      {
        foreach (var filterString in _filter)
        {
          if (string.IsNullOrEmpty(filterString) || filterString == "*")
            return true;
          // Match the pattern to the filename.
          if (Regex.Match(filename, WildcardToRegex(filterString), RegexOptions.IgnoreCase).Success)
            return true;
        }
      }
      return false;
    }

    /// <summary>
    /// Returns the provided wildcard string as a regular expression.
    /// </summary>
    /// <param name="pattern"></param>
    /// <returns></returns>
    private static string WildcardToRegex(string pattern)
    {
      return "^" + Regex.Escape(pattern).
      Replace(@"\*", ".*").
      Replace(@"\?", ".") + "$";
    }

    #endregion

    #region IEquatable<FileWatchInfo> Members

    /// <summary>
    /// Returns whether the current <see cref="FileWatcherInfo"/> is equal
    /// to another object of type <see cref="FileWatchInfo"/>.
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public override bool Equals(FileWatchInfo other)
    {
      if (!(other is FileWatcherInfo))
        return false;
      var otherFileWatcherInfo = (FileWatcherInfo) other;
      if (_id != -1 || otherFileWatcherInfo._id != -1)
        return (otherFileWatcherInfo._id == _id);
      // Else: No ID's are assigned, use base class' compare mechanism.
      return base.Equals(other);
    }

    #endregion

  }
}
