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
using System.Linq;
using System.Collections.Generic;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Plugins.MediaServer.Objects.Basic;
using MediaPortal.Plugins.MediaServer.Profiles;

namespace MediaPortal.Plugins.MediaServer.Objects.MediaLibrary
{
  public abstract class MediaLibraryContainer : BasicContainer
  {
    private readonly Guid[] _necessaryMiaTypeIds;
    private readonly Guid[] _optionalMiaTypeIds;
    private readonly IFilter _filter;

    public MediaLibraryContainer(string id, string title, Guid[] necessaryMiaTypeIds, Guid[] optionalMiaTypeIds, IFilter filter, EndPointSettings client)
      : base(id, client)
    {
      Title = title;

      _necessaryMiaTypeIds = necessaryMiaTypeIds;
      _optionalMiaTypeIds = optionalMiaTypeIds;
      _filter = filter;
    }

    public MediaLibraryContainer(MediaItem item, Guid[] necessaryMiaTypeIds, Guid[] optionalMiaTypeIds, IFilter filter, EndPointSettings client)
      : this(item.MediaItemId.ToString(), MediaItemAspect.GetAspect(item.Aspects, MediaAspect.Metadata).GetAttributeValue(MediaAspect.ATTR_TITLE).ToString(),
      necessaryMiaTypeIds, optionalMiaTypeIds, filter, client)
    {
      Item = item;
    }

    public IList<MediaItem> GetItems()
    {
      IMediaLibrary library = ServiceRegistration.Get<IMediaLibrary>();
      //TODO: Check if this is correct handling of missing filter
      if (_filter == null && Item != null)
      {
        return library.Browse(Item.MediaItemId, _necessaryMiaTypeIds, _optionalMiaTypeIds);
      }
      else
      {
        return library.Search(new MediaItemQuery(_necessaryMiaTypeIds, _optionalMiaTypeIds, _filter), true);
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
        Add((BasicObject)MediaLibraryHelper.InstansiateMediaLibraryObject(item, this));
      }
    }

    public MediaItem Item { get; protected set; }
  }
}
