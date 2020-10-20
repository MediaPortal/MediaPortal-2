#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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

using System;

namespace MediaPortal.Plugins.WifiRemote.Messages.Playlist
{
  /// <summary>
  /// One item of a MP playlist
  /// </summary>
  public class PlaylistEntry
  {
    public string MediaType { get; set; }
    public string Id => FileId.ToString();
    public int MpMediaType { get; set; }
    public int MpProviderId { get; set; }

    /// <summary>
    /// Name of the file that will get displayed in the playlist
    /// </summary>
    public String Name { get; set; }
    /// <summary>
    /// Second Name of the file that will get displayed in the playlist (e.g. album)
    /// </summary>
    public String Name2 { get; set; }
    /// <summary>
    /// Album Artist of the file that will get displayed in the playlist
    /// </summary>
    public String AlbumArtist { get; set; }
    /// <summary>
    /// Full path to the file
    /// </summary>
    public String FileName { get; set; }
    /// <summary>
    /// Id of the file
    /// </summary>
    public String FileId { get; set; }
    /// <summary>
    /// Duration of the file
    /// </summary>
    public int Duration { get; set; }
    /// <summary>
    /// Indicates if the item has been played already
    /// </summary>
    public bool Played { get; set; }
  }
}
