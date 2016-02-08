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
  /// A ‘storageVolume’ instance represents all, or a partition of, some physical storage unit of a single type (as indicated by the ‘storageMedium’ property). The storage volume may be writable or not, indicating whether new items can be created as children of the volume. A storageVolume may contain other object, except a ‘storageSystem’ or another ‘storageVolume’. A ‘storageVolume’ may only be a child of the root container or a ‘storageSystem’ container. Examples of ‘storageVolume’ instances are
  ///   a Hard Disk Drive
  ///   a partition on a Hard Disk Drive
  ///   a CD-Audio disc
  ///   a Flash memory card
  /// </summary>
  public interface IDirectoryStorageVolume : IDirectoryContainer
  {
    /// <summary>
    /// Total capacity, in bytes, of the storage represented by the container. Value –1 is reserved to indicate that the capacity is ‘unknown’.
    /// </summary>
    [DirectoryProperty("upnp:storageTotal")]
    long StorageTotal { get; set; }

    /// <summary>
    /// Combined space, in bytes, used by all the objects held in the storage represented by the container. Value –1 is reserved to indicate that the space is ‘unknown’.
    /// </summary>
    [DirectoryProperty("upnp:storageUsed")]
    long StorageUsed { get; set; }

    /// <summary>
    /// Total free capacity, in bytes, of the storage represented by the containe. Value –1 is reserved to indicate that the capacity is ‘unknown’.
    /// </summary>
    [DirectoryProperty("upnp:storageFree")]
    long StorageFree { get; set; }

    /// <summary>
    /// Indicates the type of storage medium used for the content. Potentially useful for user-interface purposes.
    /// </summary>
    [DirectoryProperty("upnp:storageMedium")]
    string StorageMedium { get; set; }
  }
}