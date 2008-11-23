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
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.MediaProviders;

namespace MediaPortal.Media.ClientMediaManager.Views
{
  /// <summary>
  /// View implementation which is based on a local provider path.
  /// </summary>
  public class LocalShareView : View
  {
    #region Ctor

    internal LocalShareView(LocalShareViewMetadata metadata) : base(metadata) { }

    #endregion

    public LocalShareViewMetadata LocalShareViewMetadata
    {
      get { return (LocalShareViewMetadata) Metadata; }
    }

    protected override IList<MediaItem> ReLoadItems()
    {
      IList<MediaItem> result = new List<MediaItem>();
      MediaManager mediaManager = ServiceScope.Get<MediaManager>();
      ShareDescriptor share = ServiceScope.Get<ISharesManagement>().GetShare(LocalShareViewMetadata.ShareId);
      Guid providerId = share.MediaProviderId;
      IMediaProvider provider = mediaManager.LocalMediaProviders[providerId];
      string path = LocalShareViewMetadata.ProviderPath;
      IEnumerable<Guid> metadataExtractorIds = share.MetadataExtractors;
      if (provider is IFileSystemMediaProvider)
      { // Add all items at the specified path
        IFileSystemMediaProvider fsmp = (IFileSystemMediaProvider) provider;
        foreach (string mediaItemPath in fsmp.GetMediaItems(path))
          AddMetadata(mediaManager, providerId, mediaItemPath, metadataExtractorIds, result);
      }
      else
        // Add the path itself (Could be a single-item share)
        AddMetadata(mediaManager, providerId, path, metadataExtractorIds, result);
      return result;
    }

    protected static void AddMetadata(MediaManager mediaManager, Guid providerId, string path,
        IEnumerable<Guid> metadataExtractorIds, ICollection<MediaItem> result)
    {
      ICollection<MediaItemAspect> aspects = mediaManager.ExtractMetadata(providerId, path, metadataExtractorIds);
      if (aspects != null)
        result.Add(new MediaItem(aspects));
    }
  }
}
