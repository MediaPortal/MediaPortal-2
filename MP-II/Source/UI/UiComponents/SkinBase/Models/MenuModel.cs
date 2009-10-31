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

using System;
using System.Collections.Generic;
using MediaPortal.Core;
using MediaPortal.Core.Commands;
using MediaPortal.Core.Localization;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.Presentation.Models;
using MediaPortal.Presentation.Workflow;

namespace UiComponents.SkinBase.Models
{
  public class MenuModel : BaseMessageControlledUIModel
  {
    #region Consts

    protected const string MODEL_ID_STR = "9E9D0CD9-4FDB-4c0f-A0C4-F356E151BDE0";
    protected const string ITEM_ACTION_KEY = "MenuModel: Item-Action";

    #endregion

    #region Protected fields

    protected ICollection<WorkflowAction> _registeredActions = new List<WorkflowAction>();
    protected ItemsList _currentMenuItems = null;
    protected object _syncObj = new object();
    protected Guid _currentWorkflowStateId = Guid.Empty;

    #endregion

    #region Protected methods

    public override Guid ModelId
    {
      get { return new Guid(MODEL_ID_STR); }
    }

    protected void OnMenuActionStateChanged(WorkflowAction action)
    {
      // TODO: Can we optimize this? If multiple actions change their state simultaneously, we only need one update
      UpdateMenu();
    }

    protected static int Compare(string a, string b)
    {
      if (string.IsNullOrEmpty(a))
        if (b == null)
          return 0; // a == null, b == null
        else
          return 1; // a == null, b != null
      else
        if (b == null)
          return -1; // a != null, b == null
      return a.CompareTo(b);
    }

    protected static int Compare(WorkflowAction a, WorkflowAction b)
    {
      int res = Compare(a.DisplayCategory, b.DisplayCategory);
      if (res != 0)
        return res;
      return Compare(a.SortOrder, b.SortOrder);
    }

    protected static IList<WorkflowAction> SortActions(IEnumerable<WorkflowAction> actions)
    {
      List<WorkflowAction> result = new List<WorkflowAction>(actions);
      result.Sort(Compare);
      return result;
    }

    protected void RegisterActionChangeHandler(WorkflowAction action)
    {
      action.StateChanged += OnMenuActionStateChanged;
      _registeredActions.Add(action);
    }

    protected void UnregisterActionChangeHandlers()
    {
      foreach (WorkflowAction action in _registeredActions)
        action.StateChanged -= OnMenuActionStateChanged;
      _registeredActions.Clear();
    }

    protected bool MenuEntriesChanged(ICollection<WorkflowAction> newActions)
    {
      if (_currentMenuItems == null)
        return true;
      int oldNumEntries = _currentMenuItems.Count;
      int newNumEntries = 0;
      foreach (WorkflowAction action in newActions)
      {
        if (!action.IsVisible)
          continue;
        if (oldNumEntries <= newNumEntries)
          return true;
        ListItem item = _currentMenuItems[newNumEntries];
        newNumEntries++;
        if (item.AdditionalProperties[ITEM_ACTION_KEY] != action)
          return true;
      }
      return oldNumEntries != newNumEntries;
    }

    /// <summary>
    /// Will be called when some actions changed their state. This will rebuild the menu without
    /// discarding the contents.
    /// </summary>
    protected void UpdateMenu()
    {
      lock (_syncObj)
      {
        NavigationContext context = ServiceScope.Get<IWorkflowManager>().CurrentNavigationContext;
        if (_currentWorkflowStateId == context.WorkflowState.StateId)
          return;
        _currentWorkflowStateId = context.WorkflowState.StateId;
        IList<WorkflowAction> actions = SortActions(context.MenuActions.Values);
        if (MenuEntriesChanged(actions))
          RebuildMenuEntries(actions);
        else
          UpdateMenuEntries(actions);
      }
    }

    protected void RebuildMenuEntries(IList<WorkflowAction> newActions)
    {
      lock (_syncObj)
      {
        UnregisterActionChangeHandlers();
        _currentMenuItems = new ItemsList();
        foreach (WorkflowAction action in newActions)
        {
          RegisterActionChangeHandler(action);
          if (!action.IsVisible)
            continue;
          ListItem item = new ListItem("Name", action.DisplayTitle)
              {
                Command = new MethodDelegateCommand(action.Execute),
                Enabled = action.IsEnabled,
              };
          item.AdditionalProperties[ITEM_ACTION_KEY] = action;
          _currentMenuItems.Add(item);
        }
      }
    }

    protected void UpdateMenuEntries(IList<WorkflowAction> newActions)
    {
      lock (_syncObj)
      {
        int i = 0;
        foreach (WorkflowAction action in newActions)
        {
          ListItem item = _currentMenuItems[i++];
          if (item.AdditionalProperties[ITEM_ACTION_KEY] != action)
            // Should not happen - this was checked by method MenuEntriesChanged
            break;
          if (!action.IsVisible)
            continue;
          IResourceString rs;
          if (!item.Labels.TryGetValue("Name", out rs) || rs != action.DisplayTitle)
            item.SetLabel("Name", action.DisplayTitle);
          item.Command = new MethodDelegateCommand(action.Execute);
          item.Enabled = action.IsEnabled;
        }
      }
    }

    #endregion

    #region Public properties

    public ItemsList MenuItems
    {
      get
      {
        UpdateMenu();
        return _currentMenuItems;
      }
    }

    #endregion
  }
}