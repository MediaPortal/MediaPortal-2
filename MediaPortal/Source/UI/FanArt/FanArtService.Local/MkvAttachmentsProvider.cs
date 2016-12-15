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
using System.IO;
using System.Linq;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Extensions.MetadataExtractors.MatroskaLib;
using MediaPortal.Extensions.UserServices.FanArtService.Interfaces;
using MediaPortal.Common.Services.ResourceAccess.VirtualResourceProvider;
using MediaPortal.Common.FanArt;

namespace MediaPortal.Extensions.UserServices.FanArtService.Local
{
  class MkvAttachmentsProvider : IBinaryFanArtProvider
  {
    private static readonly Guid[] NECESSARY_MIAS = { VideoStreamAspect.ASPECT_ID, ProviderResourceAspect.ASPECT_ID };
    private static ICollection<string> SUPPORTED_EXTENSIONS = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase)
    {
      ".mkv",
      ".webm",
    };

    #region Implementation of IFanArtProvider

    public FanArtProviderSource Source { get { return FanArtProviderSource.File; } }

    public bool TryGetFanArt(string mediaType, string fanArtType, string name, int maxWidth, int maxHeight, bool singleRandom, out IList<IResourceLocator> result)
    {
      result = null;
      return false;
    }

    public bool TryGetFanArt(string mediaType, string fanArtType, string name, int maxWidth, int maxHeight, bool singleRandom, out IList<FanArtImage> result)
    {
      result = null;
      Guid mediaItemId;
      if (!Guid.TryParse(name, out mediaItemId))
        return false;

      IMediaLibrary mediaLibrary = ServiceRegistration.Get<IMediaLibrary>(false);
      if (mediaLibrary == null)
        return false;

      IFilter filter = new MediaItemIdFilter(mediaItemId);
      IList<MediaItem> items = mediaLibrary.Search(new MediaItemQuery(NECESSARY_MIAS, filter), false, null, true);
      if (items == null || items.Count == 0)
        return false;

      MediaItem mediaItem = items.First();
      var resourceLocator = mediaItem.GetResourceLocator();
      // Virtual resources won't have any local fanart
      if (resourceLocator.NativeResourcePath.BasePathSegment.ProviderId == VirtualResourceProvider.VIRTUAL_RESOURCE_PROVIDER_ID)
        return false;

      string fileSystemPath = string.Empty;
      IList<string> patterns = new List<string>();
      switch (fanArtType)
      {
        case FanArtTypes.Banner:
          patterns.Add("banner.");
          break;
        case FanArtTypes.ClearArt:
          patterns.Add("clearart.");
          break;
        case FanArtTypes.Poster:
        case FanArtTypes.Thumbnail:
          patterns.Add("cover.");
          patterns.Add("poster.");
          patterns.Add("folder.");
          break;
        case FanArtTypes.FanArt:
          patterns.Add("backdrop.");
          patterns.Add("fanart.");
          break;
        default:
          return false;
      }
      // File based access
      try
      {
        using (var accessor = resourceLocator.CreateAccessor())
        {
          ILocalFsResourceAccessor fsra = accessor as ILocalFsResourceAccessor;
          if (fsra != null)
          {
            var ext = Path.GetExtension(fsra.LocalFileSystemPath);
            if (!SUPPORTED_EXTENSIONS.Contains(ext))
              return false;

            MatroskaInfoReader mkvReader = new MatroskaInfoReader(fsra);
            byte[] binaryData = null;
            if (patterns.Any(pattern => mkvReader.GetAttachmentByName(pattern, out binaryData)))
            {
              result = new List<FanArtImage> { new FanArtImage(name, binaryData) };
              return true;
            }
          }
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Warn("MkvAttachmentsProvider: Exception while reading mkv attachment of type '{0}' from '{1}'", ex, fanArtType, fileSystemPath);
      }
      return false;
    }

    #endregion
  }
}
