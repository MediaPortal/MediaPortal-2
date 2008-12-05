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
using System.Collections.Generic;
using MediaPortal.Core;
using MediaPortal.Core.General;
using MediaPortal.Core.Logging;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.DefaultItemAspects;
using MediaPortal.Core.MediaManagement.MediaProviders;
using MediaPortal.Core.Settings;
using MediaPortal.Media.ClientMediaManager.Views;

namespace MediaPortal.Media.ClientMediaManager
{
  /// <summary>
  /// The client's media manager class. It holds all media providers and metadata extractors and
  /// provides the concept of "views".
  /// </summary>
  public class MediaManager : MediaManagerBase, IImporter, ISharesManagement
  {
    #region Protected fields

    protected ViewCollectionView _rootView;

    protected LocalSharesManagement _localLocalSharesManagement;

    #endregion

    #region Ctor & initialization

    public MediaManager()
    {
      _localLocalSharesManagement = new LocalSharesManagement();

      ServiceScope.Get<ILogger>().Debug("MediaManager: Registering global SharesManagement service");
      ServiceScope.Add<ISharesManagement>(this);
    }

    public void Startup()
    {
      _localLocalSharesManagement.LoadSharesFromSettings();
      LoadViews();
    }

    #endregion

    #region Protected methods

    protected void InitializeDefaultViews()
    {
      ISharesManagement sharesManagement = ServiceScope.Get<ISharesManagement>();
      // Create root view
      // TODO: Localization resource for [Media.RootViewName]
      ViewCollectionView vcv = new ViewCollectionView("[Media.RootViewName]", null);
      _rootView = vcv;

      // Create a local view for each share
      ICollection<ShareDescriptor> shares = sharesManagement.GetSharesBySystem(SystemName.GetLocalSystemName()).Values;
      foreach (ShareDescriptor share in shares)
      {
        ICollection<Guid> mediaItemAspectIds = new HashSet<Guid>();
        foreach (Guid metadataExtractorId in share.MetadataExtractorIds)
        {
          MetadataExtractorMetadata metadata = LocalMetadataExtractors[metadataExtractorId].Metadata;
          foreach (MediaItemAspectMetadata aspectMetadata in metadata.ExtractedAspectTypes)
            mediaItemAspectIds.Add(aspectMetadata.AspectId);
        }
        mediaItemAspectIds.Add(ProviderResourceAspect.ASPECT_ID);
        mediaItemAspectIds.Add(MediaAspect.ASPECT_ID);
        LocalShareView lsvm = new LocalShareView(share.ShareId, share.Name, string.Empty, _rootView, mediaItemAspectIds);
        vcv.SubViews.Add(lsvm);
      }
      // TODO: Create default database views
    }

    #endregion

    #region View access & management

    protected void LoadViews()
    {
      ViewsSettings settings = ServiceScope.Get<ISettingsManager>().Load<ViewsSettings>();
      _rootView = settings.RootView;
      if (_rootView == null)
      {
        // The views are still uninitialized - use defaults
        InitializeDefaultViews();
        SaveViews();
        return;
      }
      else
        _rootView.Loaded(null);
    }

    protected void SaveViews()
    {
      ViewsSettings settings = new ViewsSettings();
      settings.RootView = _rootView;
      ServiceScope.Get<ISettingsManager>().Save(settings);
    }

    /// <summary>
    /// Returns the root view. The root view is the entrance point into the navigation hierarchy of media items.
    /// </summary>
    public View RootView
    {
      get { return _rootView; }
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
    /// <returns>Dictionary of (media item aspect id; extracted media item aspect)-mappings or
    /// <c>null</c>, if the specified provider doesn't exist or if no metadata could be extracted.</returns>
    public IDictionary<Guid, MediaItemAspect> ExtractMetadata(Guid providerId, string path,
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
            result.Add(miaMetadata.AspectId, new MediaItemAspect(miaMetadata));
        if (extractor.TryExtractMetadata(provider, path, result))
          success = true;
      }
      return success ? result : null;
    }

    #region ISharesManagement implementation

    public ShareDescriptor RegisterShare(SystemName systemName, Guid providerId, string path, string shareName, IEnumerable<string> mediaCategories, IEnumerable<Guid> metadataExtractorIds)
    {
      // TODO: When connected, assign result from the call of the method at the MP server's
      // ISharesManagement interface
      ShareDescriptor result = null;
      if (systemName == SystemName.GetLocalSystemName())
        result = _localLocalSharesManagement.RegisterShare(systemName, providerId, path, shareName, mediaCategories,
            metadataExtractorIds);
      return result;
    }

    public void RemoveShare(Guid shareId)
    {
      // TODO: When connected, also call the method at the MP server's ISharesManagement interface
      _localLocalSharesManagement.RemoveShare(shareId);
    }

    public IDictionary<Guid, ShareDescriptor> GetShares()
    {
      // TODO: When connected, call the method at the MP server's ISharesManagement interface instead of
      // calling it on the local shares management
      return _localLocalSharesManagement.GetShares();
    }

    public ShareDescriptor GetShare(Guid shareId)
    {
      ShareDescriptor result = _localLocalSharesManagement.GetShare(shareId);
      // TODO: When connected and result == null, call method at the MP server's ISharesManagement interface
      return result;
    }

    public IDictionary<Guid, ShareDescriptor> GetSharesBySystem(SystemName systemName)
    {
      if (systemName == SystemName.GetLocalSystemName())
        return _localLocalSharesManagement.GetSharesBySystem(systemName);
      else
        // TODO: When connected, call the method at the MP server's ISharesManagement interface and return
        // its results
        return new Dictionary<Guid, ShareDescriptor>();
    }

    public ICollection<SystemName> GetManagedClients()
    {
      // TODO: When connected, call the method at the MP server's ISharesManagement interface
      return new List<SystemName>(new[] {SystemName.GetLocalSystemName()});
    }

    public IDictionary<Guid, MetadataExtractorMetadata> GetMetadataExtractorsBySystem(SystemName systemName)
    {
      if (systemName == SystemName.GetLocalSystemName())
        return _localLocalSharesManagement.GetMetadataExtractorsBySystem(SystemName.GetLocalSystemName());
      else
        // TODO: When connected, call the method at the MP server's ISharesManagement interface
        return new Dictionary<Guid, MetadataExtractorMetadata>();
    }

    public void AddMetadataExtractorsToShare(Guid shareId, IEnumerable<Guid> metadataExtractorIds)
    {
      _localLocalSharesManagement.AddMetadataExtractorsToShare(shareId, metadataExtractorIds);
      // TODO: When connected, also call the method at the MP server's ISharesManagement interface
    }

    public void RemoveMetadataExtractorsFromShare(Guid shareId, IEnumerable<Guid> metadataExtractorIds)
    {
      _localLocalSharesManagement.RemoveMetadataExtractorsFromShare(shareId, metadataExtractorIds);
      // TODO: When connected, also call the method at the MP server's ISharesManagement interface
    }

    #endregion
  }
}
