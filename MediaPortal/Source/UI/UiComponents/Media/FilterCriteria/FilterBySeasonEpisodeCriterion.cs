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
using MediaPortal.Common;
using MediaPortal.Common.Exceptions;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UI.Services.UserManagement;
using MediaPortal.UiComponents.Media.Settings;

namespace MediaPortal.UiComponents.Media.FilterCriteria
{
  /// <summary>
  /// Filter criterion which filters by the season.
  /// </summary>
  public class FilterBySeasonEpisodeCriterion : MLFilterCriterion
  {
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

      IEnumerable<Guid> mias = new[] { MediaAspect.ASPECT_ID, ProviderResourceAspect.ASPECT_ID, EpisodeAspect.ASPECT_ID }.Concat(necessaryMIATypeIds);
      MediaItemQuery query = new MediaItemQuery(mias, filter)
      {
        SortInformation = new List<SortInformation> { new SortInformation(EpisodeAspect.ATTR_EPISODE, SortDirection.Ascending) }
      };
      var items = cd.Search(query, true, userProfile, ShowVirtualSetting.ShowVirtualMedia(necessaryMIATypeIds));
      IList<FilterValue> result = new List<FilterValue>(items.Count);
      foreach (var item in items)
      {
        string title;
        MediaItemAspect.TryGetAttribute(item.Aspects, MediaAspect.ATTR_TITLE, out title);
        // TODO: Now what? There's no values for an episode
        result.Add(new FilterValue(title,
          null,
          null,
          item,
          null));
      }
      return result;
    }

    public override ICollection<FilterValue> GroupValues(ICollection<Guid> necessaryMIATypeIds, IFilter selectAttributeFilter, IFilter filter)
    {
      return null;
    }

    #endregion
  }
}
