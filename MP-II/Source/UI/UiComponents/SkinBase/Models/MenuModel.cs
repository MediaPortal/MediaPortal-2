#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.Threading;
using MediaPortal.Core;
using MediaPortal.Core.Commands;
using MediaPortal.Core.Localization;
using MediaPortal.Core.Messaging;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Workflow;

namespace UiComponents.SkinBase.Models
{
  public class MenuModel : BaseMessageControlledUIModel
  {
    #region Consts

    protected const string MODEL_ID_STR = "9E9D0CD9-4FDB-4c0f-A0C4-F356E151BDE0";
    protected const string ITEM_ACTION_KEY = "MenuModel: Item-Action";
    protected const string REGISTERED_ACTIONS_KEY = "MenuModel: RegisteredActions";
    protected const string MENU_ITEMS_KEY = "MenuModel: MenuItems";

    #endregion

    #region Protected fields

    protected ICollection<WorkflowAction> _registeredActions = new List<WorkflowAction>();
    protected object _syncObj = new object();
    protected bool _dirty = true;

    #endregion

    public MenuModel()
    {
      SubscribeToMessages();
    }

    #region Private & protected methods

    void SubscribeToMessages()
    {
      _messageQueue.SubscribeToMessageChannel(WorkflowManagerMessaging.CHANNEL);
      _messageQueue.SubscribeToMessageChannel(MenuModelMessaging.CHANNEL);
      _messageQueue.MessageReceived += OnMessageReceived;
      _messageQueue.ThreadPriority = ThreadPriority.BelowNormal;
    }

