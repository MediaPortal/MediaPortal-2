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
using MediaPortal.Common.Commands;
using MediaPortal.Common.Logging;
using MediaPortal.Common;
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

    protected const string KEY_NAME = "Name";
    protected const string KEY_ISFIRST = "IsFirst";
    protected const string KEY_ISLAST = "IsLast";

    #endregion

    #region Protected fields

    protected NavigationContext _boundContext = null;

    #endregion

    public override void Dispose()
    {
      base.Dispose();
      if (_boundContext != null)
        ClearNavigationItems(_boundContext);
    }

    #region Protected methods

    protected static ItemsList GetNavigationItems(NavigationContext context)
    {
      return (ItemsList) context.GetContextVariable(NAVIGATION_ITEMS_KEY, false);
    }

    protected static ItemsList GetOrCreateNavigationItems(NavigationContext context)
    {
      lock (context.SyncRoot)
      {
        ItemsList result = GetNavigationItems(context);
        if (result == null)
          context.ContextVariables[NAVIGATION_ITEMS_KEY] = result = new ItemsList();
        return result;
      }
    }

    protected static void ClearNavigationItems(NavigationContext context)
    {
      context.ResetContextVariable(NAVIGATION_ITEMS_KEY);
    }

    protected static ItemsList UpdateNavigationItems(NavigationContext context)
    {
      try
      {
        ItemsList navigationItems = GetOrCreateNavigationItems(context);
        Stack<NavigationContext> contextStack = ServiceRegistration.Get<IWorkflowManager>().NavigationContextStack;
        List<NavigationContext> contexts = new List<NavigationContext>(contextStack);
        contexts.Reverse();
        navigationItems.Clear();

        for (int index = 0; index < contexts.Count; index++)
        {
          NavigationContext ctx = contexts[index];
          ListItem item = new ListItem(KEY_NAME, ctx.DisplayLabel);
          item.AdditionalProperties[KEY_ISFIRST] = index == 0;
          item.AdditionalProperties[KEY_ISLAST] = index == contexts.Count -1;
          NavigationContext contextCopy = ctx;
          item.Command = new MethodDelegateCommand(() => WorkflowPopToState(contextCopy.WorkflowState.StateId));
          navigationItems.Add(item);
        }
        navigationItems.FireChange();
        return navigationItems;
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("WorkflowNavigationBar: Error updating properties", e);
        return null;
      }
    }

    protected static void WorkflowPopToState(Guid workflowStateId)
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePopToState(workflowStateId, false);
    }

    #endregion

    #region Public members to be accessed via the GUI

    public ItemsList NavigationItems
    {
      get
      {
        if (_boundContext == null)
          _boundContext = ServiceRegistration.Get<IWorkflowManager>().CurrentNavigationContext;
        lock (_boundContext.SyncRoot)
        {
          ItemsList navigationItems = GetNavigationItems(_boundContext);
          return navigationItems ?? UpdateNavigationItems(_boundContext);
        }
      }
    }

    #endregion
  }
}
