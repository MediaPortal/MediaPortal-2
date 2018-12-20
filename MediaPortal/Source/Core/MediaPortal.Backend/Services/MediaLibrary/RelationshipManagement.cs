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

using MediaPortal.Backend.Database;
using MediaPortal.Backend.Services.Database;
using MediaPortal.Backend.Services.MediaLibrary.QueryEngine;
using MediaPortal.Backend.Services.UserProfileDataManagement;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.VirtualResourceProvider;
using MediaPortal.Common.UserProfileDataManagement;
using MediaPortal.Utilities;
using MediaPortal.Utilities.DB;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace MediaPortal.Backend.Services.MediaLibrary
{
  /// <summary>
  /// Class for updating and deleting relationships and their <see cref="RelationshipType"/>s.
  /// </summary>
  public class RelationshipManagement
  {
    #region Protected Members

    //Escape char to use for strings in path expressions
    protected const char ESCAPE_CHAR = '\\';

    //Used for getting the table and column names in queries.
    protected MIA_Management _miaManagement;

    //The system id to use when creating virtual resource paths
    protected string _virtualItemSystemId;    

    protected static readonly ICollection<int> _primaryResourceTypes = new int[] { ProviderResourceAspect.TYPE_PRIMARY, ProviderResourceAspect.TYPE_STUB };

    #endregion
    
    #region Constructor

    /// <summary>
    /// Creates a new <see cref="RelationshipManagement"/> instance. 
    /// </summary>
    /// <param name="miaManagement"><see cref="MIA_Management"/> to use for generating table and column names in queries.</param>
    /// <param name="virtualItemSystemId">The system id to use when creating virtual resource paths.</param>
    public RelationshipManagement(MIA_Management miaManagement, string virtualItemSystemId)
    {
      _miaManagement = miaManagement;
      _virtualItemSystemId = virtualItemSystemId;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Returns the role of a parent from its aspects.
    /// </summary>
    /// <param name="parentAspects"></param>
    /// <returns>The role guid of the parent if found.</returns>
    public Guid? GetParentRoleFromAspects(IEnumerable<Guid> parentAspects)
    {
      foreach(var knownType in ServiceRegistration.Get<IRelationshipTypeRegistration>().LocallyKnownHierarchicalRelationshipTypes)
      {
        if (parentAspects.Any(a => a == knownType.ParentAspectId))
          return knownType.ParentRole;
      }
      return null;
    }

    /// <summary>
    /// Deletes all resources in the specified path, deletes any orphaned relationships and updates the state of any remaining parent items.
    /// </summary>
    /// <param name="transaction"></param>
    /// <param name="hierarchies"></param>
    /// <param name="systemId"></param>
    /// <param name="basePath"></param>
    /// <param name="inclusive"></param>
    /// <returns>The number of media items that have been deleted.</returns>
    public int DeletePathAndRelationships(ITransaction transaction, string systemId, ResourcePath basePath, bool inclusive)
    {
      ICollection<RelationshipType> hierarchies = _miaManagement.LocallyKnownHierarchicalRelationshipTypes;

      //Delete any resource paths where another primary resource in another path exists
      DeleteSecondaryResourcesForPath(transaction, systemId, basePath, inclusive);

      //Delete all media items in the path that don't have any parents with children in another share  
      int numDeleted = DeleteOrphanedMediaItemsForPath(transaction, hierarchies, systemId, basePath, inclusive);

      //Delete all orphaned relationships
      IEnumerable<Guid> primaryChildRoles = hierarchies.Where(h => h.IsPrimaryParent).Select(h => h.ChildRole).Distinct();
      numDeleted += DeleteOrphanedRelations(transaction, primaryChildRoles);

      //Create new virtual paths for media items with parents in another path and update the parent virtual and playback state
      SetPathToVirtual(transaction, hierarchies, systemId, basePath, inclusive);

      //Update any newly created stub media items
      UpdateStubResources(transaction);

      //Delete all orphaned ReationshipAspects and attribute values
      Cleanup(transaction);

      return numDeleted;
    }

    /// <summary>
    /// Deletes the media item from the database and all orphaned relationships.
    /// </summary>
    /// <param name="transaction"></param>
    /// <param name="mediaItemId"></param>
    /// <returns></returns>
    public int DeleteMediaItemAndRelationships(ITransaction transaction, Guid mediaItemId)
    {
      int numDeleted;

      ICollection<RelationshipType> hierarchies = _miaManagement.LocallyKnownHierarchicalRelationshipTypes;

      //Delete orphaned direct relationships of media item
      using (IDbCommand command = DeleteOrphanedRelationsCommand(transaction, mediaItemId))
        numDeleted = command.ExecuteNonQuery();

      //Delete orphaned descendent relationships
      IEnumerable<Guid> primaryChildRoles = hierarchies.Where(h => h.IsPrimaryParent).Select(h => h.ChildRole).Distinct();
      numDeleted += DeleteOrphanedRelations(transaction, primaryChildRoles);

      //Map of affected parent ids and their hierarchies
      IDictionary<Guid, ICollection<RelationshipType>> affectedParents = new Dictionary<Guid, ICollection<RelationshipType>>();

      GetParents(transaction, mediaItemId, hierarchies, affectedParents);

      //Delete the actual media item
      using (IDbCommand command = DeleteMediaItemIdCommand(transaction, mediaItemId))
        numDeleted += command.ExecuteNonQuery();

      //Update remaining parents
      UpdateParentState(transaction, affectedParents);

      //Delete all orphaned ReationshipAspects and attribute values
      Cleanup(transaction);

      return numDeleted;
    }

    /// <summary>
    /// Updates the virtual and playback state of the parents of the media item with the specified <paramref name="mediaItemId"/>.
    /// </summary>
    /// <param name="transaction"></param>
    /// <param name="mediaItemId"></param>
    /// <param name="hierarchies"></param>
    public void UpdateParents(ITransaction transaction, Guid mediaItemId)
    {
      ICollection<RelationshipType> hierarchies = _miaManagement.LocallyKnownHierarchicalRelationshipTypes;

      //Map of affected parent ids and their hierarchies
      IDictionary<Guid, ICollection<RelationshipType>> affectedParents = new Dictionary<Guid, ICollection<RelationshipType>>();

      //Get the parents and their corresponding hierarchies
      GetParents(transaction, mediaItemId, hierarchies, affectedParents);

      //Update the parent virtual and playback state
      UpdateParentState(transaction, affectedParents);

      //Update any descendants
      UpdateDescendantHierarchies(transaction, affectedParents, hierarchies);
    }
    
    public void UpdateParentPlayState(ITransaction transaction, Guid mediaItemId)
    {
      IEnumerable<RelationshipType> hierarchies = _miaManagement.LocallyKnownHierarchicalRelationshipTypes.Where(h => h.UpdatePlayPercentage);

      //Map of affected parent ids and their hierarchies
      IDictionary<Guid, ICollection<RelationshipType>> affectedParents = new Dictionary<Guid, ICollection<RelationshipType>>();

      //Get the parents and their corresponding hierarchies
      GetParents(transaction, mediaItemId, hierarchies, affectedParents);

      //update parent play state
      UpdateParentPlayState(transaction, affectedParents);

      //Update descendent play state
      UpdateDescendantPlayState(transaction, affectedParents, hierarchies);
    }

    /// <summary>
    /// Returns the children for the parent.
    /// </summary>
    /// <param name="transaction"></param>
    /// <param name="parentId"></param>
    /// <returns>The children of the specified parent.</returns>
    public IEnumerable<Guid> GetPlayableChildren(ITransaction transaction, Guid parentId)
    {
      IEnumerable<RelationshipType> hierarchies = _miaManagement.LocallyKnownHierarchicalRelationshipTypes.Where(h => h.UpdatePlayPercentage);

      List<Guid> childIds = new List<Guid>();
      ISQLDatabase database = transaction.Database;
      using (IDbCommand command = SelectChildrenCommand(transaction, parentId, hierarchies))
      {
        using (IDataReader reader = command.ExecuteReader())
        {
          while (reader.Read())
          {
            childIds.Add(database.ReadDBValue<Guid>(reader, 0));
          }
        }
      }
      return childIds;
    }

    /// <summary>
    /// Returns the parent with specified role for the child.
    /// </summary>
    /// <param name="transaction"></param>
    /// <param name="childId"></param>
    /// <param name="parentRole"></param>
    /// <returns>The parent of the specified child.</returns>
    public Guid? GetParent(ITransaction transaction, Guid childId, Guid parentRole)
    {
      IEnumerable<RelationshipType> hierarchies = _miaManagement.LocallyKnownHierarchicalRelationshipTypes.Where(h => h.ParentRole == parentRole);

      //Map of parent ids and their hierarchies
      IDictionary<Guid, ICollection<RelationshipType>> parents = new Dictionary<Guid, ICollection<RelationshipType>>();
      GetParents(transaction, childId, hierarchies, parents);

      if (parents?.Count > 0)
        return parents.First().Key;

      return null;
    }

    #endregion

    #region Protected Methods

    /// <summary>
    /// Deletes all resources in the specified path from the database where another primary resource for the media item
    /// exists in another share.
    /// </summary>
    /// <param name="transaction"></param>
    /// <param name="systemId"></param>
    /// <param name="basePath"></param>
    /// <param name="inclusive"></param>
    protected int DeleteSecondaryResourcesForPath(ITransaction transaction, string systemId, ResourcePath basePath, bool inclusive)
    {
      using (IDbCommand command = DeleteSecondaryResourcesCommand(transaction, systemId, basePath, inclusive))
      {
        int affectedRows = command.ExecuteNonQuery();
        Logger.Debug("RelationshipManagement: Deleted {0} secondary resources for path {1}", affectedRows, basePath);
        return affectedRows;
      }
    }

    /// <summary>
    /// Sets the IsStub attribute on any media items that only have stub resources.
    /// </summary>
    /// <param name="transaction"></param>
    /// <returns></returns>
    protected int UpdateStubResources(ITransaction transaction)
    {
      using (IDbCommand command = UpdateStubResourcesCommand(transaction))
      {
        int affectedRows = command.ExecuteNonQuery();
        Logger.Debug("RelationshipManagement: Set IsStub to True on {0} resources", affectedRows);
        return affectedRows;
      }
    }

    /// <summary>
    /// Delete all resources in the specified path from the database where the primary parent doesn't have any children
    /// in another share.
    /// </summary>
    /// <param name="transaction"></param>
    /// <param name="hierarchies"></param>
    /// <param name="systemId"></param>
    /// <param name="basePath"></param>
    /// <param name="inclusive"></param>
    /// <returns>The number of media items that have been deleted.</returns>
    protected int DeleteOrphanedMediaItemsForPath(ITransaction transaction, IEnumerable<RelationshipType> hierarchies, string systemId, ResourcePath basePath, bool inclusive)
    {
      //number of media items that were deleted
      int numDeleted;

      //Delete all items in the path that don't have any relationships
      using (IDbCommand command = DeleteRelationlessMediaItemsCommand(transaction, hierarchies, systemId, basePath, inclusive))
        numDeleted = command.ExecuteNonQuery();

      //Delete all items in the path that don't have any parents with children in another path
      using (IDbCommand command = DeleteOrphanedMediaItemsCommand(transaction, hierarchies, systemId, basePath, inclusive))
        numDeleted += command.ExecuteNonQuery();

      Logger.Debug("RelationshipManagement: Deleted {0} orphaned media items for path {1}", numDeleted, basePath);
      return numDeleted;
    }

    /// <summary>
    /// Deletes all virtual relations that don't have any relationships. 
    /// </summary>
    /// <param name="transaction"></param>
    /// <param name="primaryChildRoles"></param>
    protected int DeleteOrphanedRelations(ITransaction transaction, IEnumerable<Guid> primaryChildRoles)
    {
      //total number of media items that were deleted
      int numDeleted = 0;
      int pass = 1;
      int affectedRows;

      //Recursively delete the orphans until no new orphans are created
      using (IDbCommand command = DeleteOrphanedRelationsCommand(transaction, primaryChildRoles))
      {
        do
        {
          affectedRows = command.ExecuteNonQuery();
          numDeleted += affectedRows;
          Logger.Info("RelationshipManagement: Deleted {0} orphaned relationships: pass {1}", affectedRows, pass++);
        }
        while (affectedRows > 0);
      }
      return numDeleted;
    }

    /// <summary>
    /// Deletes all relations of the media item that don't have any other children.
    /// </summary>
    /// <param name="transaction"></param>
    /// <param name="mediaItemId"></param>
    /// <returns></returns>
    protected int DeleteOrphanedRelationsOfMediaItem(ITransaction transaction, Guid mediaItemId)
    {
      int affectedRows;
      using (IDbCommand command = DeleteOrphanedRelationsCommand(transaction, mediaItemId))
        affectedRows = command.ExecuteNonQuery();
      return affectedRows;
    }

    /// <summary>
    /// Sets all resources for the specified path to virtual and updates the virtual state and play data for any affected parents.
    /// </summary>
    /// <param name="transaction"></param>
    /// <param name="hierarchies"></param>
    /// <param name="systemId"></param>
    /// <param name="basePath"></param>
    /// <param name="inclusive"></param>
    protected void SetPathToVirtual(ITransaction transaction, IEnumerable<RelationshipType> hierarchies, string systemId, ResourcePath basePath, bool inclusive)
    {
      //Map of affected parent ids and their hierarchies
      IDictionary<Guid, ICollection<RelationshipType>> affectedParents = new Dictionary<Guid, ICollection<RelationshipType>>();

      //Delete all ProviderResourceAspects in the specified path and get the ids of affected items and their parents 
      ICollection<Guid> affectedChildren = DeleteResourcesForPath(transaction, hierarchies, systemId, basePath, inclusive, affectedParents);

      //Nothing to update
      if (affectedChildren.Count == 0)
        return;

      //Add new virtual resource paths for affected children
      AddNewVirtualItems(transaction, affectedChildren);

      //Update the available item counts and play data of affected parents
      UpdateParentState(transaction, affectedParents);

      //Update any descendants
      UpdateDescendantHierarchies(transaction, affectedParents, hierarchies);
    }

    /// <summary>
    /// Cleans up the database by deleting any <see cref="RelationshipAspect"/>s where the <see cref="RelationshipAspect.ATTR_LINKED_ID"/>
    /// no longer exists and deletes all orphaned attribute values.
    /// </summary>
    /// <param name="transaction"></param>
    protected void Cleanup(ITransaction transaction)
    {
      using (IDbCommand command = DeleteOrphanedRelationshipAspectsCommand(transaction))
        command.ExecuteNonQuery();

      _miaManagement.CleanupAllOrphanedAttributeValues(transaction);
    }

    /// <summary>
    /// Adds the parents and their corresponding hierarchies to <paramref name="affectedParents"/>.
    /// </summary>
    /// <param name="transaction"></param>
    /// <param name="childId"></param>
    /// <param name="hierarchies"></param>
    /// <param name="affectedParents"></param>
    protected void GetParents(ITransaction transaction, Guid childId, IEnumerable<RelationshipType> hierarchies, IDictionary<Guid, ICollection<RelationshipType>> affectedParents)
    {
      ISQLDatabase database = transaction.Database;
      using (IDbCommand command = SelectParentsCommand(transaction, childId, hierarchies))
      {
        //Get the affected children and their relationships for the hierarchies
        using (IDataReader reader = command.ExecuteReader())
        {
          while (reader.Read())
          {
            Guid childRole = database.ReadDBValue<Guid>(reader, 0);
            Guid parentRole = database.ReadDBValue<Guid>(reader, 1);
            Guid parentId = database.ReadDBValue<Guid>(reader, 2);

            //Get the matching hierarchy for the relationship
            RelationshipType hierarchy = hierarchies.FirstOrDefault(h => h.ChildRole == childRole && h.ParentRole == parentRole);
            if (hierarchy != null)
              AddParentHierarchy(parentId, hierarchy, affectedParents);
          }
        }
      }
    }

    /// <summary>
    /// Deletes all ProviderResourceAspects in the specified path, adds the ids of any parents and their corresponding hierarchies to <paramref name="affectedParents"/>
    /// and returns the ids of all items that have had a ProviderResourceAspect deleted.
    /// </summary>
    /// <param name="transaction"></param>
    /// <param name="hierarchies"></param>
    /// <param name="systemId"></param>
    /// <param name="basePath"></param>
    /// <param name="inclusive"></param>
    /// <param name="affectedParents"></param>
    /// <returns>The ids of all items that have had a ProviderResourceAspect deleted.</returns>
    protected ICollection<Guid> DeleteResourcesForPath(ITransaction transaction, IEnumerable<RelationshipType> hierarchies, string systemId, ResourcePath basePath, bool inclusive,
      IDictionary<Guid, ICollection<RelationshipType>> affectedParents)
    {
      //Ids of all child items in the specified path
      HashSet<Guid> affectedChildren = new HashSet<Guid>();

      ISQLDatabase database = transaction.Database;
      using (IDbCommand command = SelectParentsCommand(transaction, hierarchies, systemId, basePath, inclusive))
      {
        //Get the affected children and their relationships and hierarchies
        using (IDataReader reader = command.ExecuteReader())
        {
          while (reader.Read())
          {
            affectedChildren.Add(database.ReadDBValue<Guid>(reader, 0));
            Guid childRole = database.ReadDBValue<Guid>(reader, 1);
            Guid parentRole = database.ReadDBValue<Guid>(reader, 2);
            Guid parentId = database.ReadDBValue<Guid>(reader, 3);

            //Get the matching hierarchy for the relationship
            RelationshipType hierarchy = hierarchies.FirstOrDefault(h => h.ChildRole == childRole && h.ParentRole == parentRole);
            if (hierarchy != null)
              AddParentHierarchy(parentId, hierarchy, affectedParents);
          }
        }
        Logger.Info("RelationshipManagement: Found {0} items to update as virtual", affectedChildren.Count);
      }

      //Delete the resource paths
      using (IDbCommand command = DeleteResourcePathsCommand(transaction, systemId, basePath, inclusive))
      {
        int affectedRows = command.ExecuteNonQuery();
        Logger.Info("RelationshipManagement: Deleted {0} resources", affectedRows);
      }

      return affectedChildren;
    }

    /// <summary>
    /// Adds a virtual resource path to the provider table for the items with the specified ids
    /// and sets the items as virtual.
    /// </summary>
    /// <param name="transaction"></param>
    /// <param name="itemsToUpdate"></param>
    protected void AddNewVirtualItems(ITransaction transaction, IEnumerable<Guid> itemsToUpdate)
    {
      //Add the virtual resource for each child and update the IsVirtual attribute
      foreach (Guid item in itemsToUpdate)
      {
        using (IDbCommand command = InsertNewVirtualResourceCommand(transaction, item))
        {
          int affectedRows = command.ExecuteNonQuery();
        }
      }
    }

    /// <summary>
    /// Updates the available item count and play data for the specified parent hierarchies
    /// </summary>
    /// <param name="transaction"></param>
    /// <param name="parentHierarchy"></param>
    protected void UpdateParentState(ITransaction transaction, IDictionary<Guid, ICollection<RelationshipType>> parentHierarchy)
    {
      //Update each parent
      foreach (var kvp in parentHierarchy)
      {
        Guid parentId = kvp.Key;

        //Update each hierarchy
        foreach (RelationshipType hierarchy in kvp.Value)
        {
          //Get the child count and play data for the hierarchy
          Dictionary<Guid, int> playData;
          int itemCount = GetChildCount(transaction, parentId, hierarchy, out playData);

          //Update the isVirtual attribute
          UpdateParentIsVirtual(transaction, parentId, itemCount == 0);

          //Update the available item count
          UpdateAvailableCount(transaction, parentId, hierarchy, itemCount);

          if (hierarchy.UpdatePlayPercentage && itemCount > 0)
          {
            //Update the play data for each user profile
            foreach (var userData in playData)
              UpdateUserPlayPercentage(transaction, parentId, userData.Key, 100 * userData.Value / itemCount);
          }
        }
      }
    }

    /// <summary>
    /// Updates the play count and percentage for each parent in the specified <paramref name="playableParentHierarchy"/>.
    /// </summary>
    /// <param name="transaction"></param>
    /// <param name="playableParentHierarchy"></param>
    protected void UpdateParentPlayState(ITransaction transaction, IDictionary<Guid, ICollection<RelationshipType>> playableParentHierarchy)
    {
      //Update each parent
      foreach (var kvp in playableParentHierarchy)
      {
        Guid parentId = kvp.Key;

        //Update each hierarchy
        foreach (RelationshipType hierarchy in kvp.Value)
        {
          //Get the child count and play data for the hierarchy
          Dictionary<Guid, int> playData;
          int itemCount = GetChildCount(transaction, parentId, hierarchy, out playData);

          //Update the play data for each user profile
          foreach (var userData in playData)
            UpdateUserPlayPercentage(transaction, parentId, userData.Key, 100 * userData.Value / itemCount);
        }
      }
    }

    /// <summary>
    /// Updates the play count and percentage of descendents of the specified <paramref name="childHierarchy"/>.
    /// </summary>
    /// <param name="transaction"></param>
    /// <param name="childHierarchy"></param>
    /// <param name="hierarchies"></param>
    protected void UpdateDescendantPlayState(ITransaction transaction, IDictionary<Guid, ICollection<RelationshipType>> childHierarchy, IEnumerable<RelationshipType> hierarchies)
    {
      //Map of descendants and their corresponding hierarchies
      IDictionary<Guid, ICollection<RelationshipType>> descendantHierarchy = new Dictionary<Guid, ICollection<RelationshipType>>();

      //Populate the descendant hierarchy
      foreach (var child in childHierarchy)
        foreach (RelationshipType hierarchy in child.Value)
        {
          ICollection<RelationshipType> hierarchyParents = GetHierarchyParents(hierarchy, hierarchies);
          if (hierarchyParents.Count > 0)
            GetParents(transaction, child.Key, hierarchies, descendantHierarchy);
        }

      if (descendantHierarchy.Count > 0)
      {
        //Update the descendant states
        UpdateParentPlayState(transaction, descendantHierarchy);

        //Update any descendants
        UpdateDescendantPlayState(transaction, descendantHierarchy, hierarchies);
      }
    }

    /// <summary>
    /// Updates the virtual and playback state of all descendants of the items in the specified <paramref name="childHierarchy"/>.
    /// </summary>
    /// <param name="transaction"></param>
    /// <param name="childHierarchy"></param>
    /// <param name="hierarchies"></param>
    protected void UpdateDescendantHierarchies(ITransaction transaction, IDictionary<Guid, ICollection<RelationshipType>> childHierarchy, IEnumerable<RelationshipType> hierarchies)
    {
      //Map of descendants and their corresponding hierarchies
      IDictionary<Guid, ICollection<RelationshipType>> descendantHierarchy = new Dictionary<Guid, ICollection<RelationshipType>>();

      //Populate the descendant hierarchy
      foreach (var child in childHierarchy)
        foreach (RelationshipType hierarchy in child.Value)
        {
          ICollection<RelationshipType> hierarchyParents = GetHierarchyParents(hierarchy, hierarchies);
          if (hierarchyParents.Count > 0)
            GetParents(transaction, child.Key, hierarchies, descendantHierarchy);
        }

      if (descendantHierarchy.Count > 0)
      {
        //Update the descendant states
        UpdateParentState(transaction, descendantHierarchy);

        //Update any descendants
        UpdateDescendantHierarchies(transaction, descendantHierarchy, hierarchies);
      }
    }

    /// <summary>
    /// Returns the number of children and a map of user profiles and played item count for the parent in the specified hierarchy.
    /// </summary>
    /// <param name="database"></param>
    /// <param name="command"></param>
    /// <param name="parentId"></param>
    /// <param name="hierarchy"></param>
    /// <param name="playData"></param>
    /// <returns>The number of children of the specified parent.</returns>
    protected int GetChildCount(ITransaction transaction, Guid parentId, RelationshipType hierarchy, out Dictionary<Guid, int> playData)
    {
      //Total number of children
      int itemCount = 0;
      IDictionary<Guid, HashSet<Guid>> childIds = new Dictionary<Guid, HashSet<Guid>>();
      IDictionary<Guid, HashSet<Guid>> childUsers = new Dictionary<Guid, HashSet<Guid>>();

      //Map of user profile id and played item count
      playData = new Dictionary<Guid, int>();

      ISQLDatabase database = transaction.Database;
      using (IDbCommand command = SelectChildrenAndPlayCountCommand(transaction, parentId, hierarchy))
      {
        using (IDataReader reader = command.ExecuteReader())
        {
          while (reader.Read())
          {
            Guid childId = database.ReadDBValue<Guid>(reader, 0);
            Guid childAttributeId = database.ReadDBValue<Guid>(reader, 1);

            HashSet<Guid> childAttributeIds;
            if (!childIds.TryGetValue(childId, out childAttributeIds))
              childIds[childId] = childAttributeIds = new HashSet<Guid>();
            
            if (childAttributeIds.Add(childAttributeId))
              itemCount++;
            
            //Get the user profile if it exists
            Guid? userProfileId = database.ReadDBValue<Guid?>(reader, 2);
            if (!userProfileId.HasValue)
              continue;

            HashSet<Guid> users;
            if (!childUsers.TryGetValue(childId, out users))
              childUsers[childId] = users = new HashSet<Guid>();

            //If child count attribute is a collection attribute we might get multiple rows
            //for the same media item - user combination. Don't duplicate the play data in this case
            if (!users.Add(userProfileId.Value))
              continue;

            bool played = false;
            int playCount;

            //Try to get the user specific play count 
            if (int.TryParse(database.ReadDBValue<string>(reader, 3), out playCount))
              played = playCount > 0;
            else
            {
              //Prefer user play count but use overall play count if not available
              int? totalPlayCount = database.ReadDBValue<int?>(reader, 4);
              played = totalPlayCount.HasValue && totalPlayCount.Value > 0;
            }

            //Update the played item count for the user profile
            IncrementPlayedCount(userProfileId.Value, played, playData);
          }
        }
      }
      return itemCount;
    }

    /// <summary>
    /// Updates the IsVirtual attribute for the specified item.
    /// </summary>
    /// <param name="transaction"></param>
    /// <param name="mediaItemId"></param>
    /// <param name="isVirtual"></param>
    protected void UpdateParentIsVirtual(ITransaction transaction, Guid mediaItemId, bool isVirtual)
    {
      using (IDbCommand command = UpdateIsVirtualCommand(transaction, mediaItemId, isVirtual))
      {
        int affectedRows = command.ExecuteNonQuery();
        if (affectedRows > 0)
          Logger.Debug("RelationshipManagement: Updated IsVirtual to {0} for item {1}", isVirtual, mediaItemId);
      }
    }

    /// <summary>
    /// Updates the available item count for the specified parent and hierarchy.
    /// </summary>
    /// <param name="transaction"></param>
    /// <param name="parentId"></param>
    /// <param name="hierarchy"></param>
    /// <param name="availableCount"></param>
    protected void UpdateAvailableCount(ITransaction transaction, Guid parentId, RelationshipType hierarchy, int availableCount)
    {
      using (IDbCommand command = UpdateAvailableCountCommand(transaction, parentId, hierarchy, availableCount))
      {
        int affectedRows = command.ExecuteNonQuery();
        if (affectedRows > 0)
          Logger.Debug("RelationshipManagement: Updated {0} to {1} for item {2}", hierarchy.ParentCountAttribute, availableCount, parentId);
      }
    }

    /// <summary>
    /// Updates the play percentage for the specified item and user profile
    /// </summary>
    /// <param name="transaction"></param>
    /// <param name="mediaItemId"></param>
    /// <param name="userProfileId"></param>
    /// <param name="playPercentage"></param>
    protected void UpdateUserPlayPercentage(ITransaction transaction, Guid mediaItemId, Guid userProfileId, int playPercentage)
    {
      using (IDbCommand command = UpdateUserPlayPercentageCommand(transaction, mediaItemId, userProfileId, playPercentage))
      {
        int affectedRows = command.ExecuteNonQuery();
        if (affectedRows > 0)
          Logger.Debug("RelationshipManagement: Updated play percentage to {0} for item {1} and user id {2}", playPercentage, mediaItemId, userProfileId);
      }
    }

    #endregion

    #region Commands

    /// <summary>
    /// Command to delete all secondary resources for the specified <paramref name="basePath"/>.
    /// </summary>
    /// <param name="transaction"></param>
    /// <param name="systemId"></param>
    /// <param name="basePath"></param>
    /// <param name="inclusive"></param>
    /// <returns></returns>
    protected IDbCommand DeleteSecondaryResourcesCommand(ITransaction transaction, string systemId, ResourcePath basePath, bool inclusive)
    {
      string providerTable = _miaManagement.GetMIATableName(ProviderResourceAspect.Metadata);
      string mediaItemIdAttribute = MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME;
      string resourceTypeAttribute = _miaManagement.GetMIAAttributeColumnName(ProviderResourceAspect.ATTR_TYPE);

      BindVarNamespace bvNamespace = new BindVarNamespace();
      IList<BindVar> bindVars = new List<BindVar>();

      //Condition for all ProviderResourceAspects in the path
      string pathCondition = CreatePathCondition(string.Empty, systemId, basePath, inclusive, bvNamespace, bindVars, false);

      //Condition for all ProviderResourceAspects not in the path
      string notPathCondition = CreatePathCondition(string.Empty, systemId, basePath, inclusive, bvNamespace, bindVars, true);

      IDbCommand result = transaction.CreateCommand();
      result.CommandText =
        "DELETE FROM " + providerTable + " WHERE " + pathCondition +
        " AND " + mediaItemIdAttribute + " IN(" +
          " SELECT " + mediaItemIdAttribute + " FROM " + providerTable +
          " WHERE (" + notPathCondition + ")" +
          " AND " + resourceTypeAttribute + " IN(" + StringUtils.Join(", ", _primaryResourceTypes) + ")" +
        ")";

      //Add the bind vars to the command
      AddCommandParameters(transaction.Database, result, bindVars);

      return result;
    }

    /// <summary>
    /// Command to set IsStub to true for all media items that only have stub resources.
    /// </summary>
    /// <param name="transaction"></param>
    /// <returns></returns>
    protected IDbCommand UpdateStubResourcesCommand(ITransaction transaction)
    {
      string providerTable = _miaManagement.GetMIATableName(ProviderResourceAspect.Metadata);
      string mediaTable = _miaManagement.GetMIATableName(MediaAspect.Metadata);
      string mediaItemIdAttribute = MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME;
      string resourceTypeAttribute = _miaManagement.GetMIAAttributeColumnName(ProviderResourceAspect.ATTR_TYPE);
      string isStubAttribute = _miaManagement.GetMIAAttributeColumnName(MediaAspect.ATTR_ISSTUB);

      IDbCommand result = transaction.CreateCommand();
      result.CommandText =
        "UPDATE " + mediaTable + " SET " + isStubAttribute + " = 1" +
        " WHERE " + isStubAttribute + " = 0" +
        " AND " + mediaItemIdAttribute + " IN(" +
          " SELECT " + mediaItemIdAttribute + " FROM " + providerTable +
          " WHERE " + resourceTypeAttribute + " = " + ProviderResourceAspect.TYPE_STUB +
          " AND " + mediaItemIdAttribute + " NOT IN(" +
            " SELECt " + mediaItemIdAttribute + " FROM " + providerTable +
            " WHERE " + resourceTypeAttribute + " = " + ProviderResourceAspect.TYPE_PRIMARY +
          ")" +
        ")";

      return result;
    }

    /// <summary>
    /// Command to delete all aspects of the media item with the specified <paramref name="mediaItemId"/>.
    /// </summary>
    /// <param name="transaction"></param>
    /// <param name="mediaItemId"></param>
    /// <returns></returns>
    protected IDbCommand DeleteMediaItemIdCommand(ITransaction transaction, Guid mediaItemId)
    {
      string mediaItemTable = MediaLibrary_SubSchema.MEDIA_ITEMS_TABLE_NAME;
      string mediaItemIdAttribute = MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME;

      BindVarNamespace bvNamespace = new BindVarNamespace();
      IList<BindVar> bindVars = new List<BindVar>();
      BindVar idVar = new BindVar(bvNamespace.CreateNewBindVarName("MEDIA_ITEM_ID"), mediaItemId, typeof(Guid));
      bindVars.Add(idVar);
      
      IDbCommand result = transaction.CreateCommand();
      result.CommandText = "DELETE FROM " + mediaItemTable + " WHERE " + mediaItemIdAttribute + " = @" + idVar.Name;

      AddCommandParameters(transaction.Database, result, bindVars);
      return result;
    }

    /// <summary>
    /// Command to delete all media items that don't have a primary parent and which have a resource in the specified <paramref name="basePath"/>. 
    /// </summary>
    /// <param name="transaction"></param>
    /// <param name="hierarchies"></param>
    /// <param name="systemId"></param>
    /// <param name="basePath"></param>
    /// <param name="inclusive"></param>
    /// <returns></returns>
    protected IDbCommand DeleteRelationlessMediaItemsCommand(ITransaction transaction, IEnumerable<RelationshipType> hierarchies, string systemId, ResourcePath basePath, bool inclusive)
    {
      string mediaItemTable = MediaLibrary_SubSchema.MEDIA_ITEMS_TABLE_NAME;
      string providerTable = _miaManagement.GetMIATableName(ProviderResourceAspect.Metadata);
      string relationshipTable = _miaManagement.GetMIATableName(RelationshipAspect.Metadata);
      string mediaItemIdAttribute = MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME;
      string linkedIdAttribute = _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_LINKED_ID);

      BindVarNamespace bvNamespace = new BindVarNamespace();
      IList<BindVar> bindVars = new List<BindVar>();

      //Condition for all ProviderResourceAspects in the path
      string pathCondition = CreatePathCondition("P.", systemId, basePath, inclusive, bvNamespace, bindVars, false);

      //Condition for all RelationshipAspects in the given hierarchies
      string hierarchyCondition = CreateHierarchyCondition("R.", hierarchies.Where(h => h.IsPrimaryParent), bvNamespace, bindVars);

      IDbCommand result = transaction.CreateCommand();
      result.CommandText =
        "DELETE FROM " + mediaItemTable +
        " WHERE " + mediaItemIdAttribute + " IN(" +
          " SELECT P." + mediaItemIdAttribute + " FROM " + providerTable + " P " +
          " WHERE (" + pathCondition + ")" +
          " AND P." + mediaItemIdAttribute + " NOT IN(" +
            " SELECT " + mediaItemIdAttribute + " FROM " + relationshipTable + " R" +
            " WHERE " + hierarchyCondition +
          ")" +
        ")";

      //Add the bind vars to the command
      AddCommandParameters(transaction.Database, result, bindVars);

      return result;
    }

    /// <summary>
    /// Command to delete all parent media items that don't have any non virtual children in a path other than the specified <paramref name="basePath"/>.
    /// </summary>
    /// <param name="transaction"></param>
    /// <param name="hierarchies"></param>
    /// <param name="systemId"></param>
    /// <param name="basePath"></param>
    /// <param name="inclusive"></param>
    /// <returns></returns>
    protected IDbCommand DeleteOrphanedMediaItemsCommand(ITransaction transaction, IEnumerable<RelationshipType> hierarchies, string systemId, ResourcePath basePath, bool inclusive)
    {
      string mediaItemTable = MediaLibrary_SubSchema.MEDIA_ITEMS_TABLE_NAME;
      string providerTable = _miaManagement.GetMIATableName(ProviderResourceAspect.Metadata);
      string relationshipTable = _miaManagement.GetMIATableName(RelationshipAspect.Metadata);
      string mediaTable = _miaManagement.GetMIATableName(MediaAspect.Metadata);
      string isVirtualAttribute = _miaManagement.GetMIAAttributeColumnName(MediaAspect.ATTR_ISVIRTUAL);
      string mediaItemIdAttribute = MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME;
      string linkedIdAttribute = _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_LINKED_ID);
      string resourceTypeAttribute = _miaManagement.GetMIAAttributeColumnName(ProviderResourceAspect.ATTR_TYPE);

      BindVarNamespace bvNamespace = new BindVarNamespace();
      IList<BindVar> bindVars = new List<BindVar>();

      //Condition for all ProviderResourceAspects in the path
      string pathCondition = CreatePathCondition("P.", systemId, basePath, inclusive, bvNamespace, bindVars, false);

      //Condition for all ProviderResourceAspects not in the path
      string notPathCondition = CreatePathCondition("P.", systemId, basePath, inclusive, bvNamespace, bindVars, true);

      //Condition for all RelationshipAspects in the given hierarchies
      string hierarchyCondition = CreateHierarchyCondition("R.", hierarchies.Where(h => h.IsPrimaryParent), bvNamespace, bindVars);

      IDbCommand result = transaction.CreateCommand();
      result.CommandText =
        "DELETE FROM " + mediaItemTable + " WHERE " + mediaItemIdAttribute + " IN(" +
          " SELECT P." + mediaItemIdAttribute + " FROM " + providerTable + " P" +
          " INNER JOIN " + relationshipTable + " R ON P." + mediaItemIdAttribute + " = R." + mediaItemIdAttribute +
          " WHERE (P." + resourceTypeAttribute + " = " + ProviderResourceAspect.TYPE_VIRTUAL + " OR (" + pathCondition + "))" +
          " AND (" + hierarchyCondition + ")" +
          " AND R." + linkedIdAttribute + " NOT IN (" +
            " SELECT R." + linkedIdAttribute + " FROM " + relationshipTable + " R" +
            " INNER JOIN " + providerTable + " P ON R." + mediaItemIdAttribute + " = P." + mediaItemIdAttribute +
            " INNER JOIN " + mediaTable + " M ON R." + mediaItemIdAttribute + " = M." + mediaItemIdAttribute +
            " WHERE (" + notPathCondition + ")" +
            " AND " + resourceTypeAttribute + " IN(" + StringUtils.Join(", ", _primaryResourceTypes) + ")" +
            " AND M." + isVirtualAttribute + " = 0" +
            " AND(" + hierarchyCondition + ")" +
          ")" +
        ")";

      //Add the bind vars to the command
      AddCommandParameters(transaction.Database, result, bindVars);

      return result;
    }

    /// <summary>
    /// Command to delete all media items that are not a primary child, only have virtual resources and do not have any children.
    /// </summary>
    /// <param name="transaction"></param>
    /// <param name="primaryChildRoles"></param>
    /// <returns></returns>
    protected IDbCommand DeleteOrphanedRelationsCommand(ITransaction transaction, IEnumerable<Guid> primaryChildRoles)
    {
      string mediaItemTable = MediaLibrary_SubSchema.MEDIA_ITEMS_TABLE_NAME;
      string providerTable = _miaManagement.GetMIATableName(ProviderResourceAspect.Metadata);
      string relationshipTable = _miaManagement.GetMIATableName(RelationshipAspect.Metadata);
      string mediaItemIdAttribute = MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME;
      string roleAttribute = _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_ROLE);
      string linkedRoleAttribute = _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_LINKED_ROLE);
      string linkedIdAttribute = _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_LINKED_ID);
      string providerResourceTypeAttribute = _miaManagement.GetMIAAttributeColumnName(ProviderResourceAspect.ATTR_TYPE);

      BindVarNamespace bvNamespace = new BindVarNamespace();
      IList<BindVar> bindVars = new List<BindVar>();

      //List of child roles
      string childRoles = CreateList(primaryChildRoles, bvNamespace, bindVars);

      IDbCommand result = transaction.CreateCommand();
      //Delete all virtual resources that aren't a primary child and don't have any children
      result.CommandText =
        "DELETE FROM " + mediaItemTable + " WHERE " + mediaItemIdAttribute + " IN(" +
          " SELECT P." + mediaItemIdAttribute + " FROM " + providerTable + " P" +
          " LEFT JOIN " + relationshipTable + " R ON P." + mediaItemIdAttribute + " = R." + mediaItemIdAttribute +
          " WHERE P." + providerResourceTypeAttribute + " = " + ProviderResourceAspect.TYPE_VIRTUAL +
          " AND(R." + roleAttribute + " IS NULL OR R." + roleAttribute + " NOT IN(" + childRoles + "))" +
          " AND P." + mediaItemIdAttribute + " NOT IN(" +
            " SELECT " + linkedIdAttribute + " FROM " + relationshipTable +
          ")" +
        ")";

      //Add the bind vars to the command
      AddCommandParameters(transaction.Database, result, bindVars);

      return result;
    }

    /// <summary>
    /// Command to delete all direct relations of the media item with the specified <paramref name="mediaItemId"/> that do not have any other children.
    /// </summary>
    /// <param name="transaction"></param>
    /// <param name="mediaItemId"></param>
    /// <returns></returns>
    protected IDbCommand DeleteOrphanedRelationsCommand(ITransaction transaction, Guid mediaItemId)
    {
      string mediaItemTable = MediaLibrary_SubSchema.MEDIA_ITEMS_TABLE_NAME;
      string providerTable = _miaManagement.GetMIATableName(ProviderResourceAspect.Metadata);
      string relationshipTable = _miaManagement.GetMIATableName(RelationshipAspect.Metadata);
      string mediaItemIdAttribute = MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME;
      string linkedIdAttribute = _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_LINKED_ID);
      string providerResourceTypeAttribute = _miaManagement.GetMIAAttributeColumnName(ProviderResourceAspect.ATTR_TYPE);

      BindVarNamespace bvNamespace = new BindVarNamespace();
      IList<BindVar> bindVars = new List<BindVar>();
      BindVar idVar = new BindVar(bvNamespace.CreateNewBindVarName("MEDIA_ITEM_ID"), mediaItemId, typeof(Guid));
      bindVars.Add(idVar);

      IDbCommand result = transaction.CreateCommand();
      //Delete all relations of the media item that aren't a relation of another media item
      result.CommandText =
        "DELETE FROM " + mediaItemTable + " WHERE " + mediaItemIdAttribute + " IN(" +
          " SELECT P." + mediaItemIdAttribute + " FROM " + providerTable + " P" +
          " INNER JOIN " + relationshipTable + " R ON P." + mediaItemIdAttribute + " = R." + linkedIdAttribute +
          " WHERE P." + mediaItemIdAttribute + " = @" + idVar.Name +
          " AND P." + providerResourceTypeAttribute + " = " + ProviderResourceAspect.TYPE_VIRTUAL +
          " AND R." + linkedIdAttribute + " NOT IN(" +
            " SELECT " + linkedIdAttribute + " FROM " + relationshipTable +
            " WHERE " + mediaItemIdAttribute + " != @" + idVar.Name + 
          ")" +
        ")";

      //Add the bind vars to the command
      AddCommandParameters(transaction.Database, result, bindVars);

      return result;
    }

    /// <summary>
    /// Command to delete all <see cref="RelationshipAspect"/>s from the relationship table where the media item with the id in <see cref="RelationshipAspect.ATTR_LINKED_ID"/>
    /// doesn't exist.
    /// </summary>
    /// <param name="transaction"></param>
    /// <returns></returns>
    protected IDbCommand DeleteOrphanedRelationshipAspectsCommand(ITransaction transaction)
    {
      string mediaItemTable = MediaLibrary_SubSchema.MEDIA_ITEMS_TABLE_NAME;
      string mediaItemIdAttribute = MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME;
      string relationshipTable = _miaManagement.GetMIATableName(RelationshipAspect.Metadata);
      string linkedIdAttribute = _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_LINKED_ID);

      IDbCommand result = transaction.CreateCommand();
      result.CommandText =
        "DELETE FROM " + relationshipTable +
        " WHERE " + linkedIdAttribute + " NOT IN (" +
          " SELECT " + mediaItemIdAttribute + " FROM " + mediaItemTable +
        ")";
      return result; 
    }

    /// <summary>
    /// Command to get role, linked role and linked id of all relationships in the specified <paramref name="hierarchies"/>
    /// of the media item with the specified <paramref name="mediaItemId"/>.
    /// </summary>
    /// <param name="transaction"></param>
    /// <param name="mediaItemId"></param>
    /// <param name="hierarchies"></param>
    /// <returns></returns>
    protected IDbCommand SelectParentsCommand(ITransaction transaction, Guid mediaItemId, IEnumerable<RelationshipType> hierarchies)
    {
      string mediaItemIdAttribute = MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME;
      string relationshipTable = _miaManagement.GetMIATableName(RelationshipAspect.Metadata);
      string roleAttribute = _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_ROLE);
      string linkedRoleAttribute = _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_LINKED_ROLE);
      string linkedIdAttribute = _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_LINKED_ID);

      BindVarNamespace bvNamespace = new BindVarNamespace();
      IList<BindVar> bindVars = new List<BindVar>();

      BindVar childIdVar = new BindVar(bvNamespace.CreateNewBindVarName("CHILD_ID"), mediaItemId, typeof(Guid));
      bindVars.Add(childIdVar);

      //Condition for all RelationshipAspects in the given hierarchies
      string hierarchyCondition = CreateHierarchyCondition(string.Empty, hierarchies, bvNamespace, bindVars);

      IDbCommand result = transaction.CreateCommand();
      result.CommandText =
        "SELECT " + roleAttribute + ", " + linkedRoleAttribute + ", " + linkedIdAttribute +
        " FROM " + relationshipTable +
        " WHERE " + mediaItemIdAttribute + " = @" + childIdVar.Name +
        " AND (" + hierarchyCondition + ")";

      //Add the bind vars to the command
      AddCommandParameters(transaction.Database, result, bindVars);

      return result;
    }

    /// <summary>
    /// Command to get media item id, role, linked role and linked id of all relationships in the specified <paramref name="hierarchies"/>
    /// of the media items with resources in the specified <paramref name="basePath"/>.
    /// </summary>
    /// <param name="transaction"></param>
    /// <param name="hierarchies"></param>
    /// <param name="systemId"></param>
    /// <param name="basePath"></param>
    /// <param name="inclusive"></param>
    /// <returns></returns>
    protected IDbCommand SelectParentsCommand(ITransaction transaction, IEnumerable<RelationshipType> hierarchies, string systemId, ResourcePath basePath, bool inclusive)
    {
      string mediaItemIdAttribute = MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME;
      string providerTable = _miaManagement.GetMIATableName(ProviderResourceAspect.Metadata);
      string relationshipTable = _miaManagement.GetMIATableName(RelationshipAspect.Metadata);
      string roleAttribute = _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_ROLE);
      string linkedRoleAttribute = _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_LINKED_ROLE);
      string linkedIdAttribute = _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_LINKED_ID);

      BindVarNamespace bvNamespace = new BindVarNamespace();
      IList<BindVar> bindVars = new List<BindVar>();

      //Condition for all ProviderResourceAspects in the path
      string pathCondition = CreatePathCondition("P.", systemId, basePath, inclusive, bvNamespace, bindVars, false);

      //Condition for all RelationshipAspects in the given hierarchies
      string hierarchyCondition = CreateHierarchyCondition("R.", hierarchies, bvNamespace, bindVars);

      IDbCommand result = transaction.CreateCommand();
      //Select all children in the specified path and their parents and roles
      result.CommandText =
        "SELECT R." + mediaItemIdAttribute + ", " + roleAttribute + ", " + linkedRoleAttribute + ", " + linkedIdAttribute +
        " FROM " + relationshipTable + " R" +
        " INNER JOIN " + providerTable + " P ON R." + mediaItemIdAttribute + " = P." + mediaItemIdAttribute +
        " WHERE (" + pathCondition + ")" +
        " AND (" + hierarchyCondition + ")";

      //Add the bind vars to the command
      AddCommandParameters(transaction.Database, result, bindVars);

      return result;
    }

    /// <summary>
    /// Command to select all media item id and resource path of children of
    /// the media item with the specified <paramref name="mediaItemId"/> in the specified <paramref name="hierarchies"/>.
    /// </summary>
    /// <param name="transaction"></param>
    /// <param name="mediaItemId"></param>
    /// <param name="hierarchies"></param>
    /// <returns></returns>
    protected IDbCommand SelectChildrenCommand(ITransaction transaction, Guid mediaItemId, IEnumerable<RelationshipType> hierarchies)
    {
      string mediaItemIdAttribute = MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME;
      string relationshipTable = _miaManagement.GetMIATableName(RelationshipAspect.Metadata);
      string linkedIdAttribute = _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_LINKED_ID);
      string mediaTable = _miaManagement.GetMIATableName(MediaAspect.Metadata);
      string isVirtualAttribute = _miaManagement.GetMIAAttributeColumnName(MediaAspect.ATTR_ISVIRTUAL);

      BindVarNamespace bvNamespace = new BindVarNamespace();
      IList<BindVar> bindVars = new List<BindVar>();

      //Condition for all RelationshipAspects in the given hierarchies
      string hierarchyCondition = CreateHierarchyCondition("R.", hierarchies, bvNamespace, bindVars);

      //Parent id
      BindVar parentIdVar = new BindVar(bvNamespace.CreateNewBindVarName("PARENT_ID"), mediaItemId, typeof(Guid));
      bindVars.Add(parentIdVar);

      IDbCommand result = transaction.CreateCommand();
      //Select all child user play data
      result.CommandText =
        "SELECT M." + mediaItemIdAttribute + 
        " FROM " + mediaTable + " M" +
        " INNER JOIN " + relationshipTable + " R ON R." + mediaItemIdAttribute + " = M." + mediaItemIdAttribute +
        " WHERE M." + isVirtualAttribute + " = 0" +
        " AND R." + linkedIdAttribute + " = @" + parentIdVar.Name +
        " AND (" + hierarchyCondition + ")";

      //Add the bind vars to the command
      AddCommandParameters(transaction.Database, result, bindVars);

      return result;
    }

    /// <summary>
    /// Deletes all resources in the specified <paramref name="basePath"/>.
    /// </summary>
    /// <param name="transaction"></param>
    /// <param name="systemId"></param>
    /// <param name="basePath"></param>
    /// <param name="inclusive"></param>
    /// <returns></returns>
    protected IDbCommand DeleteResourcePathsCommand(ITransaction transaction, string systemId, ResourcePath basePath, bool inclusive)
    {
      string providerTable = _miaManagement.GetMIATableName(ProviderResourceAspect.Metadata);

      BindVarNamespace bvNamespace = new BindVarNamespace();
      IList<BindVar> bindVars = new List<BindVar>();

      //Condition for all ProviderResourceAspects in the path
      string pathCondition = CreatePathCondition(string.Empty, systemId, basePath, inclusive, bvNamespace, bindVars, false);

      IDbCommand result = transaction.CreateCommand();
      result.CommandText = "DELETE FROM " + providerTable + " WHERE " + pathCondition;

      //Add the bind vars to the command
      AddCommandParameters(transaction.Database, result, bindVars);

      return result;
    }

    /// <summary>
    /// Command to select all media item id, user profile id, user play count and global play count of children of
    /// the media item with the specified <paramref name="mediaItemId"/> in the specified <paramref name="hierarchy"/>.
    /// </summary>
    /// <param name="transaction"></param>
    /// <param name="mediaItemId"></param>
    /// <param name="hierarchy"></param>
    /// <returns></returns>
    protected IDbCommand SelectChildrenAndPlayCountCommand(ITransaction transaction, Guid mediaItemId, RelationshipType hierarchy)
    {
      string mediaItemIdAttribute = MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME;
      string relationshipTable = _miaManagement.GetMIATableName(RelationshipAspect.Metadata);
      string linkedIdAttribute = _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_LINKED_ID);
      string mediaTable = _miaManagement.GetMIATableName(MediaAspect.Metadata);
      string isVirtualAttribute = _miaManagement.GetMIAAttributeColumnName(MediaAspect.ATTR_ISVIRTUAL);
      string playCountAttribute = _miaManagement.GetMIAAttributeColumnName(MediaAspect.ATTR_PLAYCOUNT);
      string userProfileTable = UserProfileDataManagement_SubSchema.USER_MEDIA_ITEM_DATA_TABLE_NAME;
      string userProfileIdAttribute = UserProfileDataManagement_SubSchema.USER_PROFILE_ID_COL_NAME;
      string userDataKeyAttribute = UserProfileDataManagement_SubSchema.USER_DATA_KEY_COL_NAME;
      string userDataValueAttribute = UserProfileDataManagement_SubSchema.USER_DATA_VALUE_COL_NAME;

      BindVarNamespace bvNamespace = new BindVarNamespace();
      IList<BindVar> bindVars = new List<BindVar>();

      //Condition for all RelationshipAspects in the given hierarchy
      string hierarchyCondition = CreateHierarchyCondition("R.", hierarchy, bvNamespace, bindVars);

      //Parent id
      BindVar parentIdVar = new BindVar(bvNamespace.CreateNewBindVarName("PARENT_ID"), mediaItemId, typeof(Guid));
      bindVars.Add(parentIdVar);

      //User play count
      BindVar userPlayCountVar = new BindVar(bvNamespace.CreateNewBindVarName("PLAY_COUNT"), UserDataKeysKnown.KEY_PLAY_COUNT, typeof(string));
      bindVars.Add(userPlayCountVar);

      string childId;
      string additionalAttributeJoin;
      if (hierarchy.ChildCountAttribute != null && hierarchy.ChildCountAttribute.IsCollectionAttribute)
      {
        string childAttributeTable = hierarchy.ChildCountAttribute.Cardinality == Cardinality.ManyToMany ?
          _miaManagement.GetMIACollectionAttributeNMTableName(hierarchy.ChildCountAttribute) :
          _miaManagement.GetMIACollectionAttributeTableName(hierarchy.ChildCountAttribute);

        childId = "CA." + MIA_Management.FOREIGN_COLL_ATTR_ID_COL_NAME;
        additionalAttributeJoin = " INNER JOIN " + childAttributeTable + " CA ON CA." + mediaItemIdAttribute + " = M." + mediaItemIdAttribute;
      }
      else
      {
        childId = "M." + mediaItemIdAttribute;
        additionalAttributeJoin = string.Empty;
      }

      IDbCommand result = transaction.CreateCommand();
      //Select all child user play data
      result.CommandText =
        "SELECT M." + mediaItemIdAttribute + ", " + childId + ", U." + userProfileIdAttribute + ", U." + userDataValueAttribute + ", M." + playCountAttribute +
        " FROM " + mediaTable + " M" +
        " INNER JOIN " + relationshipTable + " R ON R." + mediaItemIdAttribute + " = M." + mediaItemIdAttribute +
        additionalAttributeJoin +
        " LEFT JOIN " + userProfileTable + " U ON M." + mediaItemIdAttribute + " = U." + mediaItemIdAttribute +
        " WHERE M." + isVirtualAttribute + " = 0" +
        " AND R." + linkedIdAttribute + " = @" + parentIdVar.Name +
        " AND (" + hierarchyCondition + ")" +
        " AND (U." + userProfileIdAttribute + " IS NULL OR U." + userDataKeyAttribute + " = @" + userPlayCountVar.Name + ")";

      //Add the bind vars to the command
      AddCommandParameters(transaction.Database, result, bindVars);

      return result;
    }

    /// <summary>
    /// Command to update the available items count of the parent item of the specified <paramref name="hierarchy"/> with the specified <paramref name="mediaItemId"/>.
    /// </summary>
    /// <param name="transaction"></param>
    /// <param name="mediaItemId"></param>
    /// <param name="hierarchy"></param>
    /// <param name="availableCount"></param>
    /// <returns></returns>
    protected IDbCommand UpdateAvailableCountCommand(ITransaction transaction, Guid mediaItemId, RelationshipType hierarchy, int availableCount)
    {
      string mediaItemIdAttribute = MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME;
      string parentTable = _miaManagement.GetMIATableName(hierarchy.ParentCountAttribute.ParentMIAM);
      string parentCountAttribute = _miaManagement.GetMIAAttributeColumnName(hierarchy.ParentCountAttribute);

      List<BindVar> bindVars = new List<BindVar>();
      BindVar parentIdVar = new BindVar("PARENT_ID", mediaItemId, typeof(Guid));
      bindVars.Add(parentIdVar);
      BindVar childCountVar = new BindVar("CHILD_COUNT", availableCount, typeof(int));
      bindVars.Add(childCountVar);

      IDbCommand result = transaction.CreateCommand();
      //Update the available item attribute
      result.CommandText =
        "UPDATE " + parentTable + " SET " + parentCountAttribute + " = @" + childCountVar.Name +
        " WHERE " + mediaItemIdAttribute + " = @" + parentIdVar.Name;

      //Add the bind vars to the command
      AddCommandParameters(transaction.Database, result, bindVars);

      return result;
    }

    /// <summary>
    /// Command to update the user play percentage of the parent item with the specified <paramref name="mediaItemId"/>.
    /// </summary>
    /// <param name="transaction"></param>
    /// <param name="mediaItemId"></param>
    /// <param name="userProfileId"></param>
    /// <param name="playPercentage"></param>
    /// <returns></returns>
    protected IDbCommand UpdateUserPlayPercentageCommand(ITransaction transaction, Guid mediaItemId, Guid userProfileId, int playPercentage)
    {
      string mediaItemIdAttribute = MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME;
      string userProfileTable = UserProfileDataManagement_SubSchema.USER_MEDIA_ITEM_DATA_TABLE_NAME;
      string userProfileIdAttribute = UserProfileDataManagement_SubSchema.USER_PROFILE_ID_COL_NAME;
      string userDataKeyAttribute = UserProfileDataManagement_SubSchema.USER_DATA_KEY_COL_NAME;
      string userDataValueAttribute = UserProfileDataManagement_SubSchema.USER_DATA_VALUE_COL_NAME;

      IList<BindVar> bindVars = new List<BindVar>();
      BindVar userProfileIdVar = new BindVar("USER_PROFILE_ID", userProfileId, typeof(Guid));
      bindVars.Add(userProfileIdVar);
      BindVar parentIdVar = new BindVar("PARENT_ID", mediaItemId, typeof(Guid));
      bindVars.Add(parentIdVar);
      BindVar userPlayPercentageVar = new BindVar("PLAY_PERCENTAGE", UserDataKeysKnown.KEY_PLAY_PERCENTAGE, typeof(string));
      bindVars.Add(userPlayPercentageVar);

      IDbCommand result = transaction.CreateCommand();
      //Update the play percentage user profile data
      result.CommandText =
        "UPDATE " + userProfileTable + " SET " + userDataValueAttribute + " = " + playPercentage +
        " WHERE " + userProfileIdAttribute + " = @" + userProfileIdVar.Name +
        " AND " + mediaItemIdAttribute + " = @" + parentIdVar.Name +
        " AND " + userDataKeyAttribute + " = @" + userPlayPercentageVar.Name;

      //Add the bind vars to the command
      AddCommandParameters(transaction.Database, result, bindVars);

      return result;
    }

    /// <summary>
    /// Command to insert a new virtual resource in the media item with the specified <paramref name="mediaItemId"/>.
    /// </summary>
    /// <param name="transaction"></param>
    /// <param name="mediaItemId"></param>
    /// <returns></returns>
    protected IDbCommand InsertNewVirtualResourceCommand(ITransaction transaction, Guid mediaItemId)
    {
      string mediaItemIdAttribute = MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME;
      string providerTable = _miaManagement.GetMIATableName(ProviderResourceAspect.Metadata);
      string mediaTable = _miaManagement.GetMIATableName(MediaAspect.Metadata);
      string resourcePathAttribute = _miaManagement.GetMIAAttributeColumnName(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);
      string resourceTypeAttribute = _miaManagement.GetMIAAttributeColumnName(ProviderResourceAspect.ATTR_TYPE);
      string resourceIndexAttribute = _miaManagement.GetMIAAttributeColumnName(ProviderResourceAspect.ATTR_RESOURCE_INDEX);
      string systemIdAttribute = _miaManagement.GetMIAAttributeColumnName(ProviderResourceAspect.ATTR_SYSTEM_ID);
      string parentDirectoryIdAttribute = _miaManagement.GetMIAAttributeColumnName(ProviderResourceAspect.ATTR_PARENT_DIRECTORY_ID);
      string isVirtualAttribute = _miaManagement.GetMIAAttributeColumnName(MediaAspect.ATTR_ISVIRTUAL);

      IList<BindVar> bindVars = new List<BindVar>();
      BindVar itemIdVar = new BindVar("ITEM_ID", mediaItemId, typeof(Guid));
      bindVars.Add(itemIdVar);
      BindVar virtualPathVar = new BindVar("VIRT_PATH", VirtualResourceProvider.ToResourcePath(mediaItemId).Serialize(), typeof(string));
      bindVars.Add(virtualPathVar);
      BindVar localSystemVar = new BindVar("LOCAL_SYSTEM", _virtualItemSystemId, typeof(string));
      bindVars.Add(localSystemVar);
      BindVar parentDirVar = new BindVar("PARENT_DIR", Guid.Empty, typeof(Guid));
      bindVars.Add(parentDirVar);

      //Insert a new virtual resource path
      string commandStr =
        "INSERT INTO " + providerTable + " (" +
        mediaItemIdAttribute + ", " +
        resourcePathAttribute + ", " +
        resourceTypeAttribute + ", " +
        resourceIndexAttribute + ", " +
        systemIdAttribute + ", " +
        parentDirectoryIdAttribute +
        ") VALUES (@" + itemIdVar.Name + ", @" + virtualPathVar.Name + ", " + ProviderResourceAspect.TYPE_VIRTUAL +
        ", 0, @" + localSystemVar.Name + ", @" + parentDirVar.Name + ");";

      //Set the IsVirtual attribute
      commandStr += " UPDATE " + mediaTable + " SET " + isVirtualAttribute + " = 1 WHERE " + mediaItemIdAttribute + " = @" + itemIdVar.Name;

      IDbCommand result = transaction.CreateCommand();
      result.CommandText = commandStr;

      //Add the bind vars to the command
      AddCommandParameters(transaction.Database, result, bindVars);

      return result;
    }

    /// <summary>
    /// Updates the IsVirtual flag of the media item with the specified <paramref name="mediaItemId"/>.
    /// </summary>
    /// <param name="transaction"></param>
    /// <param name="mediaItemId"></param>
    /// <param name="isVirtual"></param>
    /// <returns></returns>
    protected IDbCommand UpdateIsVirtualCommand(ITransaction transaction, Guid mediaItemId, bool isVirtual)
    {
      string mediaItemIdAttribute = MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME;
      string mediaTable = _miaManagement.GetMIATableName(MediaAspect.Metadata);
      string isVirtualAttribute = _miaManagement.GetMIAAttributeColumnName(MediaAspect.ATTR_ISVIRTUAL);

      IList<BindVar> bindVars = new List<BindVar>();
      BindVar parentIdVar = new BindVar("PARENT_ID", mediaItemId, typeof(Guid));
      bindVars.Add(parentIdVar);
      BindVar isVirtualVar = new BindVar("IS_VIRTUAL", isVirtual ? 1 : 0, typeof(int));
      bindVars.Add(isVirtualVar);

      IDbCommand result = transaction.CreateCommand();
      result.CommandText =
        "UPDATE " + mediaTable + " SET " + isVirtualAttribute + " = @" + isVirtualVar.Name +
        " WHERE " + mediaItemIdAttribute + "= @" + parentIdVar.Name + " AND " + isVirtualAttribute + " != @" + isVirtualVar.Name;

      AddCommandParameters(transaction.Database, result, bindVars);

      return result;
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Creates or updates the list of hierarchies for the specified parent.
    /// </summary>
    /// <param name="parentId"></param>
    /// <param name="hierarchy"></param>
    /// <param name="parentHierarchy"></param>
    protected void AddParentHierarchy(Guid parentId, RelationshipType hierarchy, IDictionary<Guid, ICollection<RelationshipType>> parentHierarchy)
    {
      ICollection<RelationshipType> hierarchies;
      if (!parentHierarchy.TryGetValue(parentId, out hierarchies))
        hierarchies = parentHierarchy[parentId] = new HashSet<RelationshipType>();
      hierarchies.Add(hierarchy);
    }

    protected ICollection<RelationshipType> GetHierarchyParents(RelationshipType childHierarchy, IEnumerable<RelationshipType> hierarchies)
    {
      return hierarchies.Where(h => h.ChildRole == childHierarchy.ParentRole).ToList();
    }

    /// <summary>
    /// Creates or updates the played count for the specified user profile.
    /// </summary>
    /// <param name="userProfileId"></param>
    /// <param name="played"></param>
    /// <param name="playData"></param>
    protected void IncrementPlayedCount(Guid userProfileId, bool played, Dictionary<Guid, int> playData)
    {
      if (!playData.ContainsKey(userProfileId))
        playData.Add(userProfileId, played ? 1 : 0);
      else if (played)
        playData[userProfileId] = playData[userProfileId] + 1;
    }

    /// <summary>
    /// Clears any existing parameters for the command and adds parameters for the specified bindVars.
    /// </summary>
    /// <param name="database"></param>
    /// <param name="command"></param>
    /// <param name="bindVars"></param>
    protected void AddCommandParameters(ISQLDatabase database, IDbCommand command, IList<BindVar> bindVars)
    {
      command.Parameters.Clear();
      foreach (BindVar bindVar in bindVars)
        database.AddParameter(command, bindVar.Name, bindVar.Value, bindVar.VariableType);
    }

    /// <summary>
    /// Creates a comma separated list of bindings.
    /// </summary>
    /// <param name="items"></param>
    /// <param name="bvNamespace"></param>
    /// <param name="bindVars"></param>
    /// <returns></returns>
    protected string CreateList<T>(IEnumerable<T> items, BindVarNamespace bvNamespace, IList<BindVar> bindVars)
    {
      if (!items.Any())
        return "''";

      IList<string> roleVars = new List<string>();
      foreach (T item in items)
      {
        BindVar roleVar = new BindVar(bvNamespace.CreateNewBindVarName("ITEM_V"), item, typeof(T));
        bindVars.Add(roleVar);
        roleVars.Add("@" + roleVar.Name);
      }
      return StringUtils.Join(", ", roleVars);
    }

    /// <summary>
    /// Creates a condition for RelationshipAspects in the specified hierarchies.
    /// </summary>
    /// <param name="hierarchies"></param>
    /// <param name="bvNamespace"></param>
    /// <param name="bindVars"></param>
    /// <returns></returns>
    protected string CreateHierarchyCondition(string relationshipTableAlias, IEnumerable<RelationshipType> hierarchies,
      BindVarNamespace bvNamespace, IList<BindVar> bindVars)
    {
      if (!hierarchies.Any())
        return "1 = 1";

      IList<string> hierarchyVars = new List<string>();
      foreach (RelationshipType hierarchy in hierarchies)
        hierarchyVars.Add(CreateHierarchyCondition(relationshipTableAlias, hierarchy, bvNamespace, bindVars));
      return StringUtils.Join(" OR ", hierarchyVars);
    }

    /// <summary>
    /// Creates a condition for RelationshipAspects in the specified hierarchy.
    /// </summary>
    /// <param name="hierarchy"></param>
    /// <param name="bvNamespace"></param>
    /// <param name="bindVars"></param>
    /// <returns></returns>
    protected string CreateHierarchyCondition(string relationshipTableAlias, RelationshipType hierarchy,
      BindVarNamespace bvNamespace, IList<BindVar> bindVars)
    {
      string roleAttribute = relationshipTableAlias + _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_ROLE);
      string linkedRoleAttribute = relationshipTableAlias + _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_LINKED_ROLE);
      BindVar childVar = new BindVar(bvNamespace.CreateNewBindVarName("CHILDV"), hierarchy.ChildRole, typeof(Guid));
      bindVars.Add(childVar);
      BindVar parentVar = new BindVar(bvNamespace.CreateNewBindVarName("PARENTV"), hierarchy.ParentRole, typeof(Guid));
      bindVars.Add(parentVar);
      return string.Format("({0} = @{1} AND {2} = @{3})", roleAttribute, childVar.Name, linkedRoleAttribute, parentVar.Name);
    }

    /// <summary>
    /// Creates a condition for ProviderResourceAspects [not] in the specified path.
    /// </summary>
    /// <param name="systemId"></param>
    /// <param name="basePath"></param>
    /// <param name="inclusive"></param>
    /// <param name="bvNamespace"></param>
    /// <param name="bindVars"></param>
    /// <param name="notInPath"></param>
    /// <returns></returns>
    protected string CreatePathCondition(string providerTableAlias, string systemId, ResourcePath basePath, bool inclusive,
      BindVarNamespace bvNamespace, IList<BindVar> bindVars, bool notInPath)
    {
      BindVar systemIdVar = new BindVar(bvNamespace.CreateNewBindVarName("SYSTEM_ID"), systemId, typeof(string));
      bindVars.Add(systemIdVar);

      BindVar exactPathVar = null;
      BindVar likePath1Var = null;
      BindVar likeEscape1Var = null;
      BindVar likePath2Var = null;
      BindVar likeEscape2Var = null;
      if (basePath != null)
      {
        string path = StringUtils.RemoveSuffixIfPresent(basePath.Serialize(), "/");
        string escapedPath = SqlUtils.LikeEscape(path, ESCAPE_CHAR);

        if (inclusive)
        {
          // The path itself
          exactPathVar = new BindVar(bvNamespace.CreateNewBindVarName("EXACT_PATH"), path, typeof(string));
          bindVars.Add(exactPathVar);
          // Normal children and, if escapedPath ends with "/", the directory itself
          likePath1Var = new BindVar(bvNamespace.CreateNewBindVarName("LIKE_PATH1"), escapedPath + "/%", typeof(string));
          bindVars.Add(likePath1Var);
        }
        else
        {
          // Normal children, in any case excluding the escaped path, even if it is a directory which ends with "/"
          likePath1Var = new BindVar(bvNamespace.CreateNewBindVarName("LIKE_PATH1"), escapedPath + "/_%", typeof(string));
          bindVars.Add(likePath1Var);
        }

        // Chained children
        likeEscape1Var = new BindVar(bvNamespace.CreateNewBindVarName("LIKE_ESCAPE1"), ESCAPE_CHAR, typeof(char));
        bindVars.Add(likeEscape1Var);
        likePath2Var = new BindVar(bvNamespace.CreateNewBindVarName("LIKE_PATH2"), escapedPath + ">_%", typeof(string));
        bindVars.Add(likePath2Var);
        likeEscape2Var = new BindVar(bvNamespace.CreateNewBindVarName("LIKE_ESCAPE2"), ESCAPE_CHAR, typeof(char));
        bindVars.Add(likeEscape2Var);
      }

      if (notInPath)
        return BuildNotPathStatement(providerTableAlias, systemIdVar, exactPathVar, likePath1Var, likeEscape1Var, likePath2Var, likeEscape2Var);
      else
        return BuildPathStatement(providerTableAlias, systemIdVar, exactPathVar, likePath1Var, likeEscape1Var, likePath2Var, likeEscape2Var);
    }

    protected string BuildPathStatement(string providerTableAlias, BindVar systemIdVar, BindVar exactPathVar, BindVar likePath1Var,
      BindVar likeEscape1Var, BindVar likePath2Var, BindVar likeEscape2Var)
    {
      string systemIdAttribute = _miaManagement.GetMIAAttributeColumnName(ProviderResourceAspect.ATTR_SYSTEM_ID);
      string pathAttribute = _miaManagement.GetMIAAttributeColumnName(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);
      string commandStr = systemIdAttribute + " = @" + systemIdVar.Name;
      if (likePath1Var != null)
      {
        commandStr += " AND (";
        if (exactPathVar != null)
          commandStr += pathAttribute + " = @" + exactPathVar.Name + " OR ";
        commandStr +=
            pathAttribute + " LIKE @" + likePath1Var.Name + " ESCAPE @" + likeEscape1Var.Name + " OR " +
            pathAttribute + " LIKE @" + likePath2Var.Name + " ESCAPE @" + likeEscape2Var.Name +
            ")";
      }
      return commandStr;
    }

    protected string BuildNotPathStatement(string providerTableAlias, BindVar systemIdVar, BindVar exactPathVar, BindVar likePath1Var,
      BindVar likeEscape1Var, BindVar likePath2Var, BindVar likeEscape2Var)
    {
      string systemIdAttribute = providerTableAlias + _miaManagement.GetMIAAttributeColumnName(ProviderResourceAspect.ATTR_SYSTEM_ID);
      string pathAttribute = providerTableAlias + _miaManagement.GetMIAAttributeColumnName(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);
      string commandStr = systemIdAttribute + " != @" + systemIdVar.Name;
      if (likePath1Var != null)
      {
        commandStr += " OR (";
        if (exactPathVar != null)
          commandStr += pathAttribute + " != @" + exactPathVar.Name + " AND ";
        commandStr +=
            pathAttribute + " NOT LIKE @" + likePath1Var.Name + " ESCAPE @" + likeEscape1Var.Name + " AND " +
            pathAttribute + " NOT LIKE @" + likePath2Var.Name + " ESCAPE @" + likeEscape2Var.Name +
            ")";
      }
      return commandStr;
    }

    #endregion

    #region Logger

    protected ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }

    #endregion
  }
}
