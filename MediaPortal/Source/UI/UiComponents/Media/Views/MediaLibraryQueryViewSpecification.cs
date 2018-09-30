#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.Common.UserManagement;
using MediaPortal.UI.ServerCommunication;
using UPnP.Infrastructure.CP;
using MediaPortal.UI.Services.UserManagement;
using MediaPortal.UiComponents.Media.Helpers;
using MediaPortal.UiComponents.Media.FilterTrees;

namespace MediaPortal.UiComponents.Media.Views
{
  /// <summary>
  /// View which is based on a media library query.
  /// </summary>
  public class MediaLibraryQueryViewSpecification : ViewSpecification
  {
    #region Protected fields

    protected IFilterTree _filterTree;
    protected FilterTreePath _filterPath;
    protected IDictionary<Guid, IFilter> _linkedAspectfilters;
    protected MediaItemQuery _query;
    protected bool _onlyOnline;
    protected int? _maxNumItems;
    protected int? _absNumItems;

    #endregion

    #region Ctor

    public MediaLibraryQueryViewSpecification(string viewDisplayName, IFilterTree filterTree,
        IEnumerable<Guid> necessaryMIATypeIDs, IEnumerable<Guid> optionalMIATypeIDs, bool onlyOnline) :
        base(viewDisplayName, necessaryMIATypeIDs, optionalMIATypeIDs)
    {
      _filterTree = filterTree ?? new SimpleFilterTree();
      _query = new MediaItemQuery(necessaryMIATypeIDs, optionalMIATypeIDs, _filterTree.BuildFilter());
      _onlyOnline = onlyOnline;
    }

    #endregion

    public bool OnlyOnline
    {
      get { return _onlyOnline; }
    }

    public IFilterTree FilterTree
    {
      get { return _filterTree; }
    }

    public FilterTreePath FilterPath
    {
      get { return _filterPath; }
      set { _filterPath = value; }
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

    public MediaLibraryQueryViewSpecification CreateSubViewSpecification(string viewDisplayName, FilterTreePath filterPath, IFilter filter, Guid? linkedId)
    {
      IFilterTree newFilterTree = _filterTree.DeepCopy();
      if (linkedId.HasValue)
        newFilterTree.AddLinkedId(linkedId.Value, filterPath);
      else if (filter != null)
        newFilterTree.AddFilter(filter, filterPath);
      
      return new MediaLibraryQueryViewSpecification(viewDisplayName, newFilterTree, _necessaryMIATypeIds, _optionalMIATypeIds, _onlyOnline)
      {
        MaxNumItems = _maxNumItems,
        FilterPath = filterPath
      };
    }

    public override async Task<IEnumerable<MediaItem>> GetAllMediaItems()
    {
      IContentDirectory cd = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
      if (cd == null)
        return new List<MediaItem>();

      Guid? userProfile = null;
      IUserManagement userProfileDataManagement = ServiceRegistration.Get<IUserManagement>();
      if (userProfileDataManagement != null && userProfileDataManagement.IsValidUser)
        userProfile = userProfileDataManagement.CurrentUser.ProfileId;

      bool showVirtual = VirtualMediaHelper.ShowVirtualMedia(_query.NecessaryRequestedMIATypeIDs);

      IList<MediaItem> mediaItems = await cd.SearchAsync(_query, _onlyOnline, userProfile, showVirtual);
      CertificationHelper.ConvertCertifications(mediaItems);
      return mediaItems;
    }

    protected internal override void ReLoadItemsAndSubViewSpecifications(out IList<MediaItem> mediaItems, out IList<ViewSpecification> subViewSpecifications)
    {
      var result = ReLoadItemsAndSubViewSpecificationsAsync().Result;
      mediaItems = result.Item1;
      subViewSpecifications = result.Item2;
    }

    protected internal async Task<Tuple<IList<MediaItem>, IList<ViewSpecification>>> ReLoadItemsAndSubViewSpecificationsAsync()
    {
      IList<MediaItem> mediaItems = null;
      IList<ViewSpecification> subViewSpecifications = null;
      IContentDirectory cd = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
      if (cd == null)
        return new Tuple<IList<MediaItem>, IList<ViewSpecification>>(null, null);

      Guid? userProfile = null;
      IUserManagement userProfileDataManagement = ServiceRegistration.Get<IUserManagement>();
      if (userProfileDataManagement != null && userProfileDataManagement.IsValidUser)
      {
        userProfile = userProfileDataManagement.CurrentUser.ProfileId;
      }
      bool showVirtual = VirtualMediaHelper.ShowVirtualMedia(_query.NecessaryRequestedMIATypeIDs);

      try
      {
        if (MaxNumItems.HasValue)
        {
          // First request value groups. That is a performance consideration:
          // If we have many items, we need groups. If we have few items, we don't need the groups but simply do a search.
          // We request the groups first to make it faster for the many items case. In the case of few items, both groups and items
          // are requested which doesn't take so long because there are only few items.
          IList<MLQueryResultGroup> groups = await cd.GroupValueGroupsAsync(MediaAspect.ATTR_TITLE, null, ProjectionFunction.None,
              _query.NecessaryRequestedMIATypeIDs, _query.Filter, _onlyOnline, GroupingFunction.FirstCharacter, showVirtual);
          long numItems = groups.Aggregate<MLQueryResultGroup, long>(0, (current, group) => current + group.NumItemsInGroup);
          if (numItems > MaxNumItems.Value)
          { // Group items
            mediaItems = new List<MediaItem>(0);
            subViewSpecifications = new List<ViewSpecification>(groups.Count);
            foreach (MLQueryResultGroup group in groups)
            {
              MediaLibraryQueryViewSpecification subViewSpecification =
                CreateSubViewSpecification(string.Format("{0}", group.GroupKey), _filterPath, group.AdditionalFilter, null);
              subViewSpecification.MaxNumItems = null;
              subViewSpecification._absNumItems = group.NumItemsInGroup;
              subViewSpecifications.Add(subViewSpecification);
            }
            return new Tuple<IList<MediaItem>, IList<ViewSpecification>>(mediaItems, subViewSpecifications);
          }
          // Else: No grouping
        }
        // Else: No grouping
        mediaItems = await cd.SearchAsync(_query, _onlyOnline, userProfile, showVirtual);
        CertificationHelper.ConvertCertifications(mediaItems);
        subViewSpecifications = new List<ViewSpecification>(0);
        return new Tuple<IList<MediaItem>, IList<ViewSpecification>>(mediaItems, subViewSpecifications);
      }
      catch (UPnPRemoteException e)
      {
        ServiceRegistration.Get<ILogger>().Error("SimpleTextSearchViewSpecification.ReLoadItemsAndSubViewSpecifications: Error requesting server", e);
        return new Tuple<IList<MediaItem>, IList<ViewSpecification>>(null, null);
      }
    }
  }
}
