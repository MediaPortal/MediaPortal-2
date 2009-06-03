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
using MediaPortal.Core.Messaging;
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
    protected ItemsList _currentMenuItems;
    protected bool _invalid = true;
    protected object _syncObj = new object();

    #endregion

    #region Ctor

    public MenuModel()
    {
      SubscribeToMessages();
      RebuildMenu();
    }

    #endregion

    #region Protected methods

    void SubscribeToMessages()
    {
      IMessageBroker broker = ServiceScope.Get<IMessageBroker>();
      broker.Register_Sync(WorkflowManagerMessaging.QUEUE, OnWorkflowManagerMessageReceived_Sync);
      broker.Register_Async(WorkflowManagerMessaging.QUEUE, OnWorkflowManagerMessageReceived_Async);
    }

    protected override void UnsubscribeFromMessages()
    {
      base.UnsubscribeFromMessages();
      IMessageBroker broker = ServiceScope.Get<IMessageBroker>();
      broker.Unregister_Sync(WorkflowManagerMessaging.QUEUE, OnWorkflowManagerMessageReceived_Sync);
      broker.Unregister_Async(WorkflowManagerMessaging.QUEUE, OnWorkflowManagerMessageReceived_Async, true);
    }

    public override Guid ModelId
    {
      get { return new Guid(MODEL_ID_STR); }
    }

    protected void OnWorkflowManagerMessageReceived_Sync(QueueMessage message)
    {
      WorkflowManagerMessaging.MessageType messageType = (WorkflowManagerMessaging.MessageType)message.MessageData[WorkflowManagerMessaging.MESSAGE_TYPE];
      switch (messageType)
      {
        case WorkflowManagerMessaging.MessageType.StatePushed:
        case WorkflowManagerMessaging.MessageType.StatesPopped:
          // We'll delay the menu update until the navigation complete message, but remember that the menu isn't valid
          // any more - so we can update the menu if it was requested again before the navigation complete message
          // has arrived.
          // In fact, this will be the normal case if the screen gets changed when the workflow state changes,
          // i.e. before the workflow manager sends the navigation complete message, the screen will be updated.
          // But in case the screen doesn't change for example, the NavigationComplete message updates the menu.
          _invalid = true;
          break;
      }
    }

    protected void OnWorkflowManagerMessageReceived_Async(QueueMessage message)
    {
      WorkflowManagerMessaging.MessageType messageType = (WorkflowManagerMessaging.MessageType)message.MessageData[WorkflowManagerMessaging.MESSAGE_TYPE];
      switch (messageType)
      {
        case WorkflowManagerMessaging.MessageType.NavigationComplete:
          if (_invalid)
            RebuildMenu();
          break;
      }
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

    /// <summary>
    /// Will be called when the workflow state changed. This will completely rebuild the menu and
    /// discard the old menu items list instance.
    /// </summary>
    protected void RebuildMenu()
    {
      lock (_syncObj)
      {
        // We need to create a new ItemsList instance here, because if we would reuse the old instance,
        // the old screen (which is still visible) would update the menu to reflect the new menu state - which is not
        // what we want
        _currentMenuItems = new ItemsList();
        UpdateMenu();
      }
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

    protected bool MenuChanged(ICollection<WorkflowAction> newActions)
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
        _invalid = false;
        NavigationContext context = ServiceScope.Get<IWorkflowManager>().CurrentNavigationContext;
        IList<WorkflowAction> actions = SortActions(context.MenuActions.Values);
        if (!MenuChanged(actions))
          return;
        UnregisterActionChangeHandlers();

        _currentMenuItems.Clear();
        foreach (WorkflowAction action in actions)
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
        _currentMenuItems.FireChange();
      }
    }

    #endregion

    #region Public properties

    public ItemsList MenuItems
    {
      get
      {
        if (_invalid)
          RebuildMenu();
        return _currentMenuItems;
      }
    }

    #endregion
  }
}