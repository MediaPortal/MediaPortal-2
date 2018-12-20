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
  public class AlbumTrackStub
  {
    #region Information on internet databases

    /// <summary>
    /// ID of this recording at MusicBrainz
    /// </summary>
    /// <example>"bbb06db2-9c83-4f31-a596-63f7c712cead"</example>
    public string MusicBrainzId { get; set; }

    /// <summary>
    /// ID of this track at TheAudioDB
    /// </summary>
    /// <example>675</example>
    public long? AudioDbId { get; set; }

    /// <summary>
    /// IRSC of this recording at MusicBrainz
    /// </summary>
    /// <example>"USRC17607839"</example>
    public string Isrc { get; set; }

    #endregion

    #region Title information

    /// <summary>
    /// Title of the track as it should be displayed
    /// </summary>
    /// <example>"The Track Title"</example>
    public string Title { get; set; }

    #endregion

    #region Making-of information

    /// <summary>
    /// Artists on this track
    /// </summary>
    public HashSet<string> Artists { get; set; }

    #endregion

    #region Content information

    /// <summary>
    /// Number of the Track
    /// </summary>
    public int? TrackNumber { get; set; }

    /// <summary>
    /// Duration of the Track in minutes
    /// </summary>
    public TimeSpan? Duration { get; set; }

    #endregion

    #region Media file information

    /// <summary>
    /// Collection of stream details in the given audio file
    /// </summary>
    public HashSet<StreamDetailsStub> FileInfo { get; set; }

    #endregion
  }
}
