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

using System;

namespace MediaPortal.Common.MediaManagement.DefaultItemAspects
{
  /// <summary>
  /// Contains the metadata specification of the "Audio" media item aspect which is assigned to all audio media items.
  /// </summary>
  public static class AudioAspect
  {
    /// <summary>
    /// Media item aspect id of the audio aspect.
    /// </summary>
    public static readonly Guid ASPECT_ID = new Guid("739AC022-2CF5-4921-B4EF-108BA28C62E5");

    /// <summary>
    /// Track name.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_TRACKNAME =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("TrackName", 100, Cardinality.Inline, true);

    /// <summary>
    /// Enumeration of artist names.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_ARTISTS =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("Artists", 100, Cardinality.ManyToMany, true);

    /// <summary>
    /// Album name.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_ALBUM =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("Album", 100, Cardinality.Inline, true);

    /// <summary>
    /// If set to <c>true</c>, the track is part of a compilation of music from various artists.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_COMPILATION =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("IsCompilation", typeof(bool), Cardinality.Inline, true);

    /// <summary>
    /// Duration in seconds.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_DURATION =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("Duration", typeof(long), Cardinality.Inline, false);

    /// <summary>
    /// Track lyrics.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_LYRICS =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("Lyrics", 5000, Cardinality.Inline, false);

    /// <summary>
    /// Set to <c>true</c> if this track item represents a CD track.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_ISCD =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("IsCD", typeof(bool), Cardinality.Inline, false);

    /// <summary>
    /// Track number.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_TRACK =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("Track", typeof(int), Cardinality.Inline, false);

    /// <summary>
    /// Number of tracks on the CD.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_NUMTRACKS =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("NumTracks", typeof(int), Cardinality.Inline, false);

    /// <summary>
    /// Enumeration of album artist name strings.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_ALBUMARTISTS =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("AlbumArtists", 100, Cardinality.ManyToMany, true);

    /// <summary>
    /// Enumeration of composer name strings.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_COMPOSERS =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("Composers", 100, Cardinality.ManyToMany, true);

    /// <summary>
    /// Encoding as string. TODO: Describe format.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_ENCODING =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("Encoding", 50, Cardinality.Inline, false);

    /// <summary>
    /// Bitrate in kbits/second.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_BITRATE =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("BitRate", typeof(int), Cardinality.Inline, false);

    /// <summary>
    /// Sample rate of the audio in kHz.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_SAMPLERATE =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("SampleRate", typeof(long), Cardinality.Inline, false);

    /// <summary>
    /// Number of audio channels.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_CHANNELS =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("Channels", typeof(int), Cardinality.Inline, false);

    /// <summary>
    /// ID of the disc in the collection.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_DISCID =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("DiscId", typeof(int), Cardinality.Inline, false);

    /// <summary>
    /// Number of discs in the collection.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_NUMDISCS =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("NumDiscs", typeof(int), Cardinality.Inline, false);

    /// <summary>
    /// Contains the overall rating of the track. Value ranges from 0 (very bad) to 10 (very good).
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_TOTAL_RATING =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("TotalRating", typeof(double), Cardinality.Inline, true);

    /// <summary>
    /// Contains the overall number ratings of the track.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_RATING_COUNT =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("RatingCount", typeof(int), Cardinality.Inline, true);

    public static readonly SingleMediaItemAspectMetadata Metadata = new SingleMediaItemAspectMetadata(
        // TODO: Localize name
        ASPECT_ID, "AudioItem", new[] {
            ATTR_TRACKNAME,
            ATTR_ARTISTS,
            ATTR_ALBUM,
            ATTR_COMPILATION,
            ATTR_DURATION,
            ATTR_LYRICS,
            ATTR_ISCD,
            ATTR_TRACK,
            ATTR_NUMTRACKS,
            ATTR_ALBUMARTISTS,
            ATTR_COMPOSERS,
            ATTR_ENCODING,
            ATTR_BITRATE,
            ATTR_CHANNELS,
            ATTR_SAMPLERATE,
            ATTR_DISCID,
            ATTR_NUMDISCS,
            ATTR_TOTAL_RATING,
            ATTR_RATING_COUNT
        });

    public static readonly Guid ROLE_TRACK = new Guid("10C134B1-4E35-4750-836D-76F3AB58D40A");
  }
}
