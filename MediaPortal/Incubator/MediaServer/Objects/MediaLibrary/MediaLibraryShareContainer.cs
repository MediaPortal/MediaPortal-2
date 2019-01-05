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

using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Extensions.MediaServer.Objects.Basic;
using MediaPortal.Extensions.MediaServer.Profiles;

namespace MediaPortal.Extensions.MediaServer.Objects.MediaLibrary
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
      var allowedShares = GetAllowedShares();
      IDictionary<Guid, Share> shares = new Dictionary<Guid, Share>();

      IMediaLibrary library = ServiceRegistration.Get<IMediaLibrary>();
      foreach (var share in GetAllowedShares())
      {
        if (CategoryFilter == null || CategoryFilter.Count == 0 || share.MediaCategories.Any(x => CategoryFilter.Contains(x)))
        {
          shares.Add(share.ShareId, share);
        }
      }
      return shares;
    }

    public override void Initialise()
    {
      Logger.Debug("MediaServer initialise share containers");

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
        try
        {
          if (item.Item != null)
            Add((BasicObject)MediaLibraryHelper.InstansiateMediaLibraryObject(item.Item, parent, item.ShareName));
        }
        catch (Exception ex)
        {
          Logger.Error("Media item '{0}' could not be added", ex, item.Item);
        }
      }
    }
  }
}
