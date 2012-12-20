using System;
using System.Globalization;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.SkinEngine.MarkupExtensions;
using MediaPortal.UiComponents.BackgroundManager.Models;

namespace MediaPortal.UiComponents.BackgroundManager.Converter
{
  public class ListItemActionIdConverter : IValueConverter
  {
    public bool Convert(object val, Type targetType, object parameter, CultureInfo culture, out object result)
    {
      result = null;
      ListItem listItem = val as ListItem;
      if (listItem == null)
        return false;

      object actionObject;
      if (listItem.AdditionalProperties.TryGetValue(BackgroundManagerModel.ITEM_ACTION_KEY, out actionObject))
      {
        WorkflowAction action = (WorkflowAction) actionObject;
        result = action.ActionId + (parameter != null ? parameter.ToString() : null);
        return true;
      }
      return false;
    }

    public bool ConvertBack(object val, Type targetType, object parameter, CultureInfo culture, out object result)
    {
      result = null;
      return false;
    }
  }
}