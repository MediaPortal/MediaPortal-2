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
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Extensions.UserServices.FanArtService.Interfaces;

namespace MediaPortal.Extensions.UserServices.FanArtService
{
  class AlbumThumbs : IBinaryFanArtProvider
  {
    private static readonly Guid[] NECESSARY_MIAS = new Guid[]
      {
        AudioAspect.ASPECT_ID,
        ThumbnailLargeAspect.ASPECT_ID,
      };

    #region Implementation of IFanArtProvider

    public bool TryGetFanArt(string mediaType, string fanArtType, string name, int maxWidth, int maxHeight, bool singleRandom, out IList<IResourceLocator> result)
    {
      result = null;
      return false;
    }

    public bool TryGetFanArt(string mediaType, string fanArtType, string name, int maxWidth, int maxHeight, bool singleRandom, out IList<FanArtImage> result)
    {
      result = null;
      if (mediaType != FanArtMediaTypes.Album || fanArtType != FanArtTypes.Poster || string.IsNullOrEmpty(name))
        return false;

      IMediaLibrary mediaLibrary = ServiceRegistration.Get<IMediaLibrary>(false);
      if (mediaLibrary == null)
        return false;

      IFilter filter = new RelationalFilter(AudioAspect.ATTR_ALBUM, RelationalOperator.EQ, name);
      MediaItemQuery query = new MediaItemQuery(NECESSARY_MIAS, filter)
        {
          Limit = 1, // Only one needed
          SortInformation = new List<SortInformation> { new SortInformation(AudioAspect.ATTR_ALBUM, SortDirection.Ascending) }
        };


      var items = mediaLibrary.Search(query, false);
      result = new List<FanArtImage>();
      foreach (var mediaItem in items)
      {
        byte[] textureData;
        if (!MediaItemAspect.TryGetAttribute(mediaItem.Aspects, ThumbnailLargeAspect.ATTR_THUMBNAIL, out textureData))
          continue;

        // Only one record required
        result.Add(new FanArtImage(name, textureData));
        return true;
      }
      return true;
    }

    #endregion
  }
}
