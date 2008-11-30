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
using System.Collections;
using System.Collections.Generic;
using MediaPortal.Core;
using MediaPortal.Core.General;
using MediaPortal.Core.Logging;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.DefaultItemAspects;
using MediaPortal.Core.MediaManagement.MediaProviders;
using MediaPortal.Core.MediaManagement.MLQueries;
using MediaPortal.Core.Settings;
using MediaPortal.Media.ClientMediaManager.Views;
using MediaPortal.Utilities;

namespace MediaPortal.Media.ClientMediaManager
{
  /// <summary>
  /// The client's media manager class. It holds all media providers and metadata extractors and
  /// provides the concept of "views".
  /// </summary>
  public class MediaManager : MediaManagerBase, IImporter
  {
    /// <summary>
    /// Cache size for the internal LRU view cache.
    /// </summary>
    protected const int VIEW_CACHE_SIZE = 20;

    #region Protected fields

    protected ViewMetadata _rootView;
    protected IDictionary<Guid, ViewMetadata> _viewsIndex = new Dictionary<Guid, ViewMetadata>();

    /// <summary>
    /// Stores the <see cref="VIEW_CACHE_SIZE"/> last accessed views (with their content).
    /// The entries consist of (view id; view instance) mappings.
    /// </summary>
    protected SmallLRUCache<Guid, View> _viewCache = new SmallLRUCache<Guid, View>(VIEW_CACHE_SIZE);

    #endregion

    #region Ctor & initialization

    public MediaManager()
    {
      ServiceScope.Get<ILogger>().Debug("MediaManager: Create SharesManagement service");
      SharesManagement sharesManagement = new SharesManagement();
      ServiceScope.Add<ISharesManagement>(sharesManagement);
    }

    public void Startup()
    {
      ServiceScope.Get<ISharesManagement>().Initialize();
      LoadViews();
    }

    #endregion

    #region Protected methods

    protected void InitializeDefaultViews()
    {
      ISharesManagement sharesManagement = ServiceScope.Get<ISharesManagement>();
      // Create root view
      // TODO: Localization resource for [Media.RootViewName]
      ViewCollectionViewMetadata vcvm = new ViewCollectionViewMetadata(Guid.NewGuid(),
          "[Media.RootViewName]", null);
      _rootView = vcvm;
      _viewsIndex.Add(vcvm.ViewId, vcvm);

      // Create a local view for each share
      ICollection<ShareDescriptor> shares = sharesManagement.GetSharesBySystem(SystemName.Loopback()).Values;
      foreach (ShareDescriptor share in shares)
      {
        Guid viewId = Guid.NewGuid();
        ICollection<Guid> mediaItemAspectIds = new HashSet<Guid>();
        foreach (Guid metadataExtractorId in share.MetadataExtractorIds)
        {
          MetadataExtractorMetadata metadata = LocalMetadataExtractors[metadataExtractorId].Metadata;
          foreach (MediaItemAspectMetadata aspectMetadata in metadata.ExtractedAspectTypes)
            mediaItemAspectIds.Add(aspectMetadata.AspectId);
        }
        mediaItemAspectIds.Add(ProviderResourceAspect.ASPECT_ID);
        mediaItemAspectIds.Add(MediaAspect.ASPECT_ID);
        LocalShareViewMetadata lsvm = new LocalShareViewMetadata(viewId, share.Name,
            share.ShareId, string.Empty, _rootView.ViewId, mediaItemAspectIds);
        _viewsIndex.Add(viewId, lsvm);
        _rootView.SubViewIds.Add(viewId);
      }
    }

    #endregion

    #region View access & management

    protected void LoadViews()
    {
      ViewsSettings viewsSettings = ServiceScope.Get<ISettingsManager>().Load<ViewsSettings>();
      if (viewsSettings.ViewsStorage.Views.Count == 0)
      {
        // The views are still uninitialized - use defaults
        InitializeDefaultViews();
        SaveViews();
        return;
      }
      foreach (ViewMetadata view in viewsSettings.ViewsStorage.Views)
        _viewsIndex.Add(view.ViewId, view);
      _rootView = _viewsIndex[viewsSettings.ViewsStorage.RootViewId];
    }

    protected void SaveViews()
    {
      ViewsSettings viewsSettings = new ViewsSettings();
      viewsSettings.ViewsStorage.RootViewId = _rootView.ViewId;
      CollectionUtils.AddAll(viewsSettings.ViewsStorage.Views, _viewsIndex.Values);
      ServiceScope.Get<ISettingsManager>().Save(viewsSettings);
    }

    /// <summary>
    /// Returns the metadata of the root view. The root view is the entrance point into the
    /// hierarchy of media items.
    /// </summary>
    /// <returns>Metadata descriptor for the root view.</returns>
    public ViewMetadata RootView
    {
      get { return GetViewMetadata(_rootView.ViewId); }
    }

