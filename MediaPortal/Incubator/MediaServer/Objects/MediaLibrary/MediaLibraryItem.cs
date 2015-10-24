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

using System.Collections.Generic;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.MediaServer.Objects.Basic;
using MediaPortal.Plugins.MediaServer.Profiles;

namespace MediaPortal.Plugins.MediaServer.Objects.MediaLibrary
{
  public class MediaLibraryItem : BasicItem, IDirectoryItemThumbnail
  {
    public MediaItem Item { get; protected set; }

    public MediaLibraryItem(string baseKey, MediaItem item, EndPointSettings client)
      : base(baseKey + ":" + item.MediaItemId, client)
    {
      Item = item;
      AlbumArtUrls = new List<IDirectoryAlbumArt>();
      var albumArt = new MediaLibraryAlbumArt(item, client);
      albumArt.Initialise();
      AlbumArtUrls.Add(albumArt);
    }

    public IList<IDirectoryAlbumArt> AlbumArtUrls { get; set; }
 
    public override void Initialise()
    {
      Title = Item.Aspects[MediaAspect.ASPECT_ID].GetAttributeValue(MediaAspect.ATTR_TITLE).ToString();
    }
  }
}
