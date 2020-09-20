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
    protected IFilter _filter = null;
    protected uint? _queryLimit = null;
    protected IList<ISortInformation> _sortInformation = new List<ISortInformation> { new AttributeSortInformation(MediaAspect.ATTR_SORT_TITLE, SortDirection.Ascending) };
    protected IList<MediaItem> _initCache = new List<MediaItem>();

    public Guid? MediaItemId { get; protected set; }

    public MediaLibraryContainer(string id, string title, Guid[] necessaryMiaTypeIds, Guid[] optionalMiaTypeIds, IFilter filter, EndPointSettings client)
      : base(id, client)
    {
      Title = ServiceRegistration.Get<ILocalization>().ToString(title);

      _necessaryMiaTypeIds = necessaryMiaTypeIds;
      _optionalMiaTypeIds = optionalMiaTypeIds;
      _filter = filter;
    }

    public MediaLibraryContainer(MediaItem item, Guid[] necessaryMiaTypeIds, Guid[] optionalMiaTypeIds, IFilter filter, EndPointSettings client)
      : this(item.MediaItemId.ToString(), MediaItemAspect.GetAspect(item.Aspects, MediaAspect.Metadata).GetAttributeValue(MediaAspect.ATTR_TITLE).ToString(),
      necessaryMiaTypeIds, optionalMiaTypeIds, filter, client)
    {
      MediaItemId = item.MediaItemId;
    }

    public virtual IList<MediaItem> GetItems(string sortCriteria)
    {
      IMediaLibrary library = ServiceRegistration.Get<IMediaLibrary>();
      //TODO: Check if this is correct handling of missing filter

      List<IFilter> filters = new List<IFilter>();
      if (_filter != null)
        filters.Add(_filter);
      if (MediaItemId.HasValue)
        filters.Add(new RelationalFilter(ProviderResourceAspect.ATTR_PARENT_DIRECTORY_ID, RelationalOperator.EQ, MediaItemId.Value));

      IFilter filter = BooleanCombinationFilter.CombineFilters(BooleanOperator.And, filters);
      var query = new MediaItemQuery(_necessaryMiaTypeIds, _optionalMiaTypeIds, AppendUserFilter(filter, _necessaryMiaTypeIds));
      query.Limit = _queryLimit;
      query.SortInformation = _sortInformation;
      return library.Search(query, true, UserId, false);
    }

    public override List<IDirectoryObject> Browse()
    {
      return _children.Cast<IDirectoryObject>().ToList();
    }

    public override void Initialise(string sortCriteria, uint? offset = null, uint? count = null)
    {
      base.Initialise(sortCriteria, offset, count);

      if (offset == 0)
      {
        _initCache.Clear();
        _children.Clear();
      }
      if (!_initCache.Any(i => i != null))
      {
        _initCache = GetItems(sortCriteria);
        foreach (var item in _initCache)
        {
          var title = item.MediaItemId.ToString();
          if (MediaItemAspect.TryGetAspect(item.Aspects, MediaAspect.Metadata, out var mediaAspect))
            title = mediaAspect.GetAttributeValue<string>(MediaAspect.ATTR_TITLE);

          Add(new BasicItem(item.MediaItemId.ToString(), Client)
          {
            Title = $"{title} ({item.MediaItemId.ToString()})"
          });
        }
      }

      uint? countStart = null;
      for (int i = 0; i < _initCache.Count; i++)
      {
        MediaItem item = _initCache[i];
        bool include = (!offset.HasValue || i >= offset) && (!count.HasValue || ((Convert.ToUInt32(i) - countStart) ?? 0) < count);
        if (include && !countStart.HasValue)
          countStart = Convert.ToUInt32(i);
        if (item == null)
          continue;

        try
        {
          if (include || item.Aspects.ContainsKey(DirectoryAspect.ASPECT_ID))
          {
            _initCache[i] = null;
            var child = (BasicObject)MediaLibraryHelper.InstansiateMediaLibraryObject(item, this);
            if (child != null)
              _children[i] = child;
          }
        }
        catch (Exception ex)
        {
          Logger.Error("Media item '{0}' could not be added", ex, item);
        }
      }
    }

    public override void InitialiseContainers()
    {
      base.InitialiseContainers();
      var items = GetItems(null);
      foreach (var item in items)
      {
        if (item.Aspects.ContainsKey(DirectoryAspect.ASPECT_ID))
        {
          var child = (BasicObject)MediaLibraryHelper.InstansiateMediaLibraryObject(item, this);
          if (child != null)
            Add(child);
        }
        else
        {
          Add(new BasicItem(item.MediaItemId.ToString(), Client, true));
        }
      }
    }
  }
}
