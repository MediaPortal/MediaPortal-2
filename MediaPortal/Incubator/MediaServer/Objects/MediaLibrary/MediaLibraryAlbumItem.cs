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
using MediaPortal.Extensions.MediaServer.Objects.Basic;
using MediaPortal.Extensions.MediaServer.Tree;
using MediaPortal.Extensions.MediaServer.Profiles;
using MediaPortal.Extensions.MediaServer.Aspects;

namespace MediaPortal.Extensions.MediaServer.Objects.MediaLibrary
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
    }

    private IList<MediaItem> GetTracks()
    {
      var necessaryMiaTypeIDs = new Guid[] {
                                    MediaAspect.ASPECT_ID,
                                    AudioAspect.ASPECT_ID
                                  };
      var optionalMIATypeIDs = new Guid[]
                                 {
                                   DlnaItemAudioAspect.ASPECT_ID,
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
      set { }
    }

    public override TreeNode<object> FindNode(string key)
    {
      if (!key.StartsWith(Key)) return null;
      if (key == Key) return this;

      ServiceRegistration.Get<ILogger>().Error("No idea how to find " + key);
      return null;
    }

    public override List<IDirectoryObject> Search(string filter, string sortCriteria)
    {
      List<IDirectoryObject> result = new List<IDirectoryObject>();

      try
      {
        IList<MediaItem> items = GetTracks();

        result.AddRange(items.Select(item => MediaLibraryHelper.InstansiateMediaLibraryObject(item, BaseKey, (BasicContainer)this.Parent)));
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
