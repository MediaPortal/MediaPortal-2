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

using System.Collections.Generic;

namespace MediaPortal.Core.MediaManagement.MediaProviders
{
  /// <summary>
  /// Delegate for path/media item change events.
  /// </summary>
  /// <param name="mediaProvider">The media provider where the change occured.</param>
  /// <param name="oldPath">If the <paramref name="changeType"/> is <see cref="MediaSourceChangeType.Renamed"/>,
  /// this parameter contains the old file path.</param>
  /// <param name="path">The path which was changed.</param>
  /// <param name="changeType">Type of the change.</param>
  public delegate void PathChangeDelegate(
      IMediaProvider mediaProvider, string oldPath, string path, MediaSourceChangeType changeType);

  /// <summary>
  /// Additional interface which can be implemented by media providers if they are capable of detecting
  /// media source changes.
  /// </summary>
  public interface IMediaSourceChangeNotifier
  {
    /// <summary>
    /// Registers a delegate function to be called when the specified <paramref name="path"/> of this
    /// provider is changed in one of the ways specified by <paramref name="changeTypes"/>.
    /// </summary>
    /// <remarks>
    /// The registration of this change tracker isn't persistent and has to be re-done every time this
    /// provider is re-created, i.e. typically on system start and when a provider plugin is enabled during
    /// the runtime.
    /// <br/>
    /// If the same <paramref name="changeDelegate"/> is registered for the same <paramref name="path"/> again,
    /// the old registration will be replaced by the new.
    /// The same <paramref name="changeDelegate"/> can be registered multiple times with different
    /// <paramref name="path"/>s.
    /// </remarks>
    /// <param name="changeDelegate">Delegate function to be called when a change of the specified
    /// <paramref name="path"/> and <paramref name="changeTypes"/> is detected.</param>
    /// <param name="path">The path to track.</param>
    /// <param name="fileNameFilters">Enumeration of file name patterns in the form "*.avi". "*" can be
    /// used as wildcard for any number of arbitrary chars, "?" can be used </param>
    /// <param name="changeTypes">Change types to track.</param>
    void RegisterChangeTracker(PathChangeDelegate changeDelegate,
        string path, IEnumerable<string> fileNameFilters, IEnumerable<MediaSourceChangeType> changeTypes);

    /// <summary>
    /// Removes the registration of the specified <paramref name="changeDelegate"/> and <paramref name="path"/>
    /// combination.
    /// </summary>
    /// <param name="changeDelegate">Delegate method to be removed.</param>
    /// <param name="path">Path which was used for the registration to be removed.</param>
    void UnregisterChangeTracker(PathChangeDelegate changeDelegate, string path);

    /// <summary>
    /// Removes all registrations of the specified <paramref name="changeDelegate"/> for all paths.
    /// </summary>
    /// <param name="changeDelegate">Delegate method to remove all registrations for.</param>
    void UnregisterAll(PathChangeDelegate changeDelegate);
  }
}
