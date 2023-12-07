#region Copyright (C) 2007-2023 Team MediaPortal

/*
    Copyright (C) 2007-2023 Team MediaPortal
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

using MediaPortal.Common;
using MediaPortal.Common.UserManagement;
using MediaPortal.Common.UserProfileDataManagement;
using MediaPortal.UI.ContentLists;
using MediaPortal.UI.Presentation.DataObjects;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Webradio.MediaLists
{
  public abstract class WebradioMediaListProviderBase : IContentListProvider
  {
    protected ItemsList _allItems;

    protected WebradioMediaListProviderBase()
    {
      _allItems = new ItemsList();
    }

    public ItemsList AllItems => _allItems;

    public abstract Task<bool> UpdateItemsAsync(int maxItems, UpdateReason updateReason, ICollection<object> updatedObjects);

    protected async Task<UsageStatistics> GetSiteStats()
    {
      IUserManagement userManagement = ServiceRegistration.Get<IUserManagement>();
      if (userManagement.UserProfileDataManagement == null) return null;
      UsageStatistics stats = await userManagement.UserProfileDataManagement.GetFeatureUsageStatisticsAsync(userManagement.CurrentUser.ProfileId, "webradio");
      return stats;
    }
  }
}
