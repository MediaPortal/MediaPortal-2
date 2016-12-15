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