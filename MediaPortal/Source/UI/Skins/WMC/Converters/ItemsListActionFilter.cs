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
