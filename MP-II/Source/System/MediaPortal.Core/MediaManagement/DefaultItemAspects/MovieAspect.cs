#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
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

using System;

namespace MediaPortal.Core.MediaManagement.DefaultItemAspects
{
  /// <summary>
  /// Contains the metadata specification of the "Music" media item aspect which is assigned to all song items.
  /// </summary>
  public static class MovieAspect
  {
    /// <summary>
    /// Media item aspect id of the movie aspect.
    /// </summary>
    public static Guid ASPECT_ID = new Guid("8F8B7A4F-767C-4180-B58E-7C8999C52067");

    public static MediaItemAspectMetadata.AttributeSpecification ATTR_GENRE =
        MediaItemAspectMetadata.CreateAttributeSpecification("Genre", typeof(string), Cardinality.Inline);
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_DURATION =
        MediaItemAspectMetadata.CreateAttributeSpecification("Duration", typeof(long), Cardinality.Inline);
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_DIRECTOR =
        MediaItemAspectMetadata.CreateAttributeSpecification("Director", typeof(string), Cardinality.Inline);
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_AUDIOSTREAMCOUNT =
        MediaItemAspectMetadata.CreateAttributeSpecification("AudioStreamCount", typeof(int), Cardinality.Inline);
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_AUDIOENCODING =
        MediaItemAspectMetadata.CreateAttributeSpecification("AudioEncoding", typeof(string), Cardinality.Inline);
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_AUDIOBITRATE =
        MediaItemAspectMetadata.CreateAttributeSpecification("AudioBitRate", typeof(long), Cardinality.Inline);
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_VIDEOENCODING =
        MediaItemAspectMetadata.CreateAttributeSpecification("VideoEncoding", typeof(string), Cardinality.Inline);
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_VIDEOBITRATE =
        MediaItemAspectMetadata.CreateAttributeSpecification("VideoBitRate", typeof(long), Cardinality.Inline);
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_WIDTH =
        MediaItemAspectMetadata.CreateAttributeSpecification("Width", typeof(int), Cardinality.Inline);
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_HEIGHT =
        MediaItemAspectMetadata.CreateAttributeSpecification("Height", typeof(int), Cardinality.Inline);
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_ASPECTRATIO =
        MediaItemAspectMetadata.CreateAttributeSpecification("AspectRatio", typeof(float), Cardinality.Inline);
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_FPS =
        MediaItemAspectMetadata.CreateAttributeSpecification("FPS", typeof(int), Cardinality.Inline);
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_ACTORS =
        MediaItemAspectMetadata.CreateAttributeSpecification("Actors", typeof(string), Cardinality.ManyToMany);
    public static MediaItemAspectMetadata.AttributeSpecification ATTR_ISDVD =
        MediaItemAspectMetadata.CreateAttributeSpecification("IsDVD", typeof(bool), Cardinality.Inline);

    public static MediaItemAspectMetadata Metadata = new MediaItemAspectMetadata(
        // TODO: Localize name
        ASPECT_ID, "MovieItem", new[] {
            ATTR_GENRE,
            ATTR_DURATION,
            ATTR_DIRECTOR,
            ATTR_AUDIOSTREAMCOUNT,
            ATTR_AUDIOENCODING,
            ATTR_AUDIOBITRATE,
            ATTR_VIDEOENCODING,
            ATTR_VIDEOBITRATE,
            ATTR_WIDTH,
            ATTR_HEIGHT,
            ATTR_ASPECTRATIO,
            ATTR_FPS,
            ATTR_ACTORS,
            ATTR_ISDVD,
        });
  }
}
