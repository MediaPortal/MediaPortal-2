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

using System;

namespace MediaPortal.Common.MediaManagement.DefaultItemAspects
{
  /// <summary>
  /// Contains the metadata specification of the "Series" media item aspect which is assigned to series media items (i.e. videos, recordings).
  /// </summary>
  public static class SeriesAspect
  {
    /// <summary>
    /// Media item aspect id of the series aspect.
    /// </summary>
    public static readonly Guid ASPECT_ID = new Guid("F2ADB6BA-C536-42B3-8F3C-817DA5C04D71");

    /// <summary>
    /// Series name.
    /// </summary>
    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_SERIESNAME =
        MediaItemAspectMetadata.CreateStringAttributeSpecification("SeriesName", 50, Cardinality.Inline, false);

    /// <summary>
    /// Contains the season number.
    /// </summary>
    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_SEASONNUMBER =
        MediaItemAspectMetadata.CreateAttributeSpecification("SeasonNumber", typeof(int), Cardinality.Inline, false);

    /// <summary>
    /// Contains the episode number(s). If a file contains multiple episodes, all episode numbers are added separately.
    /// </summary>
    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_EPISODENUMBER =
        MediaItemAspectMetadata.CreateAttributeSpecification("EpisodeNumber", typeof(int), Cardinality.ManyToMany, false);

    /// <summary>
    /// Episode name. We store only the first episode name (or combined name) if the file contains multiple episodes.
    /// </summary>
    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_EPISODENAME =
        MediaItemAspectMetadata.CreateStringAttributeSpecification("EpisodeName", 100, Cardinality.Inline, false);

    public static readonly MediaItemAspectMetadata Metadata = new MediaItemAspectMetadata(
        ASPECT_ID, "SeriesItem", new[] {
            ATTR_SERIESNAME,
            ATTR_SEASONNUMBER,
            ATTR_EPISODENUMBER,
            ATTR_EPISODENAME,
        });
  }
}
