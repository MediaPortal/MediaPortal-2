#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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

namespace MediaPortal.Core.MediaManagement
{
  /// <summary>
  /// Changes that might occur to a media source item or path.
  /// </summary>
  public enum MediaSourceChangeType
  {
    /// <summary>
    /// No changes.
    /// </summary>
    None,

    /// <summary>
    /// The creation of a file or folder.
    /// </summary>
    Created,

    /// <summary>
    /// The deletion of a file or folder.
    /// </summary>
    Deleted,

    /// <summary>
    /// The change of a file or folder. The types of changes include: changes to size, attributes,
    /// security settings, last write, and last access time.
    /// </summary>
    Changed,

    /// <summary>
    /// The renaming of a file or folder.
    /// </summary>
    Renamed,

    /// <summary>
    /// The creation, deletion, change, or renaming of a file or folder.
    /// </summary>
    All,

    /// <summary>
    /// The deletion of the parent directory.
    /// </summary>
    DirectoryDeleted,
  }
}
