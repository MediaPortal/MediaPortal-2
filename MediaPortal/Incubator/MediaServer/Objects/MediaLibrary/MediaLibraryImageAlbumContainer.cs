#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using MediaPortal.Plugins.MediaServer.Objects.Basic;
using MediaPortal.Plugins.MediaServer.Profiles;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;

namespace MediaPortal.Plugins.MediaServer.Objects.MediaLibrary
{
  public class MediaLibraryImageAlbumContainer : BasicContainer
  {
    public MediaLibraryImageAlbumContainer(string id, EndPointSettings client)
      : base(id, client)
    {
    }

    public override void Initialise()
    {
      IMediaLibrary library = ServiceRegistration.Get<IMediaLibrary>();
      List<MediaItem> shares = new List<MediaItem>();
      foreach (KeyValuePair<Guid, Share> share in library.GetShares(null))
      {
        if (share.Value.MediaCategories.Where(x => x.Contains("Image")).FirstOrDefault() != null)
        {
          MediaItem item = library.LoadItem(share.Value.SystemId, share.Value.BaseResourcePath,
                                             NECESSARY_SHARE_MIA_TYPE_IDS, OPTIONAL_SHARE_MIA_TYPE_IDS);
          if (item != null && item.Aspects.ContainsKey(DirectoryAspect.ASPECT_ID))
          {
            shares.Add(item);
          }
        }
      }

      foreach (MediaItem share in shares)
      {
        IList<MediaItem> albums = library.Browse(share.MediaItemId, NECESSARY_SHARE_MIA_TYPE_IDS, OPTIONAL_SHARE_MIA_TYPE_IDS, null, true);
        foreach (MediaItem album in albums)
        {
          if (album != null && album.Aspects.ContainsKey(DirectoryAspect.ASPECT_ID))
          {
            Add(new MediaLibraryBrowser(album, Client));
          }
        }
      }
    }
  }
}
