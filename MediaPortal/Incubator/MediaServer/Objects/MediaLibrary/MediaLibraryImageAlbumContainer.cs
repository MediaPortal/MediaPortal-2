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
using System.Linq;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Extensions.MediaServer.Objects.Basic;
using MediaPortal.Extensions.MediaServer.Profiles;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;

namespace MediaPortal.Extensions.MediaServer.Objects.MediaLibrary
{
  public class MediaLibraryImageAlbumContainer : BasicContainer
  {
    public MediaLibraryImageAlbumContainer(string id, EndPointSettings client)
      : base(id, client)
    {
    }

    public override void Initialise(string sortCriteria, uint? offset = null, uint? count = null)
    {
      base.Initialise(sortCriteria, offset, count);

      IMediaLibrary library = ServiceRegistration.Get<IMediaLibrary>();
      var allowedShares = GetAllowedShares();
      List<MediaItem> shares = new List<MediaItem>();
      foreach (var share in allowedShares)
      {
        if (share.MediaCategories.Any(x => x.Contains("Image")))
        {
          MediaItem item = library.LoadItem(share.SystemId, share.BaseResourcePath, NECESSARY_SHARE_MIA_TYPE_IDS, OPTIONAL_SHARE_MIA_TYPE_IDS, UserId);
          if (item != null && item.Aspects.ContainsKey(DirectoryAspect.ASPECT_ID))
            shares.Add(item);
        }
      }

      foreach (MediaItem share in shares.OrderBy(s => MediaItemAspect.TryGetAspect(s.Aspects, MediaAspect.Metadata, out var aspect) ? aspect.GetAttributeValue<string>(MediaAspect.ATTR_SORT_TITLE) : ""))
      {
        IList<MediaItem> albums = library.Browse(share.MediaItemId, NECESSARY_SHARE_MIA_TYPE_IDS, OPTIONAL_SHARE_MIA_TYPE_IDS, UserId, false);
        foreach (MediaItem album in albums)
        {
          if (album != null && album.Aspects.ContainsKey(DirectoryAspect.ASPECT_ID))
            Add(new MediaLibraryBrowser(album, Client));
        }
      }
    }

    public override void InitialiseContainers()
    {
      base.InitialiseContainers();
      Initialise(null);
    }
  }
}
