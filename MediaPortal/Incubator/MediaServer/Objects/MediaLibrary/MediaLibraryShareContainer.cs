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
using MediaPortal.Plugins.MediaServer.Objects.Basic;
using MediaPortal.Plugins.MediaServer.Tree;
using MediaPortal.Plugins.MediaServer.Profiles;

namespace MediaPortal.Plugins.MediaServer.Objects.MediaLibrary
{
  public class MediaLibraryShareContainer : BasicContainer
  {
    protected Guid ObjectId { get; set; }
    protected string BaseKey { get; set; }
    protected IList<string> CategoryFilter { get; set; }

    public MediaLibraryShareContainer(string id, EndPointSettings client, params string[] categories)
      : base(id, client)
    {
      BaseKey = MediaLibraryHelper.GetBaseKey(id);
      ObjectId = MediaLibraryHelper.GetObjectId(id);
      if (categories != null)
      {
        CategoryFilter = new List<string>(categories);
      }
    }

    public override int ChildCount
    {
      get
      {
        // This is some what inefficient
        return ChildCount = MediaLibraryShares().Count;
      }
      set { }
    }

    public override TreeNode<object> FindNode(string key)
    {
      if (!key.StartsWith(Key)) return null;
      if (key == Key) return this;

      var item = MediaLibraryHelper.GetMediaItem(MediaLibraryHelper.GetObjectId(key));
      var parentId = new Guid(item.Aspects[ProviderResourceAspect.ASPECT_ID].GetAttributeValue(
        ProviderResourceAspect.ATTR_PARENT_DIRECTORY_ID).ToString());

      BasicContainer parent;
      if (parentId == Guid.Empty)
      {
        parent = new BasicContainer(MediaLibraryHelper.GetBaseKey(key), Client);
      }
      else
      {
        parent = new BasicContainer(MediaLibraryHelper.GetBaseKey(key) + ":" + parentId, Client);
      }

      return (TreeNode<object>)MediaLibraryHelper.InstansiateMediaLibraryObject(item, MediaLibraryHelper.GetBaseKey(key), parent);
    }

    private IDictionary<Guid, Share> MediaLibraryShares()
    {
      var library = ServiceRegistration.Get<IMediaLibrary>();
      if (CategoryFilter == null || CategoryFilter.Count == 0)
      {
        return library.GetShares(null);
      }
      else
      {
        Dictionary<Guid, Share> shares = new Dictionary<Guid, Share>();
        foreach (KeyValuePair<Guid, Share> share in library.GetShares(null))
        {
          foreach(string category in CategoryFilter)
          {
            if(share.Value.MediaCategories.Where(x => x.Contains(category)).FirstOrDefault() != null)
            {
              shares.Add(share.Key, share.Value);
              break;
            }
          }
        }
        return shares;
      }
    }

    public override List<IDirectoryObject> Search(string filter, string sortCriteria)
    {
      var shares = MediaLibraryShares();

      var necessaryMiaTypeIDs = new Guid[]
                                  {
                                    ProviderResourceAspect.ASPECT_ID,
                                    MediaAspect.ASPECT_ID,
                                  };
      var optionalMiaTypeIDs = new Guid[]
                                 {
                                   DirectoryAspect.ASPECT_ID,
                                 };

      var library = ServiceRegistration.Get<IMediaLibrary>();

      var parent = new BasicContainer(Id, Client);
      var items = (from share in shares
                   select new
                   {
                     Item = library.LoadItem(share.Value.SystemId,
                                             share.Value.BaseResourcePath,
                                             necessaryMiaTypeIDs,
                                             optionalMiaTypeIDs),
                     ShareName = share.Value.Name
                   }).ToList();
      var result = new List<IDirectoryObject>();
      foreach (var item in items)
      {
        try
        {
          result.Add(MediaLibraryHelper.InstansiateMediaLibraryObject(item.Item, Key, parent, item.ShareName));
        }
        catch (Exception e)
        {
          ServiceRegistration.Get<ILogger>().Error("Share search failed", e);
        }
      }
      return result;
    }
  }
}
