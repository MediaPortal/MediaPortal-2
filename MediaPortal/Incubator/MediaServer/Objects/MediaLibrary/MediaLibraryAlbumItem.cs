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

namespace MediaPortal.Extensions.MediaServer.Objects.MediaLibrary
{
  public class MediaLibraryAlbumItem : BasicContainer, IDirectoryAlbum
  {
    protected string ObjectId { get; set; }
    protected string BaseKey { get; set; }

    public MediaLibraryAlbumItem(string id)
      : base(id)
    {
      BaseKey = MediaLibraryHelper.GetBaseKey(id);
      //ObjectId = MediaLibraryHelper.GetObjectId(id);
      var split = id.IndexOf(':');
      ObjectId = split > 0 ? id.Substring(split + 1) : "";
    }

    private IList<MediaItem> GetTracks()
    {
      var necessaryMiaTypeIDs = new Guid[]
                                  {
                                    MediaAspect.ASPECT_ID,
                                    AudioAspect.ASPECT_ID,
                                  };
      var library = ServiceRegistration.Get<IMediaLibrary>();

      IFilter searchFilter = new RelationalFilter(AudioAspect.ATTR_ALBUM, RelationalOperator.EQ, ObjectId);
      MediaItemQuery searchQuery = new MediaItemQuery(necessaryMiaTypeIDs, null, searchFilter);

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

        result.AddRange(items.Select(item => MediaLibraryHelper.InstansiateMediaLibraryObject(item, null, null)));
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("Cannot search for album " + ObjectId, e);
      }

    return result;
    }

    public string StorageMedium
    {
        get
        {
            throw new NotImplementedException();
        }
        set
        {
            throw new NotImplementedException();
        }
    }

    public string LongDescription
    {
        get
        {
            throw new NotImplementedException();
        }
        set
        {
            throw new NotImplementedException();
        }
    }

    public string Description
    {
        get
        {
            throw new NotImplementedException();
        }
        set
        {
            throw new NotImplementedException();
        }
    }

    public IList<string> Publisher
    {
        get
        {
            throw new NotImplementedException();
        }
        set
        {
            throw new NotImplementedException();
        }
    }

    public IList<string> Contributor
    {
        get
        {
            throw new NotImplementedException();
        }
        set
        {
            throw new NotImplementedException();
        }
    }

    public string Date
    {
        get
        {
            throw new NotImplementedException();
        }
        set
        {
            throw new NotImplementedException();
        }
    }

    public string Relation
    {
        get
        {
            throw new NotImplementedException();
        }
        set
        {
            throw new NotImplementedException();
        }
    }

    public IList<string> Rights
    {
        get
        {
            throw new NotImplementedException();
        }
        set
        {
            throw new NotImplementedException();
        }
    }
  }
}