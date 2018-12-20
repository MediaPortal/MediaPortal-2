#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using System.Collections.Generic;

namespace MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Stubs
{
  /// <summary>
  /// This stub class is used to store information about an album
  /// </summary>
  public class AlbumStub
  {
    #region Information on internet databases

    /// <summary>
    /// ID of this release at MusicBrainz
    /// </summary>
    /// <example>"bbb06db2-9c83-4f31-a596-63f7c712cead"</example>
    public string MusicBrainzAlbumId { get; set; }

    /// <summary>
    /// ID of this release group at MusicBrainz
    /// </summary>
    /// <example>"d50603ae-887c-4a8f-8477-c541eb8f953a"</example>
    public string MusicBrainzReleaseGroupId { get; set; }

    /// <summary>
    /// ID of this album at TheAudioDB
    /// </summary>
    /// <example>675</example>
    public long? AudioDbId { get; set; }

    #endregion

    #region Title information

    /// <summary>
    /// Title of the album as it should be displayed
    /// </summary>
    /// <example>"The Album Title"</example>
    public string Title { get; set; }

    #endregion

    #region Making-of information

    /// <summary>
    /// Date of first release
    /// </summary>
    public DateTime? ReleaseDate { get; set; }

    /// <summary>
    /// Year of first release
    /// </summary>
    public DateTime? Year { get; set; }

    /// <summary>
    /// Record Labels
    /// </summary>
    public HashSet<string> Labels { get; set; }

    /// <summary>
    /// Artists on this album
    /// </summary>
    public HashSet<string> Artists { get; set; }

    #endregion

    #region Content information

    /// <summary>
    /// Genre(s) of the album
    /// </summary>
    public HashSet<string> Genres { get; set; }

    /// <summary>
    /// Tracks in this album
    /// </summary>
    public HashSet<AlbumTrackStub> Tracks { get; set; }

    #endregion

    #region Images

    public byte[] Thumb { get; set; }

    #endregion

    #region Ratings

    /// <summary>
    /// General rating of this album
    /// </summary>
    /// <example>8.7</example>
    public decimal? Rating { get; set; }

    /// <summary>
    /// A review of this album
    /// </summary>
    public string Review { get; set; }

    #endregion
  }
}
