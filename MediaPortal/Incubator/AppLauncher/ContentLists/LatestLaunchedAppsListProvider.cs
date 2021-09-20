#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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

using MediaPortal.UI.Presentation.DataObjects;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediaPortal.UI.ContentLists;
using MediaPortal.Plugins.AppLauncher.General;
using MediaPortal.Plugins.AppLauncher.Models;
using System;

namespace MediaPortal.Plugins.AppLauncher.ContentLists
{
  public class LatestLaunchedAppsListProvider : AppContentListProviderBase
  {
    public override Task<bool> UpdateItemsAsync(int maxItems, UpdateReason updateReason, ICollection<object> updatedObjects)
    {
      if (!updateReason.HasFlag(UpdateReason.Forced))
        return Task.FromResult(false);

      var apps = Helper.LoadApps(false);
      IEnumerable<ListItem> listItems = apps.AppsList.Where(a => a.LastUsed > default(DateTime)).OrderByDescending(a => a.LastUsed).Select(a => CreateAppItem(a));
      _allItems.Clear();
      foreach (var item in listItems.Take(maxItems))
        _allItems.Add(item);
      _allItems.FireChange();
      return Task.FromResult(true);
    }
  }
}
