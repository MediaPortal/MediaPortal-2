#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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

using System;
using System.Collections.Generic;
using MediaPortal.Core.Logging;
using MediaPortal.Core;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Workflow;

namespace MediaPortal.UI.SkinEngine.SpecialElements.Controls
{
  /// <summary>
  /// Visible control providing the workflow navigation bar for the skin.
  /// </summary>
  public class WorkflowNavigationBar : SkinEngine.Controls.Visuals.Control
  {
    #region Consts

    protected const string NAVIGATION_ITEMS_KEY = "WorkflowNavigationBar: NAVIGATION_ITEMS";
    
    protected const string NAME_KEY = "Name";

    #endregion

    #region Protected methods

    protected ItemsList GetNavigationItems(NavigationContext context)
    {
      return (ItemsList) context.GetContextVariable(NAVIGATION_ITEMS_KEY, false);
    }

    protected ItemsList GetOrCreateNavigationItems(NavigationContext context)
    {
      lock (context.SyncRoot)
      {
        ItemsList result = GetNavigationItems(context);
        if (result == null)
          context.ContextVariables[NAVIGATION_ITEMS_KEY] = result = new ItemsList();
        return result;
      }
    }

    protected ItemsList UpdateNavigationItems(NavigationContext currentContext)
    {
      try
      {
        ItemsList navigationItems = GetOrCreateNavigationItems(currentContext);
        Stack<NavigationContext> contextStack = ServiceScope.Get<IWorkflowManager>().NavigationContextStack;
        List<NavigationContext> contexts = new List<NavigationContext>(contextStack);
        contexts.Reverse();
        navigationItems.Clear();
        foreach (NavigationContext context in contexts)
        {
          ListItem item = new ListItem(NAME_KEY, context.DisplayLabel);
          navigationItems.Add(item);
        }
        navigationItems.FireChange();
        return navigationItems;
      }
      catch (Exception e)
      {
        ServiceScope.Get<ILogger>().Warn("WorkflowNavigationBar: Error updating properties", e);
        return null;
      }
    }

    #endregion

    #region Public members to be accessed via the GUI

    public ItemsList NavigationItems
    {
      get
      {
        NavigationContext currentContext = ServiceScope.Get<IWorkflowManager>().CurrentNavigationContext;
        lock (currentContext.SyncRoot)
        {
          ItemsList navigationItems = GetNavigationItems(currentContext);
          if (navigationItems != null)
            return navigationItems;
          else
            return UpdateNavigationItems(currentContext);
        }
      }
    }

    #endregion
  }
}
