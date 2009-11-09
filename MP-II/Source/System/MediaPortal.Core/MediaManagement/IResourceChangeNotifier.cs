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

namespace MediaPortal.Core.MediaManagement
{
  /// <summary>
  /// Delegate for path/media item change events.
  /// </summary>
  /// <param name="resourceAccessor">The resource accessor where the change occured.</param>
  /// <param name="oldPath">If the <paramref name="changeType"/> is <see cref="MediaSourceChangeType.Renamed"/>,
  /// this parameter contains the old file path.</param>
  /// <param name="changeType">Type of the change.</param>
  public delegate void PathChangeDelegate(
      IResourceAccessor resourceAccessor, string oldPath, MediaSourceChangeType changeType);

  /// <summary>
  /// Additional interface which can be implemented by resource accessors if they are capable of detecting
  /// resource changes.
  /// </summary>
  public interface IResourceChangeNotifier
  {
    /// <summary>
    /// Registers a delegate function to be called when this resource is changed in one of the ways specified by
    /// <paramref name="changeTypes"/>.
    /// </summary>
    /// <remarks>
    /// The registration of this change tracker will be stored in the underlaying media provider and thus will also
    /// work if this resource accessor is disposed. It isn't persistent over system shutdowns and so has to be re-done
    /// every time the underlaying provider is re-created, i.e. typically on system start and when a provider plugin is
    /// enabled during the runtime.
    /// <br/>
    /// If the same <paramref name="changeDelegate"/> is registered for the same resource accessor again,
    /// the old registration will be replaced by the new.
    /// The same <paramref name="changeDelegate"/> can be registered multiple times on different resource accessors.
    /// </remarks>
    /// <param name="changeDelegate">Delegate function to be called when a change of the resource denoted by this resource
    /// accessor is detected.</param>
    /// <param name="fileNameFilters">Enumeration of file name patterns in the form "*.avi". "*" can be
    /// used as wildcard for any number of arbitrary chars, "?" can be used for a single char.</param>
    /// <param name="changeTypes">Change types to track.</param>
    void RegisterChangeTracker(PathChangeDelegate changeDelegate, IEnumerable<string> fileNameFilters,
        IEnumerable<MediaSourceChangeType> changeTypes);

    /// <summary>
    /// Removes the registration of the specified <paramref name="changeDelegate"/> at this resource accessor.
    /// </summary>
    /// <param name="changeDelegate">Delegate method to be removed.</param>
    void UnregisterChangeTracker(PathChangeDelegate changeDelegate);

    /// <summary>
    /// Removes all registrations of the specified <paramref name="changeDelegate"/> for all resources of the underlaying
    /// media provider.
    /// </summary>
    /// <param name="changeDelegate">Delegate method to remove all registrations for.</param>
    void UnregisterAll(PathChangeDelegate changeDelegate);
  }
}
