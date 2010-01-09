#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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

namespace MediaPortal.Core.FileEventNotification
{

  /// <summary>
  /// Service to register file and directory change listeners. This service works similar to
  /// the <see cref="System.IO.FileSystemWatcher"/> service, but has more advanced functions.
  /// </summary>
  public interface IFileEventNotifier
  {
    /// <summary>
    /// Subscribes a new instance of <see cref="FileWatchInfo"/>.
    /// The return value is needed to unsubscribe from the service.
    /// </summary>
    /// <exception cref="NotSupportedDriveTypeException"> </exception>
    /// <exception cref="System.NullReferenceException"></exception>
    /// <param name="path">The path to watch. Must end with '\' for directories.</param>
    /// <param name="includeSubDirectories">Specifies whether to also report changes in subdirectories.</param>
    /// <param name="eventHandler">Reference to the method handling all events.</param>
    /// <returns>The subscribed item.</returns>
    FileWatchInfo Subscribe(string path, bool includeSubDirectories, FileEventHandler eventHandler);

    /// <summary>
    /// Subscribes a new instance of <see cref="FileWatchInfo"/>.
    /// The return value is needed to unsubscribe from the service.
    /// </summary>
    /// <exception cref="NotSupportedDriveTypeException"> </exception>
    /// <exception cref="System.NullReferenceException"></exception>
    /// <param name="path">The path to watch. Must end with '\' for directories.</param>
    /// <param name="includeSubDirectories">Specifies whether to also report changes in subdirectories.</param>
    /// <param name="eventHandler">Reference to the method handling all events.</param>
    /// <param name="filter">Filter strings.</param>
    /// <returns>The subscribed item.</returns>
    FileWatchInfo Subscribe(string path, bool includeSubDirectories, FileEventHandler eventHandler,
        IEnumerable<string> filter);

    /// <summary>
    /// Subscribes a new instance of <see cref="FileWatchInfo"/>.
    /// The return value is needed to unsubscribe from the service.
    /// </summary>
    /// <exception cref="NotSupportedDriveTypeException"> </exception>
    /// <exception cref="System.NullReferenceException"></exception>
    /// <param name="path">The path to watch. Must end with '\' for directories.</param>
    /// <param name="includeSubDirectories">Specifies whether to also report changes in subdirectories.</param>
    /// <param name="eventHandler">Reference to the method handling all events.</param>
    /// <param name="filter">Filter strings.</param>
    /// <param name="changeTypes">Changetypes to report events for.</param>
    /// <returns>The subscribed item.</returns>
    FileWatchInfo Subscribe(string path, bool includeSubDirectories, FileEventHandler eventHandler,
        IEnumerable<string> filter, IEnumerable<FileWatchChangeType> changeTypes);

    /// <summary>
    /// Unsubscribes the specified subscribed instance of <see cref="FileWatchInfo"/>.
    /// The instance of <see cref="FileWatchInfo"/> must be an instance returned by <see cref="Subscribe"/> in order to be accepted,
    /// otherwise an <see cref="InvalidFileWatchInfoException"/> is thrown.
    /// Returns if the specified <see cref="FileWatchInfo"/> was found and stopped. 
    /// </summary>
    /// <exception cref="InvalidFileWatchInfoException">
    /// Is thrown when the specified instance of <see cref="FileWatchInfo"/> has never been subscribed to the service.
    /// </exception>
    /// <param name="fileWatchInfo">The subscribed <see cref="FileWatchInfo"/> to unsubscribe.</param>
    /// <returns>
    /// True if the subscription of the specified <see cref="FileWatchInfo"/> is stopped.
    /// False if otherwise.
    /// </returns>
    bool Unsubscribe(FileWatchInfo fileWatchInfo);

  }
}
