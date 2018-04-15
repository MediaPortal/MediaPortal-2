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
using MediaPortal.Common.Commands;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Workflow;
using System.Linq;

namespace MediaPortal.UiComponents.Media.Models
{
  public class MediaGroupingModel
  {
    #region Consts

    public const string MS_MODEL_ID_STR = "68A966B6-6EAC-415E-89A4-7C486F9B5A3B";
    public static Guid MS_MODEL_ID = new Guid(MS_MODEL_ID_STR);

    #endregion

    protected readonly ItemsList _groupingItemsList = new ItemsList();

    protected NavigationData GetCurrentNavigationData()
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      return MediaNavigationModel.GetNavigationData(workflowManager.CurrentNavigationContext, false);
    }

    protected void SetGrouping(Sorting.Sorting grouping)
    {
      NavigationData navigationData = GetCurrentNavigationData();
      if (navigationData == null)
        return;
      navigationData.CurrentGrouping = grouping;
    }

    protected void UpdateGroupingsList()
    {
      _groupingItemsList.Clear();
      NavigationData navigationData = GetCurrentNavigationData();
      ICollection<Sorting.Sorting> groupings = navigationData.AvailableGroupings;
      if (groupings == null)
        return;
      ListItem groupingItem = new ListItem(Consts.KEY_NAME, Consts.RES_NO_GROUPING)
      {
        Command = new MethodDelegateCommand(() => navigationData.CurrentGrouping = null),
        Selected = navigationData.CurrentGrouping == null
      };
      groupingItem.AdditionalProperties[Consts.KEY_GROUPING] = null;
      _groupingItemsList.Add(groupingItem);

      foreach (Sorting.Sorting grouping in groupings.Where(g => g.IsAvailable(navigationData.CurrentScreenData)))
      {
        Sorting.Sorting groupingCopy = grouping;
        groupingItem = new ListItem(Consts.KEY_NAME, grouping.GroupByDisplayName)
        {
          Command = new MethodDelegateCommand(() => navigationData.CurrentGrouping = groupingCopy),
          Selected = navigationData.CurrentGrouping == groupingCopy
        };
        groupingItem.AdditionalProperties[Consts.KEY_GROUPING] = groupingCopy;
        _groupingItemsList.Add(groupingItem);
      }
      _groupingItemsList.FireChange();
    }

    #region Members to be accessed from the GUI

    public ItemsList GroupingItemsList
    {
      get
      {
        UpdateGroupingsList();
        return _groupingItemsList;
      }
    }

    #endregion
  }
}
