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
using MediaPortal.Common.MediaManagement;

namespace MediaPortal.Extensions.MetadataExtractors.Aspects
{
  /// <summary>
  /// Contains the metadata specification of the "Recording" media item aspect which is assigned to all recording media items.
  /// Recordings here are meant as TV video recordings, so common video related metadata are available in <seealso cref="VideoStreamAspect"/>.
  /// </summary>
  public class RecordingAspect
  {
    /// <summary>
    /// Media item aspect id of the recording aspect.
    /// </summary>
    public static readonly Guid ASPECT_ID = new Guid("8DB70262-0DCE-4C80-AD03-FB1CDF7E1913");

    /// <summary>
    /// Channel name where the program was recorded.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_CHANNEL =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("Channel", 50, Cardinality.Inline, true);

    /// <summary>
    /// Contains the recording start date and time.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_STARTTIME =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("StartTime", typeof(DateTime), Cardinality.Inline, false);

    /// <summary>
    /// Contains the recording start date and time.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_ENDTIME =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("EndTime", typeof(DateTime), Cardinality.Inline, false);

    public static readonly SingleMediaItemAspectMetadata Metadata = new SingleMediaItemAspectMetadata(
        ASPECT_ID, "RecordingItem", new[] {
            ATTR_CHANNEL,
            ATTR_STARTTIME,
            ATTR_ENDTIME
        });
  }
}
