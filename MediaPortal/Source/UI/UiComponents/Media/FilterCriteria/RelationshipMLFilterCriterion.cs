﻿#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using MediaPortal.UiComponents.Media.Settings;
using MediaPortal.Common.Settings;
using System.Linq;

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
    protected SortInformation _sortInformation;

    public RelationshipMLFilterCriterion(Guid role, Guid linkedRole, IEnumerable<Guid> necessaryMIATypeIds, SortInformation sortInformation)
    {
      _role = role;
      _linkedRole = linkedRole;
      _necessaryMIATypeIds = necessaryMIATypeIds;
      _optionalMIATypeIds = null;
      _sortInformation = sortInformation;
    }

    public RelationshipMLFilterCriterion(Guid role, Guid linkedRole, IEnumerable<Guid> necessaryMIATypeIds, IEnumerable<Guid> optionalMIATypeIds, SortInformation sortInformation)
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

      ViewSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<ViewSettings>();
      bool showVirtual = settings.ShowVirtual;

      IEnumerable<Guid> mias = _necessaryMIATypeIds ?? necessaryMIATypeIds;
      IEnumerable<Guid> optMias = _optionalMIATypeIds != null ? _optionalMIATypeIds.Except(mias) : null;
      IFilter queryFilter = filter != null ? new RelationshipFilter(filter, _linkedRole, _role) : null;
      MediaItemQuery query = new MediaItemQuery(mias, optMias, queryFilter);
      if (_sortInformation != null)
        query.SortInformation = new List<SortInformation> { _sortInformation };
      IList<MediaItem> items = cd.Search(query, true, userProfile, showVirtual);
      IList<FilterValue> result = new List<FilterValue>(items.Count);
      foreach (MediaItem item in items)
      {
        string name;
        MediaItemAspect.TryGetAttribute(item.Aspects, MediaAspect.ATTR_TITLE, out name);
        result.Add(new FilterValue(name,
          new RelationshipFilter(item.MediaItemId, _role, _linkedRole),
          null,
          item,
          this));
      }
      return result;
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
