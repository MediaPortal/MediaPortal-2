#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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

namespace MediaPortal.Plugins.MediaServer.Objects
{
  /// <summary>
  /// A ‘storageFolder’ instance represents a collection of objects stored on some storage medium. The storage folder may be writable or not, indicating whether new items can be created as children of the folder or whether existing child items can be removed. If the parent storage container is not writable, then the ‘storageFolder’ itself cannot be writable. A ‘storageFolder’ may contain other objects, except a ‘storageSystem’ or a ‘storageVolume’. A ‘storageFolder’ may only be a child of the root container or another storage container.
  /// </summary>
  public interface IDirectoryStorageFolder : IDirectoryContainer
  {
    /// <summary>
    /// Combined space, in bytes, used by all the objects held in the storage represented by the container. Value –1 is reserved to indicate that the space is ‘unknown’.
    /// </summary>
    [DirectoryProperty("upnp:storageUsed")]
    long StorageUsed { get; set; }
  }
}