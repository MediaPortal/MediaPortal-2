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
using MediaPortal.Common;
using MediaPortal.Common.Exceptions;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UI.Services.UserManagement;
using System.Linq;
using MediaPortal.UiComponents.Media.Settings;

namespace MediaPortal.UiComponents.Media.FilterCriteria
{
  /// <summary>
  /// Filter criterion which creates a filter by a simple attribute value.
  /// </summary>
  public class RelationshipMLFilterCriterion : MLFilterCriterion
  {
    protected Guid _role;
    protected Guid _linkedRole;
    protected IEnumerable<Guid> _necessaryMIATypeIds;
    protected IEnumerable<Guid> _optionalMIATypeIds;
    protected ISortInformation _sortInformation;

    public RelationshipMLFilterCriterion(Guid role, Guid linkedRole, IEnumerable<Guid> necessaryMIATypeIds, IEnumerable<Guid> optionalMIATypeIds, ISortInformation sortInformation)
    {
      _role = role;
      _linkedRole = linkedRole;
      _necessaryMIATypeIds = necessaryMIATypeIds;
      _optionalMIATypeIds = optionalMIATypeIds;
      _sortInformation = sortInformation;
    }

    #region Base overrides

    public override ICollection<FilterValue> GetAvailableValues(IEnumerable<Guid> necessaryMIATypeIds, IFilter selectAttributeFilter, IFilter filter)
    {
      IContentDirectory cd = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
      if (cd == null)
        throw new NotConnectedException("The MediaLibrary is not connected");

      Guid? userProfile = null;
      IUserManagement userProfileDataManagement = ServiceRegistration.Get<IUserManagement>();
      if (userProfileDataManagement != null && userProfileDataManagement.IsValidUser)
        userProfile = userProfileDataManagement.CurrentUser.ProfileId;

      IEnumerable<Guid> mias = _necessaryMIATypeIds ?? necessaryMIATypeIds;
      IEnumerable<Guid> optMias = _optionalMIATypeIds != null ? _optionalMIATypeIds.Except(mias) : null;

      bool showVirtual = ShowVirtualSetting.ShowVirtualMedia(necessaryMIATypeIds);
      IFilter queryFilter = CreateQueryFilter(necessaryMIATypeIds, filter, showVirtual);

      MediaItemQuery query = new MediaItemQuery(mias, optMias, queryFilter);
      if (_sortInformation != null)
        query.SortInformation = new List<ISortInformation> { _sortInformation };

      IList<MediaItem> items = cd.Search(query, true, userProfile, showVirtual);
      IList<FilterValue> result = new List<FilterValue>(items.Count);
      foreach (MediaItem item in items)
      {
        string name;
        MediaItemAspect.TryGetAttribute(item.Aspects, MediaAspect.ATTR_TITLE, out name);
        result.Add(new FilterValue(name,
          new RelationshipFilter(_linkedRole, _role, item.MediaItemId),
          null,
          item,
          this));
      }
      return result;
    }

    /// <summary>
    /// Creates the filter that will be used when getting the available values for this filter criterion.
    /// </summary>
    /// <param name="necessaryMIATypeIds">Media item aspects which need to be available in the media items, from which
    /// the available relations will be collected.</param>
    /// <param name="filter">Base filter for the media items from which the available values will be collected.</param>
    /// <param name="showVirtual">Whether the query includes virtual items.</param>
    /// <returns>Filter that will be used to get the available values for this criterion.</returns>
    protected virtual IFilter CreateQueryFilter(IEnumerable<Guid> necessaryMIATypeIds, IFilter filter, bool showVirtual)
    {
      if (filter == null)
        //No filter just Create a simple relationship filter
        return new RelationshipFilter(_role, _linkedRole, Guid.Empty);

      //The showVirtual flag is handled by the server for the main query however
      //the FilteredRelationshipFilter uses a subquery to get the linked ids.
      //If showVirtual is false, we need to manually add a filter to the subquery to
      //ensure that it doesn't return virtual linked ids.
      //TODO: Add proper subquery support to the server so we can also handle the onlyOnline filter 
      //in subqueries as this is not possible to add here as we don't know the online systems.
      IFilter subFilter = showVirtual ? filter : AddOnlyNonVirtualFilter(filter);
      return new FilteredRelationshipFilter(_role, _linkedRole, subFilter);
    }
    
    protected IFilter AddOnlyNonVirtualFilter(IFilter innerFilter)
    {
      IFilter nonVirtualFilter = new RelationalFilter(MediaAspect.ATTR_ISVIRTUAL, RelationalOperator.EQ, false);
      return innerFilter == null ? nonVirtualFilter : BooleanCombinationFilter.CombineFilters(BooleanOperator.And, innerFilter, nonVirtualFilter);
    }

    protected virtual string GetDisplayName(object groupKey)
    {
      return string.Format("{0}", groupKey).Trim();
    }

    public override ICollection<FilterValue> GroupValues(ICollection<Guid> necessaryMIATypeIds, IFilter selectAttributeFilter, IFilter filter)
    {
      return null;
    }

    #endregion
  }
}
