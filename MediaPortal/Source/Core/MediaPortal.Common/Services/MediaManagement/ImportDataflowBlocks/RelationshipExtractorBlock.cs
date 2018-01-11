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

using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace MediaPortal.Common.Services.MediaManagement.ImportDataflowBlocks
{
  /// <summary>
  /// Takes MediaItems, extracts their relations, adds RelationshipAspects and reconciles the extracted relations in the media library.
  /// </summary>
  /// <remarks>
  /// This DataflowBlock expects that the <see cref="PendingImportResourceNewGen"/>s passed to it have a valid media item id.
  /// </remarks>
  class RelationshipExtractorBlock : ImporterWorkerDataflowBlockBase
  {
    protected class ExtractedRelation
    {
      public ExtractedRelation(IRelationshipRoleExtractor extractor, IDictionary<Guid, IList<MediaItemAspect>> aspects)
      {
        Extractor = extractor;
        Aspects = aspects;
      }

      public IRelationshipRoleExtractor Extractor { get; protected set; }
      public IDictionary<Guid, IList<MediaItemAspect>> Aspects { get; protected set; }
    }

    #region Consts

    public const String BLOCK_NAME = "RelationshipExtractorBlock";

    protected static readonly IEnumerable<Guid> RECONCILE_MIA_ID_ENUMERATION = new[]
      {
        MediaAspect.ASPECT_ID,
        RelationshipAspect.ASPECT_ID
      };

    #endregion

    #region Variables

    protected SemaphoreSlim _cacheSync;
    protected RelationshipCache _relationshipCache;
    protected CancellationToken _ct;

    #endregion

    #region Constructor

    /// <summary>
    /// Initiates the RelationshipExtractorBlock
    /// </summary>
    /// <remarks>
    /// The preceding MediaItemSaveBlock has a BoundedCapacity. To avoid that this limitation does not have any effect
    /// because all the items are immediately passed to an unbounded InputBlock of this RelationshipExtractorBlock, we
    /// have to set the BoundedCapacity of the InputBlock to 1. The BoundedCapacity of the InnerBlock is set to 500,
    /// which is a good trade-off between speed and memory usage. The OutputBlock disposes the PendingImportResources and 
    /// therefore does not need a BoundedCapacity.
    /// </remarks>
    /// <param name="ct">CancellationToken used to cancel this DataflowBlock</param>
    /// <param name="importJobInformation"><see cref="ImportJobInformation"/> of the ImportJob this DataflowBlock belongs to</param>
    /// <param name="parentImportJobController">ImportJobController to which this DataflowBlock belongs</param>
    public RelationshipExtractorBlock(CancellationToken ct, ImportJobInformation importJobInformation, ImportJobController parentImportJobController)
      : base(importJobInformation,
      new ExecutionDataflowBlockOptions { CancellationToken = ct, BoundedCapacity = 1 },
      new ExecutionDataflowBlockOptions { CancellationToken = ct, MaxDegreeOfParallelism = Environment.ProcessorCount * 5, BoundedCapacity = 50 },
      new ExecutionDataflowBlockOptions { CancellationToken = ct },
      BLOCK_NAME, true, parentImportJobController)
    {
      _relationshipCache = new RelationshipCache();
      _ct = ct;
      _cacheSync = new SemaphoreSlim(1, 1);
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Main process method for the InnerBlock
    /// </summary>
    /// <remarks>
    /// Extracts relationships for the media item and adds them to the media library.
    /// </remarks>
    /// <param name="importResource"><see cref="PendingImportResourceNewGen"/> to be processed</param>
    /// <returns><see cref="PendingImportResourceNewGen"/> after processing</returns>
    private async Task<PendingImportResourceNewGen> ProcessMediaItem(PendingImportResourceNewGen importResource)
    {
      try
      {
        //Check if we can extract relations for this import resource
        if (await ValidateImportResource(importResource))
        {
          //Try to cache the resource as a matching item might get extracted later, e.g. the SeriesEpisodeExtractor
          //might extract a matching episode and we should avoid processing the item again.
          //If CacheImportResource returns false then an item with the same media item id has already been cached/processed
          //so we can just ignore it, this can happen if an external subtitle is merged into an existing media item and therefore
          //has the same media item id as the existing item.
          if (await CacheImportResource(importResource))
            await ExtractRelationships(importResource.MediaItemId.Value, importResource.Aspects);
        }

        importResource.IsValid = false;
        return importResource;
      }
      catch (OperationCanceledException)
      {
        return importResource;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Warn("ImporterWorker.{0}.{1}: Error while processing {2}", ex, ParentImportJobController, ToString(), importResource);
        importResource.IsValid = false;
        return importResource;
      }
    }

    /// <summary>
    /// Determines whether the specified <paramref name="importResource"/> has a valid media item id, and in the case
    /// of an import resource restored from disk attempts to load its aspects from the media library.
    /// </summary>
    /// <param name="importResource">The <see cref="PendingImportResourceNewGen"/> to validate.</param>
    /// <returns>True if the import resource has a valid media item id and aspects.</returns>
    private async Task<bool> ValidateImportResource(PendingImportResourceNewGen importResource)
    {
      //It's possible for the media item id to be empty, particularly in the case of subtitles where no mergable media item
      //was found in the database. Don't process the item if that is the case
      if (!importResource.MediaItemId.HasValue || importResource.MediaItemId.Value == Guid.Empty)
        return false;

      //Aspects will be null if this import resource was restored from disk, try and load the aspects from the DB
      if (importResource.Aspects == null)
      {
        MediaItem loadItem = await LoadLocalItem(importResource.MediaItemId.Value, null, await GetAllManagedMediaItemAspectTypes());
        if (loadItem == null)
          return false;
        importResource.Aspects = loadItem.Aspects;
      }
      return true;
    }

    #endregion

    #region Protected methods

    /// <summary>
    /// Extracts all relationships for the <paramref name="mediaItem"/> and updates the MediaLibrary.
    /// </summary>
    /// <param name="mediaItemId">The id of the media item.</param>
    /// <param name="aspects">The aspects of the media item.</param>
    /// <returns></returns>
    protected async Task ExtractRelationships(Guid mediaItemId, IDictionary<Guid, IList<MediaItemAspect>> aspects)
    {
      //Get the relations
      ICollection<ExtractedRelation> relations = await ExtractRelationshipMetadata(mediaItemId, aspects);

      if (relations.Count == 0)
        return;

      //Update the item and add any new relations to the database
      ICollection<MediaItem> updatedMediaItems = await ReconcileRelationships(mediaItemId, aspects, relations);

      //Extract relationships for any new relations
      if (updatedMediaItems != null && updatedMediaItems.Count > 0)
        await Task.WhenAll(updatedMediaItems.Select(i => ExtractChildRelationships(i.MediaItemId, i.Aspects)));
    }

    /// <summary>
    /// Extracts relationships and additionally swallows TaskCancelledExceptions.
    /// </summary>
    /// <remarks>
    /// Multiple tasks created by this method will be awaited with Task.WhenAll. To avoid that an AggregateException
    /// is thrown with a OperationCanceledException for each task when an import is cancelled we swallow the OperationCanceledException,
    /// The exception is still thrown in the parent task where it is handled appropriately.
    /// </remarks>
    /// <param name="mediaItemId">The id of the media item.</param>
    /// <param name="aspects">The aspects of the media item.</param>
    /// <returns></returns>
    protected async Task ExtractChildRelationships(Guid mediaItemId, IDictionary<Guid, IList<MediaItemAspect>> aspects)
    {
      try
      {
        await ExtractRelationships(mediaItemId, aspects);
      }
      catch (OperationCanceledException)
      {
        //Operation cancelled is handled by the parent task, we don't want to
        //throw it again from child tasks
      }
    }

    /// <summary>
    /// Extracts relations from all <see cref="IRelationshipRoleExtractor"/>s that support the given aspects.
    /// </summary>
    /// <param name="mediaItemId">The id of the media item.</param>
    /// <param name="aspects">The aspects of the media item.</param>
    /// <returns></returns>
    protected async Task<ICollection<ExtractedRelation>> ExtractRelationshipMetadata(Guid mediaItemId, IDictionary<Guid, IList<MediaItemAspect>> aspects)
    {
      ICollection<ExtractedRelation> relations = new List<ExtractedRelation>();
      foreach (IRelationshipRoleExtractor extractor in GetRoleExtractors(aspects))
        await ExtractRelationshipMetadata(extractor, mediaItemId, aspects, relations);
      return relations;
    }

    /// <summary>
    /// Extracts relations for the specified <paramref name="roleExtractor"/> and adds them to <paramref name="relations"/> collection.
    /// </summary>
    /// <param name="roleExtractor">The extractor to use to extract relations.</param>
    /// <param name="mediaItemId">The id of the media item.</param>
    /// <param name="aspects">The aspects of the media item.</param>
    /// <param name="relations">Collection of relations to add any extracted relations.</param>
    protected async Task ExtractRelationshipMetadata(IRelationshipRoleExtractor roleExtractor, Guid mediaItemId, IDictionary<Guid, IList<MediaItemAspect>> aspects, ICollection<ExtractedRelation> relations)
    {
      int extractedCount = 0;
      IList<IDictionary<Guid, IList<MediaItemAspect>>> extractedItems = new List<IDictionary<Guid, IList<MediaItemAspect>>>();
      if (await roleExtractor.TryExtractRelationshipsAsync(aspects, extractedItems))
      {
        extractedCount = extractedItems.Count;
        foreach (IDictionary<Guid, IList<MediaItemAspect>> extractedItem in extractedItems)
          relations.Add(new ExtractedRelation(roleExtractor, extractedItem));
      }

      ServiceRegistration.Get<ILogger>().Debug("Extractor {0} extracted {1} media items from media item {2}", roleExtractor.GetType().Name, extractedCount, mediaItemId);
    }

    /// <summary>
    /// Reconciles all extracted relations either from the cache or at the MediaLibrary and adds
    /// the RelationshipAspects of the media item to the MediaLibrary.
    /// </summary>
    /// <param name="mediaItemId">The id of the media item.</param>
    /// <param name="aspects">The aspects of the media item.</param>
    /// <param name="relations">Collection of relations extracted from the media item.</param>
    /// <returns>A collection of added or updated media items for the specified relations.</returns>
    protected async Task<ICollection<MediaItem>> ReconcileRelationships(Guid mediaItemId, IDictionary<Guid, IList<MediaItemAspect>> aspects, ICollection<ExtractedRelation> relations)
    {
      ICollection<MediaItem> newMediaItems;

      await _cacheSync.WaitAsync(_ct);
      try
      {
        //Add relationship aspects for any cached relations, and get a collection of uncached relations.
        ICollection<ExtractedRelation> newRelations = AddCachedRelationshipsAndGetUncached(mediaItemId, aspects, relations);

        //Get an enumeration of the minimum aspects required to reconcile.
        IList<MediaItemAspect> reconcileAspects = GetReconcileAspects(aspects);

        //Add new relations to the MediaLibrary and update the relationship aspects of the parent item.
        //The MediaLibrary will handle adding the relationship aspects for the new relations. 
        newMediaItems = await ReconcileMediaItemRelationships(mediaItemId, reconcileAspects,
          newRelations.Select(r => new RelationshipItem(r.Extractor.Role, r.Extractor.LinkedRole, r.Aspects)));

        //Cache all newly added/updated relations
        foreach (MediaItem updatedMediaItem in newMediaItems)
        {
          var itemMatcher = GetLinkedRoleExtractors(updatedMediaItem.Aspects).FirstOrDefault();
          if (itemMatcher != null)
            _relationshipCache.TryAddExternalItem(updatedMediaItem, itemMatcher);
        }
      }
      finally
      {
        _cacheSync.Release();
      }

      ServiceRegistration.Get<ILogger>().Info(
        $"{BLOCK_NAME}: Added {relations.Count} relations ({newMediaItems.Count} new) to {GetMediaItemName(aspects)} ({mediaItemId})");

      TransferTransientAspects(aspects, newMediaItems);
      return newMediaItems;
    }

    /// <summary>
    /// Adds RelationshipAspects for all cached relations and returns a collection of uncached relations.
    /// </summary>
    /// <param name="mediaItemId">The id of the media item.</param>
    /// <param name="aspects">The aspects to add <see cref="RelationshipAspect"/>s for cached relations to.</param>
    /// <param name="relations">Collection of relations to check against the cache.</param>
    /// <returns>A collection of relations not in the cache.</returns>
    protected ICollection<ExtractedRelation> AddCachedRelationshipsAndGetUncached(Guid mediaItemId, IDictionary<Guid, IList<MediaItemAspect>> aspects,
      ICollection<ExtractedRelation> relations)
    {
      IList<ExtractedRelation> newRelations = new List<ExtractedRelation>();
      foreach (var relation in relations)
      {
        MediaItem externalItem;
        if (_relationshipCache.TryGetExternalItem(relation.Aspects, relation.Extractor, out externalItem))
        {
          if (relation.Extractor.BuildRelationship)
            AddRelationship(relation.Extractor, externalItem.MediaItemId, aspects, relation.Aspects, false);
        }
        else
        {
          newRelations.Add(relation);
          //If the relationship should be added to the extracted relation, add it here so it's present when the extracted item is committed to the
          //database. This ensures that we don't have any 'unlinked' virtual items at any time in the database which might get erroneously cleaned up
          //after a share deletion whilst this import is running.
          //TODO: Update the interface definition to better reflect the usage of BuildRelationship
          if (!relation.Extractor.BuildRelationship)
            AddRelationship(relation.Extractor, mediaItemId, aspects, relation.Aspects, true);
        }
      }
      return newRelations;
    }

    /// <summary>
    /// Caches the aspects of the specified <see cref="PendingImportResourceNewGen"/>.
    /// </summary>
    /// <param name="importResource">The <see cref="PendingImportResourceNewGen"/> containing the aspects to cache.</param>
    /// <returns>A Task that completes when the item has been cached.</returns>
    protected async Task<bool> CacheImportResource(PendingImportResourceNewGen importResource)
    {
      MediaItem item = new MediaItem(importResource.MediaItemId.Value, importResource.Aspects);
      bool result = false;
      await _cacheSync.WaitAsync();
      try
      {
        foreach (IRelationshipRoleExtractor roleExtractor in GetLinkedRoleExtractors(importResource.Aspects))
          result |= _relationshipCache.TryAddExternalItem(item, roleExtractor);
      }
      finally
      {
        _cacheSync.Release();
      }
      return result;
    }

    #endregion

    #region Static methods

    /// <summary>
    /// Adds a <see cref="RelationshipAspect"/> for the relation.
    /// </summary>
    /// <param name="roleExtractor">The <see cref="IRelationshipRoleExtractor"/> that extracted the relation.</param>
    /// <param name="linkedId">The media item id of the linked item.</param>
    /// <param name="aspects">The aspects of the media item.</param>
    /// <param name="extractedAspects">The aspects of the extracted item.</param>
    /// <param name="addToLinkedItem">Whether to to add the <see cref="RelationshipAspect"/> to the the linked item rather than the media item.</param>
    private static void AddRelationship(IRelationshipRoleExtractor roleExtractor, Guid linkedId, IDictionary<Guid, IList<MediaItemAspect>> aspects,
      IDictionary<Guid, IList<MediaItemAspect>> extractedAspects, bool addToLinkedItem)
    {
      int index;
      if (!roleExtractor.TryGetRelationshipIndex(aspects, extractedAspects, out index))
        index = 0;

      Guid role;
      Guid linkedRole;
      IDictionary<Guid, IList<MediaItemAspect>> roleAspects;
      //Reverse the role, linked role and aspects if adding to the linked item
      if (addToLinkedItem)
      {
        role = roleExtractor.LinkedRole;
        linkedRole = roleExtractor.Role;
        roleAspects = extractedAspects;
      }
      else
      {
        role = roleExtractor.Role;
        linkedRole = roleExtractor.LinkedRole;
        roleAspects = aspects;
      }

      //Get whether this relationship will update the parent's play percentage
      IRelationshipTypeRegistration rtr = ServiceRegistration.Get<IRelationshipTypeRegistration>();
      RelationshipType rt = rtr.LocallyKnownRelationshipTypes.FirstOrDefault(r => r.ChildRole == role && r.ParentRole == linkedRole);
      bool playable = rt != null ? rt.UpdatePlayPercentage : false;

      MediaItemAspect.AddOrUpdateRelationship(roleAspects, role, linkedRole, linkedId, playable, index);
    }

    private static IList<MediaItemAspect> GetReconcileAspects(IDictionary<Guid, IList<MediaItemAspect>> aspects)
    {
      List<MediaItemAspect> result = new List<MediaItemAspect>();

      IList<MediaItemAspect> aspect;
      foreach (Guid aspectId in RECONCILE_MIA_ID_ENUMERATION)
        if (aspects.TryGetValue(aspectId, out aspect))
          result.AddRange(aspect);

      return result;
    }

    private static void TransferTransientAspects(IDictionary<Guid, IList<MediaItemAspect>> aspects, IEnumerable<MediaItem> destinationMediaItems)
    {
      var transientAspects = MediaItemAspect.GetAspects(aspects).Where(mia => mia.Metadata.IsTransientAspect);
      foreach (MediaItemAspect aspect in transientAspects)
      {
        SingleMediaItemAspect singleAspect = aspect as SingleMediaItemAspect;
        if (singleAspect != null)
        {
          foreach (MediaItem destination in destinationMediaItems)
            MediaItemAspect.SetAspect(destination.Aspects, singleAspect);
        }
        else
        {
          MultipleMediaItemAspect multiAspect = aspect as MultipleMediaItemAspect;
          if (multiAspect != null)
            foreach (MediaItem destination in destinationMediaItems)
              MediaItemAspect.AddOrUpdateAspect(destination.Aspects, multiAspect);
        }
      }
    }

    private static IEnumerable<IRelationshipRoleExtractor> GetRoleExtractors(IDictionary<Guid, IList<MediaItemAspect>> aspects)
    {
      IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
      return mediaAccessor.LocalRelationshipExtractors.Values.SelectMany(r => r.RoleExtractors)
        .Where(r => r.RoleAspects.All(a => aspects.ContainsKey(a)));
    }

    private static IEnumerable<IRelationshipRoleExtractor> GetLinkedRoleExtractors(IDictionary<Guid, IList<MediaItemAspect>> aspects)
    {
      IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
      return mediaAccessor.LocalRelationshipExtractors.Values.SelectMany(r => r.RoleExtractors)
        .Where(r => r.LinkedRoleAspects.All(a => aspects.ContainsKey(a)));
    }

    private static string GetMediaItemName(IDictionary<Guid, IList<MediaItemAspect>> aspects)
    {
      string name;
      if (!MediaItemAspect.TryGetAttribute(aspects, MediaAspect.ATTR_TITLE, out name) || name == null)
        name = string.Empty;
      return name;
    }

    #endregion

    #region Base overrides

    protected override IPropagatorBlock<PendingImportResourceNewGen, PendingImportResourceNewGen> CreateInnerBlock()
    {
      return new TransformBlock<PendingImportResourceNewGen, PendingImportResourceNewGen>(new Func<PendingImportResourceNewGen, Task<PendingImportResourceNewGen>>(ProcessMediaItem), InnerBlockOptions);
    }

    #endregion
  }
}
