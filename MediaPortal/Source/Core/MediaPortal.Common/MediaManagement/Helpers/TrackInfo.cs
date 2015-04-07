#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using System.Globalization;
using System.Linq;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;

namespace MediaPortal.Common.MediaManagement.Helpers
{
  /// <summary>
  /// <see cref="TrackInfo"/> contains information about a track. It's used as an interface structure for external 
  /// online data scrapers to fill in metadata.
  /// </summary>
  public class TrackInfo
  {
    public bool Matched { get; set; }

    public string MusicBrainzId { get; set; }

    public string Title { get; set; }
    public string ArtistId { get; set; }
    public string ArtistName { get; set; }
    public string AlbumId { get; set; }
    public string AlbumName { get; set; }
    public string Genre { get; set; }
    public int Year { get; set; }
    public int TrackNum { get; set; }
    public string AlbumArtistId { get; set; }
    public string AlbumArtistName { get; set; }

      /// <summary>
    /// Copies the contained track information into MediaItemAspect.
    /// </summary>
    /// <param name="aspectData">Dictionary with extracted aspects.</param>
    public bool SetMetadata(IDictionary<Guid, IList<MediaItemAspect>> aspectData)
    {
      MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_TITLE, Title);
      if (!string.IsNullOrEmpty(MusicBrainzId)) MediaItemAspect.SetExternalAttribute(aspectData, ExternalIdentifierAspect.Source.MUSICBRAINZ, ExternalIdentifierAspect.TYPE_TRACK, MusicBrainzId);
      if (!string.IsNullOrEmpty(ArtistName)) MediaItemAspect.SetAttribute(aspectData, AudioAspect.ATTR_ARTISTS, new string[] { ArtistName});

      if (Year > 0) MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_RECORDINGTIME, new DateTime(Year, 1, 1));

      return true;
    }
  }
}
