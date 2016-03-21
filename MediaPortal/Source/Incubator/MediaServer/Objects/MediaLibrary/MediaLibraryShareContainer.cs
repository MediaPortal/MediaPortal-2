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

namespace MediaPortal.Plugins.MediaServer.Objects.MediaLibrary
{
  public class MediaLibraryShareContainer : BasicContainer
  {
    protected IList<string> CategoryFilter { get; set; }

    public MediaLibraryShareContainer(string id, EndPointSettings client, params string[] categories)
      : base(id, client)
    {
      if (categories != null)
      {
        CategoryFilter = new List<string>(categories);
      }
    }

    private IDictionary<Guid, Share> GetShares()
    {
      IMediaLibrary library = ServiceRegistration.Get<IMediaLibrary>();
      if (CategoryFilter == null || CategoryFilter.Count == 0)
      {
        return library.GetShares(null);
      }

      Dictionary<Guid, Share> shares = new Dictionary<Guid, Share>();
      foreach (KeyValuePair<Guid, Share> share in library.GetShares(null))
      {
        foreach (string category in CategoryFilter)
        {
          if (share.Value.MediaCategories.Where(x => x.Contains(category)).FirstOrDefault() != null)
          {
            shares.Add(share.Key, share.Value);
            break;
          }
        }
      }
      return shares;
    }

    public override void Initialise()
    {
      Console.WriteLine("MediaLibraryShareContainer: Initialise");

      IDictionary<Guid, Share> shares = GetShares();

      IMediaLibrary library = ServiceRegistration.Get<IMediaLibrary>();

      BasicContainer parent = new BasicContainer(Id, Client);
      var items = (from share in shares
                   select new
                   {
                     Item = library.LoadItem(share.Value.SystemId,
                                             share.Value.BaseResourcePath,
                                             NECESSARY_SHARE_MIA_TYPE_IDS,
                                             OPTIONAL_SHARE_MIA_TYPE_IDS),
                     ShareName = share.Value.Name
                   }).ToList();
      foreach (var item in items)
      {
        if (item.Item != null)
          Add((BasicObject)MediaLibraryHelper.InstansiateMediaLibraryObject(item.Item, parent, item.ShareName));
      }
    }
  }
}
