using System;
using System.Collections.Generic;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.LocalFsResourceProvider;

namespace Test.OnlineLibraries
{
  public class TestMediaAccessor : IMediaAccessor
  {
    private IDictionary<Guid, IResourceProvider> _localResourceProviders = new Dictionary<Guid, IResourceProvider>();
    private IDictionary<string, MediaCategory> _mediaCategories = new Dictionary<string, MediaCategory>();

    public TestMediaAccessor()
    {
      _localResourceProviders[LocalFsResourceProvider.LOCAL_FS_RESOURCE_PROVIDER_ID] = new LocalFsResourceProvider();
    }

    public IDictionary<string, MediaCategory> MediaCategories
    {
      get { return _mediaCategories; }
    }

    public IDictionary<Guid, IResourceProvider> LocalResourceProviders
    {
      get { return _localResourceProviders; }
    }

    public IEnumerable<IBaseResourceProvider> LocalBaseResourceProviders
    {
      get { throw new NotImplementedException(); }
    }

    public IEnumerable<IChainedResourceProvider> LocalChainedResourceProviders
    {
      get { throw new NotImplementedException(); }
    }

    public IDictionary<Guid, IMetadataExtractor> LocalMetadataExtractors
    {
      get { throw new NotImplementedException(); }
    }

    public IDictionary<Guid, IRelationshipExtractor> LocalRelationshipExtractors
    {
      get { throw new NotImplementedException(); }
    }

    public IDictionary<Guid, IMediaMergeHandler> LocalMergeHandlers
    {
      get { throw new NotImplementedException(); }
    }

    public void Initialize()
    {
      throw new NotImplementedException();
    }

    public void Shutdown()
    {
      throw new NotImplementedException();
    }

    public ICollection<Share> CreateDefaultShares()
    {
      throw new NotImplementedException();
    }

    public MediaCategory RegisterMediaCategory(string name, ICollection<MediaCategory> parentCategories)
    {
      MediaCategory category = new MediaCategory(name, parentCategories);
      _mediaCategories[name] = category;
      return category;
    }

    public ICollection<MediaCategory> GetAllMediaCategoriesInHierarchy(MediaCategory mediaCategory)
    {
      throw new NotImplementedException();
    }

    public ICollection<Guid> GetMetadataExtractorsForCategory(string mediaCategory)
    {
      throw new NotImplementedException();
    }

    public ICollection<Guid> GetMetadataExtractorsForMIATypes(IEnumerable<Guid> miaTypeIDs)
    {
      throw new NotImplementedException();
    }

    public IDictionary<Guid, IList<MediaItemAspect>> ExtractMetadata(IResourceAccessor mediaItemAccessor, IEnumerable<Guid> metadataExtractorIds, bool forceQuickMode)
    {
      throw new NotImplementedException();
    }

    public IDictionary<Guid, IList<MediaItemAspect>> ExtractMetadata(IResourceAccessor mediaItemAccessor, IEnumerable<IMetadataExtractor> metadataExtractors, bool forceQuickMode)
    {
      throw new NotImplementedException();
    }

    public MediaItem CreateLocalMediaItem(IResourceAccessor mediaItemAccessor, IEnumerable<Guid> metadataExtractorIds)
    {
      throw new NotImplementedException();
    }

    public IDictionary<Guid, IList<MediaItemAspect>> ExtractMetadata(IResourceAccessor mediaItemAccessor, IEnumerable<Guid> metadataExtractorIds, IDictionary<Guid, IList<MediaItemAspect>> existingAspects, bool forceQuickMode)
    {
      throw new NotImplementedException();
    }

    public IDictionary<Guid, IList<MediaItemAspect>> ExtractMetadata(IResourceAccessor mediaItemAccessor, IEnumerable<IMetadataExtractor> metadataExtractors, IDictionary<Guid, IList<MediaItemAspect>> existingAspects, bool forceQuickMode)
    {
      throw new NotImplementedException();
    }
  }
}
