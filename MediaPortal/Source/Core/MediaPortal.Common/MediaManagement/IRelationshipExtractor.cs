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

using MediaPortal.Common.MediaManagement.MLQueries;
using System;
using System.Collections.Generic;

namespace MediaPortal.Common.MediaManagement
{
  /// <summary>
  /// A relationship role extractor is response for enriching a single type of relationship from one group of aspects to another
  /// </summary>
  public interface IRelationshipRoleExtractor
  {
    /// <summary>
    /// Role that the media item being processed will get
    /// </summary>
    Guid Role { get; }

    /// <summary>
    /// Aspects that the media item being processed must have
    /// </summary>
    Guid[] RoleAspects { get; }

    /// <summary>
    /// Role that the media item linked to the media item being process will get
    /// </summary>
    Guid LinkedRole { get; }

    /// <summary>
    /// Aspects that the media item linked to the media item being processed must have
    /// </summary>
    Guid[] LinkedRoleAspects { get; }

    /// <summary>
    /// Aspects that must be present in order to accurately match items in <see cref="TryMatch"/>  
    /// </summary>
    Guid[] MatchAspects { get; }

    /// <summary>
    /// Specifies whether or not this relation should be built. A relationship should not be 
    /// built if it creates the inverse of an already existing relationship.
    /// E.g. If Series -> Episode exists don't create Episode -> Series.
    /// </summary>
    bool BuildRelationship { get; }

    /// <summary>
    /// Get optimized filter that can be used to find a direct match to any existing media item.
    /// </summary>
    /// <param name="extractedAspects"></param>
    /// <returns></returns>
    IFilter GetSearchFilter(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects);

    /// <summary>
    /// Add extracted media item to cache so querying the database can be avoided
    /// </summary>
    /// <param name="extractedItemId"></param>
    /// <param name="extractedAspects"></param>
    /// <returns></returns>
    void CacheExtractedItem(Guid extractedItemId, IDictionary<Guid, IList<MediaItemAspect>> extractedAspects);

    /// <summary>
    /// Part 1 of the relationship building - try to build a relationship
    /// from a group of aspects with Role to another group of aspects Linked Role
    /// </summary>
    /// <param name="aspects"></param>
    /// <param name="importOnly"></param>
    /// <param name="extractedLinkedAspects"></param>
    /// <returns></returns>
    bool TryExtractRelationships(IDictionary<Guid, IList<MediaItemAspect>> aspects, bool importOnly, out IList<RelationshipItem> extractedLinkedAspects);

    /// <summary>
    /// Part 2 of the relationship building - if the extract was successful
    /// see if each group of linked aspects match an existing group.
    /// If the extracted data contains external identifiers these will be queried
    /// by MediaLibrary against any existing media items. There's no guarantee that
    /// an MI which contains a particular source / type / ID is the same item as the
    /// extracted data (for example a TVDB series identifier is shared by all seasons
    /// and episodes of that series) and since MediaLibrary doesn't know how to choose
    /// between them it delegates to the extractor
    /// </summary>
    /// <param name="extractedAspects"></param>
    /// <param name="existingAspects"></param>
    /// <returns></returns>
    bool TryMatch(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects, IDictionary<Guid, IList<MediaItemAspect>> existingAspects);

    /// <summary>
    /// Part 3 of the relationship building - if the linked items are new
    /// MediaLibrary will create a relationship between them. It knows what to
    /// set the Role and LinkedRole attributes to but needs help with the Index
    /// </summary>
    /// <param name="aspects"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    bool TryGetRelationshipIndex(IDictionary<Guid, IList<MediaItemAspect>> aspects, IDictionary<Guid, IList<MediaItemAspect>> linkedAspects, out int index);
  }

  /// <summary>
  /// A relationship extractor is responsible for enriching a media item with related data.
  /// </summary>
  /// <remarks>
  /// </remarks>
  public interface IRelationshipExtractor
  {
    /// <summary>
    /// Returns the metadata descriptor for this metadata relationship extractor.
    /// </summary>
    RelationshipExtractorMetadata Metadata { get; }

    /// <summary>
    /// Returns a list of relationship role extractors.
    /// </summary>
    IList<IRelationshipRoleExtractor> RoleExtractors { get; }

    /// <summary>
    /// Returns a list of relationship role hierarchies.
    /// </summary>
    IList<RelationshipHierarchy> Hierarchies { get; }

    /// <summary>
    /// Returns a list filters to use to find media items which can be updated 
    /// because new metadata is available. Each filter also has a limit to the 
    /// number of items to find.
    /// </summary>
    IDictionary<IFilter, uint> GetLastChangedItemsFilters();

    /// <summary>
    /// Resets the current list of changed items so they are not included in 
    /// the nest query for changed items.
    /// </summary>
    void ResetLastChangedItems();
  }
}
