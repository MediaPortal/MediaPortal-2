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
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Plugins.MediaServer.Profiles;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common.General;
using MediaPortal.Common;

namespace MediaPortal.Plugins.MediaServer.Objects.MediaLibrary
{
  public class MediaLibrarySeriesItem : MediaLibraryContainer
  {
    private IFilter _episodeFilter = null;

    public MediaLibrarySeriesItem(MediaItem item, IFilter epsiodeFilter, EndPointSettings client)
      : base(item, NECESSARY_SEASON_MIA_TYPE_IDS, OPTIONAL_SEASON_MIA_TYPE_IDS, null, client)
    {
      _episodeFilter = epsiodeFilter;

      Genre = new List<string>();
      Artist = new List<string>();
      Contributor = new List<string>();

      if (Client.Profile.Settings.Metadata.Delivery == MetadataDelivery.All)
      {
        SingleMediaItemAspect seriesAspect;
        if (MediaItemAspect.TryGetAspect(Item.Aspects, SeriesAspect.Metadata, out seriesAspect))
        {
          var descriptionObj = seriesAspect.GetAttributeValue(SeriesAspect.ATTR_DESCRIPTION);
          if (descriptionObj != null)
            Description = descriptionObj.ToString();
        }
      }

      //Support alternative ways to get album art
      var albumArt = new MediaLibraryAlbumArt(Item, Client);
      if (albumArt != null)
      {
        albumArt.Initialise();
        if (Client.Profile.Settings.Thumbnails.Delivery == ThumbnailDelivery.All || Client.Profile.Settings.Thumbnails.Delivery == ThumbnailDelivery.Resource)
        {
          var albumResource = new MediaLibraryAlbumArtResource(albumArt);
          albumResource.Initialise();
          Resources.Add(albumResource);
        }
        if (Client.Profile.Settings.Thumbnails.Delivery == ThumbnailDelivery.All || Client.Profile.Settings.Thumbnails.Delivery == ThumbnailDelivery.AlbumArt)
        {
          AlbumArtUrl = albumArt.Uri;
        }
      }
    }

    public IList<MediaItem> GetItems()
    {
      IMediaLibrary library = ServiceRegistration.Get<IMediaLibrary>();
      if (_episodeFilter != null)
      {
        List<Guid> necessaryMias = new List<Guid>(NECESSARY_EPISODE_MIA_TYPE_IDS);
        if (necessaryMias.Contains(EpisodeAspect.ASPECT_ID)) necessaryMias.Remove(EpisodeAspect.ASPECT_ID); //Group MIA cannot be present
        HomogenousMap seasonItems = library.GetValueGroups(EpisodeAspect.ATTR_SEASON, null, ProjectionFunction.None, necessaryMias.ToArray(),
          _episodeFilter, true);

        List<object> seasonNumbers = new List<object>();
        foreach (object o in seasonItems.Keys)
        {
          if (o is int)
          {
            seasonNumbers.Add(o);
          }
        }

        return library.Search(new MediaItemQuery(NECESSARY_SEASON_MIA_TYPE_IDS, null,
          BooleanCombinationFilter.CombineFilters(BooleanOperator.And, new InFilter(SeasonAspect.ATTR_SEASON, seasonNumbers),
          new RelationshipFilter(Item.MediaItemId, SeriesAspect.ROLE_SERIES, SeasonAspect.ROLE_SEASON)))
          , true);
      }
      else
      {
        return library.Search(new MediaItemQuery(NECESSARY_SEASON_MIA_TYPE_IDS, null,
          new RelationshipFilter(Item.MediaItemId, SeriesAspect.ROLE_SERIES, SeasonAspect.ROLE_SEASON)), true);
      }
    }

    public override void Initialise()
    {
      _children.Clear();
      IList<MediaItem> items = GetItems();
      foreach (MediaItem item in items)
      {
        Add(new MediaLibrarySeasonItem(item, _episodeFilter, Client));
      }
    }

    public override string Class
    {
      get { return "object.container.album.musicAlbum"; }
    }

    public string Description { get; set; }
    public IList<string> Contributor { get; set; }
    public IList<string> Artist { get; set; }
    public IList<string> Genre { get; set; }
    public string AlbumArtUrl { get; set; }
  }
}
