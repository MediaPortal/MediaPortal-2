using System;
using System.Collections.Generic;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.ResourceAccess;
using RelocationMode = MediaPortal.Backend.MediaLibrary.RelocationMode;

namespace Test.MediaServer
{
  class TestMediaLibrary : IMediaLibrary
  {
    private IDictionary<Guid, Share> _shares = new Dictionary<Guid, Share>();
    private IDictionary<string, MediaItem> _items = new Dictionary<string, MediaItem>();

    private ILogger Logger { get { return ServiceRegistration.Get<ILogger>(); } }

    public void Startup()
    {
      throw new NotImplementedException();
    }

    public void ActivateImporterWorker()
    {
      throw new NotImplementedException();
    }

    public void Shutdown()
    {
      throw new NotImplementedException();
    }

    public MediaItemQuery BuildSimpleTextSearchQuery(string searchText, IEnumerable<Guid> necessaryMIATypes, IEnumerable<Guid> optionalMIATypes, IFilter filter, bool includeCLOBs, bool caseSensitive)
    {
      throw new NotImplementedException();
    }

    public MediaItem LoadItem(string systemId, ResourcePath path, IEnumerable<Guid> necessaryRequestedMIATypeIDs, IEnumerable<Guid> optionalRequestedMIATypeIDs)
    {
      Logger.Debug("Loading {0}, {1}", systemId, path);
      throw new NotImplementedException();
    }

    public IList<MediaItem> Browse(Guid parentDirectoryId, IEnumerable<Guid> necessaryRequestedMIATypeIDs, IEnumerable<Guid> optionalRequestedMIATypeIDs, uint? offset = null, uint? limit = null)
    {
      throw new NotImplementedException();
    }

    public IList<MediaItem> Search(MediaItemQuery query, bool filterOnlyOnline)
    {
      throw new NotImplementedException();
    }

    public HomogenousMap GetValueGroups(MediaItemAspectMetadata.AttributeSpecification attributeType, IFilter selectAttributeFilter, ProjectionFunction projectionFunction, IEnumerable<Guid> necessaryMIATypeIDs, IFilter filter, bool filterOnlyOnline)
    {
      throw new NotImplementedException();
    }

    public IList<MLQueryResultGroup> GroupValueGroups(MediaItemAspectMetadata.AttributeSpecification attributeType, IFilter selectAttributeFilter, ProjectionFunction projectionFunction, IEnumerable<Guid> necessaryMIATypeIDs, IFilter filter, bool filterOnlyOnline, GroupingFunction groupingFunction)
    {
      throw new NotImplementedException();
    }

    public int CountMediaItems(IEnumerable<Guid> necessaryMIATypeIDs, IFilter filter, bool filterOnlyOnline)
    {
      throw new NotImplementedException();
    }

    public ICollection<PlaylistInformationData> GetPlaylists()
    {
      throw new NotImplementedException();
    }

    public void SavePlaylist(PlaylistRawData playlistData)
    {
      throw new NotImplementedException();
    }

    public bool DeletePlaylist(Guid playlistId)
    {
      throw new NotImplementedException();
    }

    public PlaylistRawData ExportPlaylist(Guid playlistId)
    {
      throw new NotImplementedException();
    }

    public IList<MediaItem> LoadCustomPlaylist(IList<Guid> mediaItemIds, IEnumerable<Guid> necessaryMIATypes, IEnumerable<Guid> optionalMIATypes, uint? offset = null, uint? limit = null)
    {
      throw new NotImplementedException();
    }

    public Guid AddOrUpdateMediaItem(Guid parentDirectoryId, string systemId, ResourcePath path, IEnumerable<MediaItemAspect> mediaItemAspects)
    {
      throw new NotImplementedException();
    }

    public void UpdateMediaItem(Guid mediaItemId, IEnumerable<MediaItemAspect> mediaItemAspects)
    {
      throw new NotImplementedException();
    }

    public void DeleteMediaItemOrPath(string systemId, ResourcePath path, bool inclusive)
    {
      throw new NotImplementedException();
    }

    public void ClientStartedShareImport(Guid shareId)
    {
      throw new NotImplementedException();
    }

    public void ClientCompletedShareImport(Guid shareId)
    {
      throw new NotImplementedException();
    }

    public ICollection<Guid> GetCurrentlyImportingShareIds()
    {
      throw new NotImplementedException();
    }

    public void NotifyPlayback(Guid mediaItemId)
    {
      throw new NotImplementedException();
    }

    public bool MediaItemAspectStorageExists(Guid aspectId)
    {
      throw new NotImplementedException();
    }

    public MediaItemAspectMetadata GetMediaItemAspectMetadata(Guid aspectId)
    {
      throw new NotImplementedException();
    }

    public void AddMediaItemAspectStorage(MediaItemAspectMetadata miam)
    {
      throw new NotImplementedException();
    }

    public void RemoveMediaItemAspectStorage(Guid aspectId)
    {
      throw new NotImplementedException();
    }

    public IDictionary<Guid, MediaItemAspectMetadata> GetManagedMediaItemAspectMetadata()
    {
      throw new NotImplementedException();
    }

    public IDictionary<Guid, DateTime> GetManagedMediaItemAspectCreationDates()
    {
      throw new NotImplementedException();
    }

    public MediaItemAspectMetadata GetManagedMediaItemAspectMetadata(Guid aspectId)
    {
      throw new NotImplementedException();
    }

    public void RegisterShare(Share share)
    {
      throw new NotImplementedException();
    }

    public Guid CreateShare(string systemId, ResourcePath baseResourcePath, string shareName, IEnumerable<string> mediaCategories)
    {
      throw new NotImplementedException();
    }

    public void RemoveShare(Guid shareId)
    {
      throw new NotImplementedException();
    }

    public void RemoveSharesOfSystem(string systemId)
    {
      throw new NotImplementedException();
    }

    public int UpdateShare(Guid shareId, ResourcePath baseResourcePath, string shareName, IEnumerable<string> mediaCategories, RelocationMode relocationMode)
    {
      throw new NotImplementedException();
    }

    public IDictionary<Guid, Share> GetShares(string systemId)
    {
      return _shares;
    }

    public Share GetShare(Guid shareId)
    {
      throw new NotImplementedException();
    }

    public void SetupDefaultLocalShares()
    {
      throw new NotImplementedException();
    }

    public IDictionary<string, SystemName> OnlineClients
    {
      get { throw new NotImplementedException(); }
    }

    public void NotifySystemOnline(string systemId, SystemName currentSystemName)
    {
      throw new NotImplementedException();
    }

    public void NotifySystemOffline(string systemId)
    {
      throw new NotImplementedException();
    }

    internal void Clear()
    {
      _items.Clear();
    }

    public void AddItem(MediaItem item)
    {
      _items[item.MediaItemId.ToString()] = item;
    }

    public void AddShare(string shareId, string systemId, string directory, string name, string[] categories)
    {
      Guid id = new Guid(shareId);
      ProviderPathSegment segment = new ProviderPathSegment(LocalFsResourceProviderBase.LOCAL_FS_RESOURCE_PROVIDER_ID, directory, true);
      ResourcePath path = new ResourcePath(new[] { segment });
      _shares[id] = new Share(id, systemId, path, name, categories);
    }
  }
}
