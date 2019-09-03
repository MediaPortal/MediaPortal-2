﻿#region Copyright (C) 2007-2017 Team MediaPortal

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
using System.Linq;
using System.Collections.Generic;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Extensions.MediaServer.Objects.Basic;
using MediaPortal.Extensions.MediaServer.Profiles;
using MediaPortal.Common.Localization;

namespace MediaPortal.Extensions.MediaServer.Objects.MediaLibrary
{
  public abstract class MediaLibraryContainer : BasicContainer
  {
    protected readonly Guid[] _necessaryMiaTypeIds;
    protected readonly Guid[] _optionalMiaTypeIds;
    protected MediaItemQuery _query = null;

    public MediaLibraryContainer(string id, string title, Guid[] necessaryMiaTypeIds, Guid[] optionalMiaTypeIds, IFilter filter, EndPointSettings client)
      : base(id, client)
    {
      Title = ServiceRegistration.Get<ILocalization>().ToString(title);

      _necessaryMiaTypeIds = necessaryMiaTypeIds;
      _optionalMiaTypeIds = optionalMiaTypeIds;
      _query = new MediaItemQuery(_necessaryMiaTypeIds, _optionalMiaTypeIds, filter);
    }

    public MediaLibraryContainer(MediaItem item, Guid[] necessaryMiaTypeIds, Guid[] optionalMiaTypeIds, IFilter filter, EndPointSettings client)
      : this(item.MediaItemId.ToString(), MediaItemAspect.GetAspect(item.Aspects, MediaAspect.Metadata).GetAttributeValue(MediaAspect.ATTR_TITLE).ToString(),
      necessaryMiaTypeIds, optionalMiaTypeIds, filter, client)
    {
      Item = item;
    }

    public virtual IList<MediaItem> GetItems()
    {
      IMediaLibrary library = ServiceRegistration.Get<IMediaLibrary>();
      //TODO: Check if this is correct handling of missing filter

      _query.Filter = AppendUserFilter(_query.Filter, _necessaryMiaTypeIds);
      if (_query.Filter == null && Item != null)
      {
        return library.Browse(Item.MediaItemId, _necessaryMiaTypeIds, _optionalMiaTypeIds, _userId, false);
      }
      else
      {
        return library.Search(_query, true, _userId, false);
      }
    }

    public override List<IDirectoryObject> Browse(string sortCriteria)
    {
      _children.Sort();
      return _children.Cast<IDirectoryObject>().ToList();
    }

    public override void Initialise()
    {
      _children.Clear();
      IList<MediaItem> items = GetItems();
      foreach (MediaItem item in items)
      {
        try
        {
          Add((BasicObject)MediaLibraryHelper.InstansiateMediaLibraryObject(item, this));
        }
        catch(Exception ex)
        {
          Logger.Error("Media item '{0}' could not be added", ex, item);
        }
      }
    }

    public MediaItem Item { get; protected set; }
  }
}
