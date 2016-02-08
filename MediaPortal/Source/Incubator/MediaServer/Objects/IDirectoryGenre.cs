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
  /// A ‘genre’ instance represents an unordered collection of ‘objects’ that “belong” to the genre, in a loose sense.
  /// </summary>
  public interface IDirectoryGenre : IDirectoryContainer
  {
    /// <summary>
    /// A few lines of description of the content item (longer than DublinCore’s description element
    /// </summary>
    [DirectoryProperty("upnp:longDescription", Required = false)]
    string LongDescription { get; set; }

    /// <summary>
    /// An account of the resource.
    /// </summary>
    [DirectoryProperty("dc:description", Required = false)]
    string Description { get; set; }
  }

  /// <summary>
  /// A ‘musicGenre’ instance is a ‘genre’ which should be interpreted as a “style of music”. A ‘musicGenre’ container can contain objects of class ‘musicArtist, ‘musicAlbum’, ‘audioItem’ or “sub”-music genres of the same class (e.g. ‘Rock’ contains ‘Alternative Rock’).
  /// </summary>
  public interface IDirectoryMusicGenre : IDirectoryGenre
  {
  }

  /// <summary>
  /// A ‘movieGenre’ instance is a ‘genre’ which should be interpreted as a “style of movies”. A ‘movieGenre’ container can contain objects of class ‘people’, ‘videoItem’ or “sub”-movie genres of the same class (e.g. ‘Western’ contains ‘Spaghetti Western’).
  /// </summary>
  public interface IDirectoryMovieGenre : IDirectoryGenre
  {
  }
}