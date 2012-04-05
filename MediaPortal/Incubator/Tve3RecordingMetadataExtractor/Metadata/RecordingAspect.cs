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
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;

namespace MediaPortal.Extensions.MetadataExtractors.Aspects
{
  /// <summary>
  /// Contains the metadata specification of the "Recording" media item aspect which is assigned to all recording media items.
  /// Recordings here are meant as TV video recordings, so common video related metadata are available in <seealso cref="VideoAspect"/>.
  /// </summary>
  public class RecordingAspect
  {
    /// <summary>
    /// Media item aspect id of the recording aspect.
    /// </summary>
    public static readonly Guid ASPECT_ID = new Guid("C389F655-ED60-4271-91EA-EC589BD815C6");

    /// <summary>
    /// Channel name where the program was recorded.
    /// </summary>
    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_CHANNEL =
        MediaItemAspectMetadata.CreateStringAttributeSpecification("Channel", 50, Cardinality.Inline, true);

    /// <summary>
    /// Contains the recording start date and time.
    /// </summary>
    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_STARTTIME =
        MediaItemAspectMetadata.CreateAttributeSpecification("StartTime", typeof(DateTime), Cardinality.Inline, false);

    /// <summary>
    /// Contains the recording start date and time.
    /// </summary>
    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_ENDTIME =
        MediaItemAspectMetadata.CreateAttributeSpecification("EndTime", typeof(DateTime), Cardinality.Inline, false);

    public static readonly MediaItemAspectMetadata Metadata = new MediaItemAspectMetadata(
        ASPECT_ID, "RecordingItem", new[] {
            ATTR_CHANNEL,
            ATTR_STARTTIME,
            ATTR_ENDTIME
        });
  }
}
