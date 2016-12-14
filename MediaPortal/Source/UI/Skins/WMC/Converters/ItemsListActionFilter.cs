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
using System.Globalization;
using System.Linq;
using MediaPortal.UiComponents.SkinBase.General;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.SkinEngine.MpfElements.Converters;
using MediaPortal.UI.SkinEngine.MpfElements.Resources;
using MediaPortal.Utilities;

namespace MediaPortal.UiComponents.WMCSkin.Converters
{
  public abstract class AbstractItemsListActionFilter: AbstractSingleDirectionConverter
  {
    public override bool Convert(object val, Type targetType, object parameter, CultureInfo culture, out object result)
    {
      // We can only work on ItemmsLists
      ItemsList list = val as ItemsList;
      if (list == null)
      {
        result = null;
        return false;
      }
      // If there is no filter (list of Guids for Actions), we use the given list
      ResourceWrapper res = parameter as ResourceWrapper;
      string actionIds = res != null ? (string)res.Resource : parameter as string;
      if (string.IsNullOrEmpty(actionIds))
      {
        result = list;
        return true;
      }
      // Apply filtering
      ICollection<Guid> guids = new HashSet<Guid>();
      CollectionUtils.AddAll(guids, actionIds.Split(';').Select(part => new Guid(part)));

      ItemsList filteredList = new ItemsList();
      foreach (ListItem listItem in list)
      {
        object action;
        if (!listItem.AdditionalProperties.TryGetValue(Consts.KEY_ITEM_ACTION, out action))
          continue;
        WorkflowAction wfAction = action as WorkflowAction;
        if (wfAction == null)
          continue;

        if (!ShouldIncludeItem(guids, wfAction))
          continue;

        filteredList.Add(listItem);
      }
      result = filteredList;
      return true;
    }

    protected abstract bool ShouldIncludeItem(ICollection<Guid> action, WorkflowAction wfAction);
  }

  public class IncludeItemsListActionFilter : AbstractItemsListActionFilter
  {
    protected override bool ShouldIncludeItem(ICollection<Guid> action, WorkflowAction wfAction)
    {
      return action.Contains(wfAction.ActionId);
    }
  }

  public class ExcludeItemsListActionFilter : AbstractItemsListActionFilter
  {
    protected override bool ShouldIncludeItem(ICollection<Guid> action, WorkflowAction wfAction)
    {
      return !action.Contains(wfAction.ActionId);
    }
  }
}
