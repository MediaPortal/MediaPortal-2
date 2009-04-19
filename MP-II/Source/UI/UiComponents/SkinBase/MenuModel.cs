#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Collections.Generic;
using MediaPortal.Core;
using MediaPortal.Core.Commands;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.Presentation.Workflow;

namespace UiComponents.SkinBase
{
  public class MenuModel
  {
    protected const string MENU_ITEMS_KEY = "MenuModel: Menu-Items";

    public MenuModel() { }

    public ItemsList MenuItems
    {
      get { return GetCurrentMenuItems(); }
    }

    #region Protected methods

    protected static ItemsList GetCurrentMenuItems()
    {
      IWorkflowManager workflowManager = ServiceScope.Get<IWorkflowManager>();
      NavigationContext context = workflowManager.CurrentNavigationContext;
      ItemsList result = (ItemsList) context.GetContextVariable(MENU_ITEMS_KEY, false);
      if (result == null)
      {
        result = WrapMenu(context.MenuActions.Values);
        context.SetContextVariable(MENU_ITEMS_KEY, result);
      }
      return result;
    }

    public static ItemsList WrapMenu(ICollection<WorkflowAction> actions)
    {
      ItemsList result = new ItemsList();
      foreach (WorkflowAction action in actions)
      {
        ListItem item = new ListItem("Name", action.DisplayTitle)
        {
            Command = new MethodDelegateCommand(action.Execute),
            Enabled = action.IsEnabled
        };
        result.Add(item);
      }
      return result;
    }

    #endregion
  }
}
