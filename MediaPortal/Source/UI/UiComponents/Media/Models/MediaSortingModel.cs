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
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.Media.General;
using System.Linq;

namespace MediaPortal.UiComponents.Media.Models
{
  public class MediaSortingModel
  {
    #region Consts

    public const string MS_MODEL_ID_STR = "3871146E-AFF4-4B7F-90E5-091764E4F45A";
    public static Guid MS_MODEL_ID = new Guid(MS_MODEL_ID_STR);

    #endregion

    protected readonly ItemsList _sortingItemsList = new ItemsList();

    protected NavigationData GetCurrentNavigationData()
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      return MediaNavigationModel.GetNavigationData(workflowManager.CurrentNavigationContext, false);
    }

    protected void SetSorting(Sorting.Sorting sorting)
    {
      NavigationData navigationData = GetCurrentNavigationData();
      if (navigationData == null)
        return;
      navigationData.CurrentSorting = sorting;
    }

    protected void UpdateSortingsList()
    {
      _sortingItemsList.Clear();
      NavigationData navigationData = GetCurrentNavigationData();
      ICollection<Sorting.Sorting> sortings = navigationData.AvailableSortings;
      if (sortings == null)
        return;
      foreach (Sorting.Sorting sorting in sortings.Where(s => s.IsAvailable(navigationData.CurrentScreenData)))
      {
        Sorting.Sorting sortingCopy = sorting;
        ListItem sortingItem = new ListItem(Consts.KEY_NAME, sorting.DisplayName)
        {
          Command = new MethodDelegateCommand(() => navigationData.CurrentSorting = sortingCopy),
          Selected = navigationData.CurrentSorting == sortingCopy
        };
        sortingItem.AdditionalProperties[Consts.KEY_SORTING] = sortingCopy;
        _sortingItemsList.Add(sortingItem);
      }
      _sortingItemsList.FireChange();
    }

    #region Members to be accessed from the GUI

    public ItemsList SortingItemsList
    {
      get
      {
        UpdateSortingsList();
        return _sortingItemsList;
      }
    }

    #endregion
  }
}