    void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == WorkflowManagerMessaging.CHANNEL)
      {
        if (((WorkflowManagerMessaging.MessageType) message.MessageType) ==
            WorkflowManagerMessaging.MessageType.StatesPopped)
        {
          ICollection<NavigationContext> removedContexts = ((IDictionary<Guid, NavigationContext>) message.MessageData[WorkflowManagerMessaging.CONTEXTS]).Values;
          foreach (NavigationContext context in removedContexts)
            UnregisterActionChangeHandlers(context);
        }
      }
      else if (message.ChannelName == MenuModelMessaging.CHANNEL)
      {
        if (((MenuModelMessaging.MessageType) message.MessageType) ==
            MenuModelMessaging.MessageType.UpdateMenu)
          CheckUpdateMenus();
      }
    }

    void OnMenuActionStateChanged(WorkflowAction action)
    {
      Invalidate();
    }

    // Avoids too many menu updates
    protected void Invalidate()
    {
      lock (_syncObj)
      {
        _dirty = true;
        MenuModelMessaging.SendMenuMessage(MenuModelMessaging.MessageType.UpdateMenu);
      }
    }

    protected void CheckUpdateMenus()
    {
      Thread.Sleep(5);
      lock (_syncObj)
      {
        if (!_dirty)
          return;
        _dirty = false;
      }
      UpdateMenus();
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

    protected void RegisterActionChangeHandler(NavigationContext context, WorkflowAction action)
    {
      lock (context.SyncRoot)
      {
        object regs;
        ICollection<WorkflowAction> registrations;
        if (context.ContextVariables.TryGetValue(REGISTERED_ACTIONS_KEY, out regs))
          registrations = (ICollection<WorkflowAction>) regs;
        else
          context.ContextVariables[REGISTERED_ACTIONS_KEY] = registrations = new List<WorkflowAction>();
        action.StateChanged += OnMenuActionStateChanged;
        registrations.Add(action);
      }
    }

    protected void UnregisterActionChangeHandlers(NavigationContext context)
    {
      lock (context.SyncRoot)
      {
        ICollection<WorkflowAction> registeredActions =
            (ICollection<WorkflowAction>) context.GetContextVariable(REGISTERED_ACTIONS_KEY, false);
        if (registeredActions == null)
          return;
        foreach (WorkflowAction action in registeredActions)
          action.StateChanged -= OnMenuActionStateChanged;
        context.ContextVariables.Remove(REGISTERED_ACTIONS_KEY);
      }
    }

    protected ItemsList GetMenuItems(NavigationContext context)
    {
      return (ItemsList) context.GetContextVariable(MENU_ITEMS_KEY, false);
    }

    protected ItemsList GetOrCreateMenuItems(NavigationContext context)
    {
      lock (context.SyncRoot)
      {
        ItemsList result = GetMenuItems(context);
        if (result == null)
          context.ContextVariables[MENU_ITEMS_KEY] = result = new ItemsList();
        return result;
      }
    }

    /// <summary>
    /// Will be called when some actions changed their state. This will rebuild the menus without
    /// discarding the contents.
    /// </summary>
    protected void UpdateMenus()
    {
      IWorkflowManager workflowManager = ServiceScope.Get<IWorkflowManager>();
      workflowManager.Lock.EnterReadLock();
      try
      {
        foreach (NavigationContext context in workflowManager.NavigationContextStack)
          UpdateMenu(context);
      }
      finally
      {
        workflowManager.Lock.ExitReadLock();
      }
    }

    protected ItemsList UpdateMenu(NavigationContext context)
    {
      IList<WorkflowAction> actions;
      lock (context.SyncRoot)
        actions = SortActions(context.MenuActions.Values);
      bool fireListChanged = false;
      ICollection<ListItem> changed;
      ItemsList menuItems = GetOrCreateMenuItems(context);
      lock (menuItems.SyncRoot)
      {
        if (!TryUpdateMenuEntries(context, menuItems, actions, out changed))
        {
          changed = null;
          RebuildMenuEntries(context, menuItems, actions);
          fireListChanged = true;
        }
      }
      if (fireListChanged)
        menuItems.FireChange();
      if (changed != null)
        foreach (ListItem item in changed)
          item.FireChange();
      return menuItems;
    }

    /// <summary>
    /// Tries to update the given <paramref name="menuItems"/> to the given list of <paramref name="newActions"/>.
    /// </summary>
    /// <remarks>
    /// This method locks the synchronization object of the <paramref name="menuItems"/> list and thus must not call
    /// change handlers.
    /// After executing this method, the <see cref="ListItem.FireChange"/> method should be called on all list items
    /// in the returned <paramref name="changedItems"/> collection.
    /// </remarks>
    /// <param name="context">Workflow navigation context whose menu should be updated.</param>
    /// <param name="menuItems">Menu items list to update.</param>
    /// <param name="newActions">Preprocessed list (sorted etc.) of actions to be used for the new menu.</param>
    /// <param name="changedItems">Returns a collection of list items which were changed by this method.</param>
    protected bool TryUpdateMenuEntries(NavigationContext context, ItemsList menuItems, IList<WorkflowAction> newActions,
        out ICollection<ListItem> changedItems)
    {
      lock (menuItems.SyncRoot)
      {
        changedItems = null;
        int oldNumEntries = menuItems.Count;
        int newNumEntries = 0;
        foreach (WorkflowAction action in newActions)
        {
          if (!action.IsVisible)
            continue;
          if (oldNumEntries <= newNumEntries)
            return false;
          bool wasChanged = false;
          ListItem item = menuItems[newNumEntries];
          newNumEntries++;
          // Check if it is still the same action at this place
          if (item.AdditionalProperties[ITEM_ACTION_KEY] != action)
            return false;
          // Check and update all properties of the current item
          IResourceString rs;
          if (!item.Labels.TryGetValue("Name", out rs) || rs != action.DisplayTitle)
          {
            item.SetLabel("Name", action.DisplayTitle);
            wasChanged = true;
          }
          // Not easy to check equality of the command - doesn't matter, simply recreate it
          item.Command = new MethodDelegateCommand(action.Execute);
          if (item.Enabled != action.IsEnabled)
          {
            item.Enabled = action.IsEnabled;
            wasChanged = true;
          }
          if (wasChanged)
          {
            if (changedItems == null)
              changedItems = new List<ListItem>();
            changedItems.Add(item);
          }
        }
        return oldNumEntries == newNumEntries;
      }
    }

    /// <summary>
    /// Rebuilds all items of the current menu in the given <paramref name="context"/>.
    /// </summary>
    /// <remarks>
    /// This method locks the synchronization object of the <paramref name="menuItems"/> list and thus must not call
    /// change handlers.
    /// After executing this method, the returned <see cref="ItemsList"/>'s <see cref="ItemsList.FireChange"/>
    /// method must be called.
    /// </remarks>
    /// <param name="context">Workflow navigation context the menu should be built for.</param>
    /// <param name="menuItems">Menu items list to rebuild.</param>
    /// <param name="newActions">Preprocessed list (sorted etc.) of actions to be used for the new menu.</param>
    protected void RebuildMenuEntries(NavigationContext context, ItemsList menuItems, IList<WorkflowAction> newActions)
    {
      UnregisterActionChangeHandlers(context);
      lock (menuItems.SyncRoot)
      {
        menuItems.Clear();
        foreach (WorkflowAction action in newActions)
        {
          RegisterActionChangeHandler(context, action);
          if (!action.IsVisible)
            continue;
          ListItem item = new ListItem("Name", action.DisplayTitle)
              {
                Command = new MethodDelegateCommand(action.Execute),
                Enabled = action.IsEnabled,
              };
          item.AdditionalProperties[ITEM_ACTION_KEY] = action;
          menuItems.Add(item);
        }
      }
    }

    #endregion

    #region Public properties

    public override Guid ModelId
    {
      get { return new Guid(MODEL_ID_STR); }
    }

    public ItemsList MenuItems
    {
      get
      {
        NavigationContext currentContext = ServiceScope.Get<IWorkflowManager>().CurrentNavigationContext;
        lock (currentContext.SyncRoot)
        {
          ItemsList menu = GetMenuItems(currentContext);
          if (menu != null)
            return menu;
          else
            return UpdateMenu(currentContext);
        }
      }
    }

    #endregion
  }
}
