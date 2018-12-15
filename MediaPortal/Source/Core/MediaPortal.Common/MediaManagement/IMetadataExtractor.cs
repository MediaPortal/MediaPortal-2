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

using MediaPortal.Common.ResourceAccess;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MediaPortal.Common.MediaManagement
{
  /// <summary>
  /// Contains data for a match found during a search.
  /// </summary>
  public class MediaItemSearchResult
  {
    /// <summary>
    /// External Ids found in the match.
    /// </summary>
    public IDictionary<string, string> ExternalIds = new Dictionary<string, string>();
    /// <summary>
    /// A descriptive (preferably unique) name for the match. 
    /// </summary>
    public string Name;
    /// <summary>
    /// Detailed description of the match if available.
    /// </summary>
    public string Description;
    /// <summary>
    /// Aspect data for the match that can be used for storing if needed.
    /// </summary>
    public IDictionary<Guid, IList<MediaItemAspect>> AspectData = new Dictionary<Guid, IList<MediaItemAspect>>();
  }

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
    /// <remarks>
    /// If <see cref="MetadataExtractorMetadata.ProcessesNonFiles"/> is <c>true</c>, file resources as well as other resources
    /// will be passed to this method.
    /// </remarks>
    /// <param name="mediaItemAccessor">The media item resource accessor to open the stream to the physical media.</param>
    /// <param name="extractedAspectData">Dictionary containing a mapping of media item aspect ids to
    /// already present media item aspects, this metadata extractor should edit. If a media item aspect is not present
    /// in this dictionary but found by this metadata extractor, it will add it to the dictionary.</param>
    /// <param name="forceQuickMode">Interactive browsing needs to be quick, but the server side importer can extract further details.
    /// If the value is set to <c>true</c>, no slow operations are permitted (like lookup of metadata from the internet or
    /// non-cached thumbnail extraction).</param>
    /// 
    /// <returns><c>true</c> if the metadata could be extracted from the specified media item, else <c>false</c>.
    /// If the return value is <c>true</c>, the extractedAspectData collection was filled by this metadata extractor.
    /// If the return value is <c>false</c>, the <paramref name="extractedAspectData"/> collection remains
    /// unchanged.</returns>
    Task<bool> TryExtractMetadataAsync(IResourceAccessor mediaItemAccessor, IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData, bool forceQuickMode);

    /// <summary>
    /// Checks if the given directory <paramref name="mediaItemAccessor"/> is considered a "single item" media source (like DVD or BD folders on hard drive).
    /// </summary>
    /// <param name="mediaItemAccessor">The media item resource accessor to open the stream to the physical media.</param>
    /// <returns><c>true</c> if it is a single item.</returns>
    bool IsDirectorySingleResource(IResourceAccessor mediaItemAccessor);

    /// <summary>
    /// Checks if the given <paramref name="mediaItemAccessor"/> points to a stub (a CD, DVD or BD placeholder).
    /// </summary>
    /// <param name="mediaItemAccessor">The media item resource accessor to open the stream to the physical media.</param>
    /// <returns><c>true</c> if it is a stub item.</returns>
    bool IsStubResource(IResourceAccessor mediaItemAccessor);

    /// <summary>
    /// Worker method to actually try stub extraction from the media resource given by
    /// <paramref name="mediaItemAccessor"/>.
    /// If this method returns <c>true</c>, the extracted media item aspects were written to the
    /// <paramref name="extractedAspectData"/> collection.
    /// </summary>
    /// <remarks>
    /// If <see cref="MetadataExtractorMetadata.ProcessesNonFiles"/> is <c>true</c>, file resources as well as other resources
    /// will be passed to this method.
    /// </remarks>
    /// <param name="mediaItemAccessor">The media item resource accessor to open the stream to the physical media.</param>
    /// <param name="extractedStubAspectData">List of dictionaries containing a mapping of media item aspect ids to stub aspects.</param>
    /// <returns><c>true</c> if stubs could be extracted from the specified media item, else <c>false</c>.
    /// If the return value is <c>true</c>, the extractedStubAspectData collection was filled by this metadata extractor.</returns>
    bool TryExtractStubItems(IResourceAccessor mediaItemAccessor, ICollection<IDictionary<Guid, IList<MediaItemAspect>>> extractedStubAspectData);

    /// <summary>
    /// Searches for other media matching the specified <paramref name="searchAspectData"/>.
    /// The search results could have various sources like files, online data etc.
    /// </summary>
    /// <param name="searchAspectData">A dictionary containing all the aspects to use for finding a match.</param>
    /// <param name="searchCategories">The media categories to find matches for if available. This is to avoid finding series matches for movie aspects.</param>
    /// <returns>A list of <see cref="MediaItemSearchResult"/> with descriptive data.</returns>
    Task<IList<MediaItemSearchResult>> SearchForMatchesAsync(IDictionary<Guid, IList<MediaItemAspect>> searchAspectData, ICollection<string> searchCategories);

    /// <summary>
    /// Adds more aspect details to <paramref name="matchedAspectData"/> if available.
    /// The details could have various sources like files, online data etc.
    /// </summary>
    /// <param name="matchedAspectData">A dictionary containing all the currently matched aspects to which details can be added if available.</param>
    /// <returns><c>true</c> if details were added, else <c>false</c>.
    Task<bool> AddMatchedAspectDetailsAsync(IDictionary<Guid, IList<MediaItemAspect>> matchedAspectData);
  }
}
