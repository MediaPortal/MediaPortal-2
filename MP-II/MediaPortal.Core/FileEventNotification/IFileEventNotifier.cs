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


namespace MediaPortal.FileEventNotification
{

  /// <summary>
  /// IFileEventNotifier is a service which provides an interfaces to a more advanced <see cref="System.IO.FileSystemWatcher"/>.
  /// </summary>
  public interface IFileEventNotifier
  {

    /// <summary>
    /// Subscribes the specified instance of FileWatchInfo.
    /// The return value is needed to unsubscribe.
    /// </summary>
    /// <exception cref="NotSupportedDriveTypeException"> </exception>
    /// <exception cref="System.NullReferenceException"></exception>
    /// <param name="fileWatchInfo">The FileWatchInfo to subscribe.</param>
    /// <returns>The subscribed item.</returns>
    FileWatchInfo Subscribe(FileWatchInfo fileWatchInfo);

    /// <summary>
    /// Unsubscribes the specified subscribed instance of FileWatchInfo.
    /// The instance of FileWatchInfo must be a previously subscribed instance in order to be accepted,
    /// otherwise an InvalidFileWatchInfoException is thrown.
    /// Returns if the specified FileWatchInfo was found and stopped. 
    /// </summary>
    /// <exception cref="InvalidFileWatchInfoException">
    /// Is thrown when the specified instance of FileWatchInfo has never been subscribed to the service.
    /// </exception>
    /// <param name="fileWatchInfo">The subscribed FileWatchInfo to unsubscribe.</param>
    /// <returns>
    /// True if the subscription of the specified FileWatchInfo is stopped.
    /// False if otherwise.
    /// </returns>
    bool Unsubscribe(FileWatchInfo fileWatchInfo);

  }
}
