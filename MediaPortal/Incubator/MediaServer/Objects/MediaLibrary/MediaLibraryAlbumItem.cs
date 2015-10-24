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
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Plugins.MediaServer.Objects.Basic;
using MediaPortal.Plugins.MediaServer.Tree;
using MediaPortal.Plugins.MediaServer.Profiles;
using MediaPortal.Utilities;
using MediaPortal.Plugins.Transcoding.Aspects;

namespace MediaPortal.Plugins.MediaServer.Objects.MediaLibrary
{
  public class MediaLibraryAlbumItem : BasicContainer, IDirectoryMusicAlbum
  {
    protected string ObjectId { get; set; }
    protected string BaseKey { get; set; }

    private readonly string _title;

    public MediaLibraryAlbumItem(string id, string title, EndPointSettings client)
      : base(id, client)
    {
      ServiceRegistration.Get<ILogger>().Debug("Create album {0}={1}", id, title);
      _title = title;
      BaseKey = MediaLibraryHelper.GetBaseKey(Key);
    }

    public override string Class
    {
      get { return "object.container.album.musicAlbum"; }
    }

    public override void Initialise()
    {
      Title = _title;
      IList<MediaItem> items = GetTracks();
      if (items != null && items.Count > 0)
      {
        MediaItem item = items[0];
        Genre = new List<string>();
        Artist = new List<string>();
        Contributor = new List<string>();

        if (Client.Profile.Settings.Metadata.Delivery == MetadataDelivery.All)
        {
          MediaItemAspect audioAspect;
          if (item.Aspects.TryGetValue(AudioAspect.ASPECT_ID, out audioAspect))
          {
            // TODO: the attribute is defined as IEnumerable<string>, why is it here IEnumerable<object>???
            var genreObj = audioAspect.GetCollectionAttribute<object>(AudioAspect.ATTR_GENRES);
            if (genreObj != null)
              CollectionUtils.AddAll(Genre, genreObj.Cast<string>());

            var artistObj = audioAspect.GetCollectionAttribute<object>(AudioAspect.ATTR_ALBUMARTISTS);
            if (artistObj != null)
              CollectionUtils.AddAll(Artist, artistObj.Cast<string>());

            var composerObj = audioAspect.GetCollectionAttribute<object>(AudioAspect.ATTR_COMPOSERS);
            if (composerObj != null)
              CollectionUtils.AddAll(Contributor, composerObj.Cast<string>());
          }
        }

        //Support alternative ways to get album art
        var albumArt = new MediaLibraryAlbumArt(item, Client);
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
    }

    private IList<MediaItem> GetTracks()
    {
      var necessaryMiaTypeIDs = new Guid[] {
                                    MediaAspect.ASPECT_ID,
                                    AudioAspect.ASPECT_ID,
                                    TranscodeItemAudioAspect.ASPECT_ID,
                                    ProviderResourceAspect.ASPECT_ID
                                  };
      var optionalMIATypeIDs = new Guid[]
                                 {
                                 };
      IMediaLibrary library = ServiceRegistration.Get<IMediaLibrary>();

      ServiceRegistration.Get<ILogger>().Debug("Looking for album " + _title);
      IFilter searchFilter = new RelationalFilter(AudioAspect.ATTR_ALBUM, RelationalOperator.EQ, _title);
      MediaItemQuery searchQuery = new MediaItemQuery(necessaryMiaTypeIDs, optionalMIATypeIDs, searchFilter);

      return library.Search(searchQuery, true);
    }

    public override int ChildCount
    {
      get { return GetTracks().Count; }
    }

    public override TreeNode<object> FindNode(string key)
    {
      if (!key.StartsWith(Key)) return null;
      if (key == Key) return this;

      return null;
    }

    public override List<IDirectoryObject> Search(string filter, string sortCriteria)
    {
      List<IDirectoryObject> result = new List<IDirectoryObject>();

      try
      {
        var parent = new BasicContainer(Id, Client);
        IList<MediaItem> items = GetTracks();
        result.AddRange(items.Select(item => MediaLibraryHelper.InstansiateMediaLibraryObject(item, BaseKey, parent)));
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("Cannot search for album " + ObjectId, e);
      }

      return result;
    }

    public string StorageMedium { get; set; }
    public string LongDescription { get; set; }
    public string Description { get; set; }
    public IList<string> Publisher { get; set; }
    public IList<string> Contributor { get; set; }
    public string Date { get; set; }
    public string Relation { get; set; }
    public IList<string> Rights { get; set; }
    public IList<string> Artist { get; set; }
    public IList<string> Genre { get; set; }
    public IList<string> Producer { get; set; }
    public string AlbumArtUrl { get; set; }
    public string Toc { get; set; }
  }
}
