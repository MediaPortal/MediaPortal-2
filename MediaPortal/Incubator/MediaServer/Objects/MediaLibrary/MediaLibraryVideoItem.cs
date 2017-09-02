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
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.MediaServer.Profiles;
using MediaPortal.Utilities;
using MediaPortal.Plugins.Transcoding.Interfaces;

namespace MediaPortal.Plugins.MediaServer.Objects.MediaLibrary
{
  public class MediaLibraryVideoItem : MediaLibraryItem, IDirectoryVideoItem
  {
    public MediaLibraryVideoItem(MediaItem item, EndPointSettings client)
      : base(item, client)
    {
      Genre = new List<string>();
      Producer = new List<string>();
      Actor = new List<string>();
      Director = new List<string>();
      Publisher = new List<string>();

      if (client.Profile.Settings.Metadata.Delivery == MetadataDelivery.All)
      {
        SingleMediaItemAspect videoAspect;
        if (MediaItemAspect.TryGetAspect(item.Aspects, VideoAspect.Metadata, out videoAspect))
        {
          // TODO: type issue again :-/
          var genreObj = videoAspect.GetCollectionAttribute<object>(VideoAspect.ATTR_GENRES);
          if (genreObj != null)
            CollectionUtils.AddAll(Genre, genreObj.Cast<string>());

          var actorObj = videoAspect.GetCollectionAttribute<object>(VideoAspect.ATTR_ACTORS);
          if (actorObj != null)
            CollectionUtils.AddAll(Actor, actorObj.Cast<string>());

          var directorsObj = videoAspect.GetCollectionAttribute<object>(VideoAspect.ATTR_DIRECTORS);
          if (directorsObj != null)
            CollectionUtils.AddAll(Director, directorsObj.Cast<string>());

          var descriptionObj = videoAspect.GetAttributeValue(VideoAspect.ATTR_STORYPLOT);
          if (descriptionObj != null)
            Description = descriptionObj.ToString();
        }
      }
      object oValue = item[MediaAspect.Metadata].GetAttributeValue(MediaAspect.ATTR_RECORDINGTIME);
      if (oValue != null)
      {
        Date = Convert.ToDateTime(oValue).Date.ToString("yyyy-MM-dd");
      }

      //Support alternative ways to get cover
      if (AlbumArtUrls.Count > 0)
      {
        if (client.Profile.Settings.Thumbnails.Delivery == ThumbnailDelivery.All || client.Profile.Settings.Thumbnails.Delivery == ThumbnailDelivery.Icon)
        {
          Icon = AlbumArtUrls[0].Uri;
        }
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

      if (client.Profile.MediaTranscoding.SubtitleSettings.SubtitleMode == SubtitleSupport.SoftCoded)
      {
        MediaLibrarySubtitle sub = new MediaLibrarySubtitle(item, client);
        sub.Initialise();
        if (string.IsNullOrEmpty(sub.Uri) == false)
        {
          var subResource = new MediaLibrarySubtitleResource(sub);
          subResource.Initialise();
          Resources.Add(subResource);
        }
      }
    }

    public override string Class
    {
      get { return "object.item.videoItem"; }
    }

    public string Icon { get; set; }

    public string Date { get; set; }

    public IList<string> Genre { get; set; }

    public string LongDescription { get; set; }

    public IList<string> Producer { get; set; }

    public string Rating { get; set; }

    public IList<string> Actor { get; set; }

    public IList<string> Director { get; set; }

    public string Description { get; set; }

    public IList<string> Publisher { get; set; }

    public string Language { get; set; }

    public string Relation { get; set; }
  }
}
