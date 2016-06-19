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

using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess;
using MediaPortal.Extensions.UserServices.FanArtService.Interfaces;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Extensions.OnlineLibraries.Matchers;

namespace MediaPortal.Extensions.UserServices.FanArtService
{
  public class AudioFanartProvider : IFanArtProvider
  {
    private static readonly Guid[] NECESSARY_MIAS = { ProviderResourceAspect.ASPECT_ID, ExternalIdentifierAspect.ASPECT_ID };
    private static readonly Guid[] OPTIONAL_MIAS = { AudioAspect.ASPECT_ID, AudioAlbumAspect.ASPECT_ID, PersonAspect.ASPECT_ID, CompanyAspect.ASPECT_ID };

    private static readonly List<string> VALID_MEDIA_TYPES = new List<string>()
    {
       FanArtMediaTypes.Album,
       FanArtMediaTypes.Audio,
       FanArtMediaTypes.Artist,
       FanArtMediaTypes.Composer,
       FanArtMediaTypes.MusicLabel,
    };

    private static readonly List<string> VALID_FANART_TYPES = new List<string>()
    {
      FanArtTypes.Banner,
      FanArtTypes.ClearArt,
      FanArtTypes.DiscArt,
      FanArtTypes.FanArt,
      FanArtTypes.Logo,
      FanArtTypes.Poster,
      FanArtTypes.Thumbnail,
    };

    /// <summary>
    /// Gets a list of <see cref="FanArtImage"/>s for a requested <paramref name="mediaType"/>, <paramref name="fanArtType"/> and <paramref name="name"/>.
    /// The name can be: Series name, Actor name, Artist name depending on the <paramref name="mediaType"/>.
    /// </summary>
    /// <param name="mediaType">Requested FanArtMediaType</param>
    /// <param name="fanArtType">Requested FanArtType</param>
    /// <param name="name">Requested name of Series, Actor, Artist...</param>
    /// <param name="maxWidth">Maximum width for image. <c>0</c> returns image in original size.</param>
    /// <param name="maxHeight">Maximum height for image. <c>0</c> returns image in original size.</param>
    /// <param name="singleRandom">If <c>true</c> only one random image URI will be returned</param>
    /// <param name="result">Result if return code is <c>true</c>.</param>
    /// <returns><c>true</c> if at least one match was found.</returns>
    public bool TryGetFanArt(string mediaType, string fanArtType, string name, int maxWidth, int maxHeight, bool singleRandom, out IList<IResourceLocator> result)
    {
      result = null;

      if (!VALID_MEDIA_TYPES.Contains(mediaType) || !VALID_FANART_TYPES.Contains(fanArtType))
        return false;

      if (string.IsNullOrWhiteSpace(name))
        return false;

      Guid mediaItemId;
      if (Guid.TryParse(name, out mediaItemId) == false)
        return false;

      IMediaLibrary mediaLibrary = ServiceRegistration.Get<IMediaLibrary>(false);
      if (mediaLibrary == null)
        return false;

      IFilter filter = new MediaItemIdFilter(mediaItemId);
      IList<MediaItem> items = mediaLibrary.Search(new MediaItemQuery(NECESSARY_MIAS, OPTIONAL_MIAS, filter), false);
      if (items == null || items.Count == 0)
        return false;

      MediaItem mediaItem = items.First();
      List<string> fanArtFiles = new List<string>();
      object infoObject = null;
      if (mediaType == FanArtMediaTypes.Artist || mediaType == FanArtMediaTypes.Composer)
        infoObject = new PersonInfo().FromMetadata(mediaItem.Aspects);
      else if (mediaType == FanArtMediaTypes.MusicLabel)
        infoObject = new CompanyInfo().FromMetadata(mediaItem.Aspects);
      else if (mediaType == FanArtMediaTypes.Audio)
        infoObject = new TrackInfo().FromMetadata(mediaItem.Aspects);
      else if (mediaType == FanArtMediaTypes.Album)
        infoObject = new AlbumInfo().FromMetadata(mediaItem.Aspects);

      fanArtFiles.AddRange(MusicTheAudioDbMatcher.Instance.GetFanArtFiles(infoObject, mediaType, fanArtType));
      fanArtFiles.AddRange(MusicBrainzMatcher.Instance.GetFanArtFiles(infoObject, mediaType, fanArtType));
      fanArtFiles.AddRange(MusicFanArtTvMatcher.Instance.GetFanArtFiles(infoObject, mediaType, fanArtType));

      List<IResourceLocator> files = new List<IResourceLocator>();
      try
      {
        files.AddRange(fanArtFiles
              .Select(fileName => new ResourceLocator(ResourcePath.BuildBaseProviderPath(LocalFsResourceProviderBase.LOCAL_FS_RESOURCE_PROVIDER_ID, fileName)))
              );
        result = files;
        return result.Count > 0;
      }
      catch (Exception) { }
      return false;
    }
  }
}
