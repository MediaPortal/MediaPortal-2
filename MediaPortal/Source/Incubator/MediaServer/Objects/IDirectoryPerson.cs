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

using System.Collections.Generic;

namespace MediaPortal.Plugins.MediaServer.Objects
{
  /// <summary>
  /// A ‘person’ instance represents an unordered collection of ‘objects’ that “belong” to the people, in a loose sense.
  /// </summary>
  public interface IDirectoryPerson : IDirectoryContainer
  {
    /// <summary>
    /// A language of the resource.
    /// </summary>
    [DirectoryProperty("dc:language", Required = false)]
    IList<string> Language { get; set; }
  }

  /// <summary>
  /// A ‘musicArtist’ instance is a ‘person’ which should be interpreted as a music artist. A ‘musicArtist’ container can contain objects of class ‘musicAlbum’, ‘musicTrack’ or ‘musicVideoClip’.
  /// </summary>
  public interface IDirectoryMusicArtist : IDirectoryPerson
  {
  }
}