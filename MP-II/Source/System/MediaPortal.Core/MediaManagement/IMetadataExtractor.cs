#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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
using System.Collections.Generic;

namespace MediaPortal.Core.MediaManagement
{
  /// <summary>
  /// A metadata extractor is responsible for extracting metadata from a physical media file.
  /// Each metadata extractor creates metadata reports for its defined <see cref="MediaItemAspect"/> instances.
  /// </summary>
  /// <remarks>
  /// The metadata extractor is partitioned in its metadata descriptor part (<see cref="Metadata"/>)
  /// and this worker class.
  /// </remarks>
  public interface IMetadataExtractor
  {
    /// <summary>
    /// Returns the metadata descriptor for this metadata extractor.
    /// </summary>
    MetadataExtractorMetadata Metadata { get; }

    /// <summary>
    /// Worker method to actually try a metadata extraction from the media resource given by
    /// <paramref name="mediaItemAccessor"/>.
    /// If this method returns <c>true</c>, the extracted media item aspects were written to the
    /// <paramref name="extractedAspectData"/> collection.
    /// </summary>
    /// <param name="mediaItemAccessor">The media item resource accessor to open the stream to the physical media.</param>
    /// <param name="extractedAspectData">Dictionary containing a mapping of media item aspect ids to
    /// already present media item aspects, this metadata extractor should edit. If a media item aspect is not present
    /// in this dictionary but found by this metadata extractor, it will add it to the dictionary.</param>
    /// <returns><c>true</c> if the metadata could be extracted from the specified media item, else <c>false</c>.
    /// If the return value is <c>true</c>, the extractedAspectData collection was filled by this metadata extractor.
    /// If the return value is <c>false</c>, the <paramref name="extractedAspectData"/> collection remains
    /// unchanged.</returns>
    bool TryExtractMetadata(IResourceAccessor mediaItemAccessor, IDictionary<Guid, MediaItemAspect> extractedAspectData);
  }
}
