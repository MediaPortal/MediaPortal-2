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
using MediaPortal.Common.MediaManagement;

namespace MediaPortal.UiComponents.Media.Views.RemovableMediaDrives
{
  /// <summary>
  /// Handler for a specific removable media which is currently present in a removable media drive.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This is a generic, abstract interface which is to be implemented for each potential type of media which can be inserted into a
  /// removable media drive.
  /// </para>
  /// <para>
  /// Instances of this interface will be created for a specific inserted media in a drive, i.e. will not change if the removable media in the
  /// drive is changed. If the media is changed, this instance will typically be disposed and a new instance matching the new media is created.
  /// So implementors don't implement a handler for removable media changes.
  /// </para>
  /// <para>
  /// The return values from <see cref="MediaItems"/> and <see cref="SubViewSpecifications"/> are typically mixed together by the
  /// UI element list generator with other root entries in a view, so implementors should either return one element in <see cref="MediaItems"/>
  /// or one element in <see cref="SubViewSpecifications"/>. Multiple items on a media disc should be clustered in a single
  /// <see cref="ViewSpecification"/>, which should be returned in the <see cref="SubViewSpecifications"/>.
  /// Only in special cases, multiple items can be returned by <see cref="MediaItems"/> or by <see cref="SubViewSpecifications"/>.
  /// For example if a hybrid disk is handled where both audio and video contents are contained.
  /// </para>
  /// <para>
  /// That are only suggestions; it's up to the implementor how items are returned.
  /// </para>
  /// </remarks>
  public interface IRemovableDriveHandler
  {
    /// <summary>
    /// Returns a sensible volume label for the current media in the removable media drive.
    /// </summary>
    string VolumeLabel { get; }

    /// <summary>
    /// Gets one or more root media item instances for this drive handler as described in the class docs above.
    /// </summary>
    IList<MediaItem> MediaItems { get; }

    /// <summary>
    /// Returns one or more root view specification instances for this drive handler as described in the class docs above.
    /// </summary>
    IList<ViewSpecification> SubViewSpecifications { get; }

    /// <summary>
    /// Returns all media items of the root and recursively of all view specifications of this drive.
    /// </summary>
    /// <returns>Enumeration of media items.</returns>
    IEnumerable<MediaItem> GetAllMediaItems();
  }
}