    /// <summary>
    /// Returns the metadata of the view with the specified <paramref name="viewId"/>.
    /// </summary>
    /// <returns>Metadata descriptor for the view with the specified <paramref name="viewId"/>.</returns>
    public ViewMetadata GetViewMetadata(Guid viewId)
    {
      return _viewsIndex[viewId];
    }

    /// <summary>
    /// Adds a new database view with the specified settings.
    /// </summary>
    /// <param name="displayName">Name of the new view to be displayed in the GUI. This
    /// might be a localized string ("[section.name]").</param>
    /// <param name="parentViewId">Id of the parent view, under that the new view should be located.</param>
    /// <param name="query">Database query which specifies the items contained in the new view.</param>
    /// <param name="mediaItemAspectIds">Collection of ids of media item aspects which should be contained
    /// in the new view.</param>
    public void AddDatabaseView(string displayName, Guid parentViewId,
        IQuery query, ICollection<Guid> mediaItemAspectIds)
    {
      Guid viewId = Guid.NewGuid();
      MediaLibraryViewMetadata mlvm = new MediaLibraryViewMetadata(viewId, displayName, query, parentViewId, mediaItemAspectIds);
      _viewsIndex.Add(viewId, mlvm);
    }

    /// <summary>
    /// Removes the view with the specified id. The root view and local views cannot be removed.
    /// </summary>
    /// <param name="viewId">Id of the view to be removed.</param>
    public void RemoveView(Guid viewId)
    {
      _viewsIndex.Remove(viewId);
    }

    /// <summary>
    /// Returns the view with the specified <paramref name="viewId"/>.
    /// </summary>
    /// <param name="viewId">Id of the view to return.</param>
    /// <returns>View with the specified id.</returns>
    public View GetView(Guid viewId)
    {
      View result = _viewCache.Get(viewId);
      if (result != null)
        return result;
      ViewMetadata metadata = GetViewMetadata(viewId);
      if (metadata is LocalShareViewMetadata)
        return new LocalShareView((LocalShareViewMetadata) metadata);
      else if (metadata is MediaLibraryViewMetadata)
        return new MediaLibraryView((MediaLibraryViewMetadata) metadata);
      else
        throw new NotImplementedException(string.Format(
            "View generation for the view's metadata class '{0}' is not supported.", metadata.GetType().Name));
    }

    /// <summary>
    /// Invalidates all cached media items which belong to the specified share.
    /// </summary>
    /// <param name="shareId">Id of the share whose items should be invalidated.</param>
    internal void InvalidateMediaItemsFromShare(Guid shareId)
    {
      foreach (View view in _viewCache.Values)
        if (view is LocalShareView && ((LocalShareView) view).LocalShareViewMetadata.ShareId.Equals(shareId))
          view.Invalidate();
    }

    #endregion

    #region IImporter implementation

    public void ForceImport(Guid? shareId, string path)
    {
      // TODO
      throw new System.NotImplementedException();
    }

    #endregion

    /// <summary>
    /// Synchronous metadata extraction method for an extraction of the specified metadata
    /// from the specified media provider location. Only the specified location will be processed,
    /// i.e. if the location denotes a media item, that item will be processed, else if the location
    /// denotes a folder, metadata for the folder itself will be extracted, no sub items will be processed.
    /// </summary>
    /// <param name="providerId">Id of the media provider to use as source for this metadata extraction.</param>
    /// <param name="path">Path in the provider to extract metadata from.</param>
    /// <param name="metadataExtractorIds">Enumeration of ids of metadata extractors to apply to the
    /// specified media file.</param>
    /// <returns>Enumeration of extracted media item aspects or <c>null</c>, if the specified provider doesn't
    /// exist or if no metadata could be extracted.</returns>
    public ICollection<MediaItemAspect> ExtractMetadata(Guid providerId, string path,
      IEnumerable<Guid> metadataExtractorIds)
    {
      if (!LocalMediaProviders.ContainsKey(providerId))
        return null;
      IMediaProvider provider = LocalMediaProviders[providerId];
      IDictionary<Guid, MediaItemAspect> result = new Dictionary<Guid, MediaItemAspect>();
      bool success = false;
      foreach (Guid extractorId in metadataExtractorIds)
      {
        if (!LocalMetadataExtractors.ContainsKey(extractorId))
          continue;
        IMetadataExtractor extractor = LocalMetadataExtractors[extractorId];
        foreach (MediaItemAspectMetadata miaMetadata in extractor.Metadata.ExtractedAspectTypes)
          if (!result.ContainsKey(miaMetadata.AspectId))
            result.Add(miaMetadata.AspectId, new MediaItemAspect(miaMetadata, providerId, path));
        if (extractor.TryExtractMetadata(provider, path, result))
          success = true;
      }
      return success ? result.Values : null;
    }
  }
}
