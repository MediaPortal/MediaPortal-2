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

    public IDictionary<Guid, IMediaFanArtHandler> LocalFanArtHandlers
    {
      get
      {
        throw new NotImplementedException();
      }
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

    public IDictionary<Guid, IList<MediaItemAspect>> ExtractMetadata(IResourceAccessor mediaItemAccessor, IEnumerable<Guid> metadataExtractorIds, bool importOnly)
    {
      throw new NotImplementedException();
    }

    public IDictionary<Guid, IList<MediaItemAspect>> ExtractMetadata(IResourceAccessor mediaItemAccessor, IEnumerable<IMetadataExtractor> metadataExtractors, bool importOnly)
    {
      throw new NotImplementedException();
    }

    public MediaItem CreateLocalMediaItem(IResourceAccessor mediaItemAccessor, IEnumerable<Guid> metadataExtractorIds)
    {
      throw new NotImplementedException();
    }

    public IDictionary<Guid, IList<MediaItemAspect>> ExtractMetadata(IResourceAccessor mediaItemAccessor, IEnumerable<Guid> metadataExtractorIds, IDictionary<Guid, IList<MediaItemAspect>> existingAspects, bool importOnly)
    {
      throw new NotImplementedException();
    }

    public IDictionary<Guid, IList<MediaItemAspect>> ExtractMetadata(IResourceAccessor mediaItemAccessor, IEnumerable<IMetadataExtractor> metadataExtractors, IDictionary<Guid, IList<MediaItemAspect>> existingAspects, bool importOnly)
    {
      throw new NotImplementedException();
    }
  }
}
