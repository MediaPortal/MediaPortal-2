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

using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.MediaServer.Profiles;
using MediaPortal.Utilities;

namespace MediaPortal.Plugins.MediaServer.Objects.MediaLibrary
{
  public class MediaLibraryAudioItem : MediaLibraryItem, IDirectoryAudioItem
  {
    public MediaLibraryAudioItem(MediaItem item, EndPointSettings client)
      : base(item, client)
    {
      Genre = new List<string>();
      Publisher = new List<string>();
      Rights = new List<string>();

      if (MediaItemAspect.TryGetAspect(Item.Aspects, AudioAspect.Metadata, out SingleMediaItemAspect audioAspect))
      {
        Title = audioAspect.GetAttributeValue<string>(AudioAspect.ATTR_TRACKNAME);
      }

      if (client.Profile.Settings.Metadata.Delivery == MetadataDelivery.All)
      {
        if (MediaItemAspect.TryGetAspects(item.Aspects, GenreAspect.Metadata, out IList<MultipleMediaItemAspect> genreAspects))
        {
            CollectionUtils.AddAll(Genre, genreAspects.Select(g => g.GetAttributeValue<string>(GenreAspect.ATTR_GENRE)));
        }
      }

      //Support alternative ways to get album art
      if (AlbumArtUrls.Count > 0)
      {
        if (client.Profile.Settings.Thumbnails.Delivery == ThumbnailDelivery.All || client.Profile.Settings.Thumbnails.Delivery == ThumbnailDelivery.Resource)
        {
          var albumResource = new MediaLibraryAlbumArtResource((MediaLibraryAlbumArt)AlbumArtUrls[0]);
          albumResource.Initialise();
          Resources.Add(albumResource);
        }
        if (client.Profile.Settings.Thumbnails.Delivery != ThumbnailDelivery.All && client.Profile.Settings.Thumbnails.Delivery != ThumbnailDelivery.AlbumArt)
        {
          AlbumArtUrls.Clear();
        }
      }

      var resource = new MediaLibraryResource(item, client);
      resource.Initialise();
      Resources.Add(resource);
    }

    public override string Class
    {
      get { return "object.item.audioItem"; }
    }

    public IList<string> Genre { get; set; }

    public string Description { get; set; }

    public string LongDescription { get; set; }

    public IList<string> Publisher { get; set; }

    public string Language { get; set; }

    public string Relation { get; set; }

    public IList<string> Rights { get; set; }
  }
}
