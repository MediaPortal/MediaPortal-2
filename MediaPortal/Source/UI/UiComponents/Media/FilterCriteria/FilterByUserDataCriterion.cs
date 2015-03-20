#region Copyright (C) 2007-2015 Team MediaPortal

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
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UI.Services.UserManagement;
using MediaPortal.UiComponents.Media.General;
using Newtonsoft.Json;

namespace MediaPortal.UiComponents.Media.FilterCriteria
{
  public class FilterByUserDataCriterion : MLFilterCriterion
  {
    const string KEY_USER_QUERIES = "SavedUserQueries";

    public override ICollection<FilterValue> GetAvailableValues(IEnumerable<Guid> necessaryMIATypeIds, IFilter selectAttributeFilter, IFilter filter)
    {
      IContentDirectory cd = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
      if (cd == null)
        throw new NotConnectedException("The MediaLibrary is not connected");

      IFilter watchedFilter = new LikeFilter(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, "{e88e64a8-0233-4fdf-ba27-0b44c6a39ae9}:///D:/Capture/The Code%", null);
      var filters = new List<FilterValue>(new[] { new FilterValue("Test 'The Code'", watchedFilter, null, 0, this) });

      IUserManagement userProfileDataManagement = ServiceRegistration.Get<IUserManagement>();
      bool recreate = false;
      if (userProfileDataManagement.IsValidUser)
      {
        string serializeObject;
        // Load lists
        if (!recreate && userProfileDataManagement.UserProfileDataManagement.GetUserAdditionalData(userProfileDataManagement.CurrentUser.ProfileId, KEY_USER_QUERIES, out serializeObject))
        {
          filters = JsonConvert.DeserializeObject<List<FilterValue>>(serializeObject, SerializerConfig.Default);
        }
        else
        {
          // Create default for later use
          serializeObject = JsonConvert.SerializeObject(filters, SerializerConfig.Default);
          userProfileDataManagement.UserProfileDataManagement.SetUserAdditionalData(userProfileDataManagement.CurrentUser.ProfileId, KEY_USER_QUERIES, serializeObject);
        }

        foreach (var filterValue in filters)
        {
          filterValue.NumItems = cd.CountMediaItems(necessaryMIATypeIds, filterValue.Filter, true);
        }
      }

      return filters;
    }

    public override ICollection<FilterValue> GroupValues(ICollection<Guid> necessaryMIATypeIds, IFilter selectAttributeFilter, IFilter filter)
    {
      return null;
    }
  }
}
