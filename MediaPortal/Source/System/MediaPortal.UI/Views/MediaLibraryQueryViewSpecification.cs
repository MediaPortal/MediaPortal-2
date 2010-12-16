#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.DefaultItemAspects;
using MediaPortal.Core.MediaManagement.MLQueries;
using MediaPortal.UI.ServerCommunication;
using UPnP.Infrastructure.CP;

namespace MediaPortal.UI.Views
{
  /// <summary>
  /// View which is based on a media library query.
  /// </summary>
  public class MediaLibraryQueryViewSpecification : ViewSpecification
  {
    #region Protected fields

    protected IFilter _filter;
    protected MediaItemQuery _query;
    protected bool _onlyOnline;
    protected int? _maxNumItems;
    protected int? _absNumItems;

    #endregion

    #region Ctor

    public MediaLibraryQueryViewSpecification(string viewDisplayName, IFilter filter,
        IEnumerable<Guid> necessaryMIATypeIDs, IEnumerable<Guid> optionalMIATypeIDs, bool onlyOnline) :
        base(viewDisplayName, necessaryMIATypeIDs, optionalMIATypeIDs)
    {
      _filter = filter;
      _query = new MediaItemQuery(necessaryMIATypeIDs, optionalMIATypeIDs, filter);
      _onlyOnline = onlyOnline;
    }

    #endregion

    public bool OnlyOnline
    {
      get { return _onlyOnline; }
    }

    public IFilter Filter
    {
      get { return _filter; }
    }

    /// <summary>
    /// Gets or sets the maximum number of media items to be shown before grouping items together in sub views.
    /// </summary>
    public int? MaxNumItems
    {
      get { return _maxNumItems; }
      set { _maxNumItems = value; }
    }

    /// <summary>
    /// Can be set to provide the overall number of all items and child items in this view.
    /// </summary>
    public override int? AbsNumItems
    {
      get { return _absNumItems; }
    }

    public override bool CanBeBuilt
    {
      get
      {
        IServerConnectionManager scm = ServiceRegistration.Get<IServerConnectionManager>();
        return scm.IsHomeServerConnected;
      }
    }

    public MediaLibraryQueryViewSpecification CreateSubViewSpecification(string viewDisplayName, IFilter filter)
    {
      IFilter combinedFilter = _filter == null ? filter : BooleanCombinationFilter.CombineFilters(BooleanOperator.And, new IFilter[] {_filter, filter});
      return new MediaLibraryQueryViewSpecification(viewDisplayName, combinedFilter, _necessaryMIATypeIds, _optionalMIATypeIds, _onlyOnline)
        {
            MaxNumItems = _maxNumItems
        };
    }

    public override IEnumerable<MediaItem> GetAllMediaItems()
    {
      IContentDirectory cd = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
      if (cd == null)
        return new List<MediaItem>();
      return cd.Search(_query, _onlyOnline);
    }

    protected internal override void ReLoadItemsAndSubViewSpecifications(out IList<MediaItem> mediaItems, out IList<ViewSpecification> subViewSpecifications)
    {
      mediaItems = null;
      subViewSpecifications = null;
      IContentDirectory cd = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
      if (cd == null)
        return;
      try
      {
        if (MaxNumItems.HasValue)
        {
          // First request value groups. That is a performance consideration:
          // If we have many items, we need groups. If we have few items, we don't need the groups but simply do a search.
          // We request the groups first to make it faster for the many items case. In the case of few items, both groups and items
          // are requested which doesn't take so long because there are only few items.
          IList<MLQueryResultGroup> groups = cd.GroupSearch(_query, MediaAspect.ATTR_TITLE, _onlyOnline, GroupingFunction.FirstCharacter);
          long numItems = 0;
          foreach (MLQueryResultGroup group in groups)
            numItems += group.NumItemsInGroup;
          if (numItems > MaxNumItems.Value)
          { // Group items
            mediaItems = new List<MediaItem>(0);
            subViewSpecifications = new List<ViewSpecification>(groups.Count);
            foreach (MLQueryResultGroup group in groups)
            {
              MediaLibraryQueryViewSpecification subViewSpecification = CreateSubViewSpecification(group.GroupName, group.AdditionalFilter);
              subViewSpecification.MaxNumItems = null;
              subViewSpecification._absNumItems = group.NumItemsInGroup;
              subViewSpecifications.Add(subViewSpecification);
            }
            return;
          }
          // Else: No grouping
        }
        // Else: No grouping
        mediaItems = cd.Search(_query, _onlyOnline);
        subViewSpecifications = new List<ViewSpecification>(0);
      }
      catch (UPnPRemoteException e)
      {
        ServiceRegistration.Get<ILogger>().Error("SimpleTextSearchViewSpecification.ReLoadItemsAndSubViewSpecifications: Error requesting server", e);
        mediaItems = null;
        subViewSpecifications = null;
      }
    }
  }
}
