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
using MediaPortal.Extensions.UserServices.FanArtService.Interfaces;
using MediaPortal.Common.FanArt;
using TagLib;

namespace MediaPortal.Extensions.UserServices.FanArtService.Local
{
  class AudioTagProvider : IBinaryFanArtProvider
  {
    private static readonly Guid[] NECESSARY_MIAS = { AudioAspect.ASPECT_ID, ProviderResourceAspect.ASPECT_ID };
    private static ICollection<string> SUPPORTED_EXTENSIONS = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase)
    {
      ".ape",
      ".flac",
      ".mp3",
      ".ogg",
      ".wv",
      ".wav",
      ".wma",
      ".mp4",
      ".m4a",
      ".m4p",
      ".mpc",
      ".mp+",
      ".mpp",
      ".dsf",
      ".dff",
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

      if (mediaType != FanArtMediaTypes.Album && mediaType != FanArtMediaTypes.Audio)
        return false;

      if (!Guid.TryParse(name, out mediaItemId))
        return false;

      IMediaLibrary mediaLibrary = ServiceRegistration.Get<IMediaLibrary>(false);
      if (mediaLibrary == null)
        return false;

      IFilter filter = null;
      if (mediaType == FanArtMediaTypes.Album)
      {
        filter = new RelationshipFilter(AudioAspect.ROLE_TRACK, AudioAlbumAspect.ROLE_ALBUM, mediaItemId);
      }
      else if (mediaType == FanArtMediaTypes.Audio)
      {
        filter = new MediaItemIdFilter(mediaItemId);
      }
      MediaItemQuery mediaQuery = new MediaItemQuery(NECESSARY_MIAS, filter);
      mediaQuery.Limit = 1;
      IList<MediaItem> items = mediaLibrary.Search(mediaQuery, false, null, true);
      if (items == null || items.Count == 0)
        return false;

      MediaItem mediaItem = items.First();
      // Virtual resources won't have any local fanart
      if (mediaItem.IsVirtual)
        return false;
      var resourceLocator = mediaItem.GetResourceLocator();
      string fileSystemPath = string.Empty;
      IList<PictureType> patterns = new List<PictureType>();
      switch (fanArtType)
      {
        case FanArtTypes.Undefined:
        case FanArtTypes.Poster:
        case FanArtTypes.Cover:
        case FanArtTypes.Thumbnail:
          patterns.Add(PictureType.FrontCover);
          patterns.Add(PictureType.Other);
          break;
        default:
          return false;
      }
      // File based access
      try
      {
        using (var accessor = resourceLocator?.CreateAccessor())
        {
          ILocalFsResourceAccessor fsra = accessor as ILocalFsResourceAccessor;
          if (fsra != null)
          {
            var ext = Path.GetExtension(fsra.LocalFileSystemPath);
            if (!SUPPORTED_EXTENSIONS.Contains(ext))
              return false;

            ByteVector.UseBrokenLatin1Behavior = true;  // Otherwise we have problems retrieving non-latin1 chars
            using (var tag = TagLib.File.Create(fsra.LocalFileSystemPath))
            {
              IPicture[] pics = tag.Tag.Pictures;
              if (pics.Length > 0)
              {
                foreach (var pattern in patterns)
                {
                  var picTag = pics.FirstOrDefault(p => p.Type == pattern);
                  if (picTag != null)
                  {
                    result = new List<FanArtImage> { new FanArtImage(name, picTag.Data.Data) };
                    return true;
                  }
                }
                //If no matching images found, use first images for thumbnails
                if (fanArtType == FanArtTypes.Thumbnail)
                {
                  result = new List<FanArtImage> { new FanArtImage(name, pics[0].Data.Data) };
                  return true;
                }
              }
            }
          }
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Warn("AudioTagProvider: Exception while reading tag of type '{0}' from '{1}'", ex, fanArtType, fileSystemPath);
      }
      return false;
    }

    #endregion
  }
}
