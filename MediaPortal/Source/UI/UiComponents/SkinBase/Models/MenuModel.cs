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
using System.Threading;
using MediaPortal.Common;
using MediaPortal.Common.Commands;
using MediaPortal.Common.General;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.Runtime;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.SkinBase.General;

namespace MediaPortal.UiComponents.SkinBase.Models
{
  public class MenuModel : BaseMessageControlledModel
  {
    #region Consts

    public const string STR_MENU_MODEL_ID = "9E9D0CD9-4FDB-4c0f-A0C4-F356E151BDE0";
    public static readonly Guid MENU_MODEL_ID = new Guid(STR_MENU_MODEL_ID);

    #endregion

    #region Protected fields

    protected ICollection<WorkflowAction> _registeredActions = new List<WorkflowAction>();
    protected AbstractProperty _isMenuOpenProperty = new WProperty(typeof(bool), true);
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
      if (!IsSystemActive())
        return;
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

    protected bool IsSystemActive()
    {
      ISystemStateService sss = ServiceRegistration.Get<ISystemStateService>();
      return sss.CurrentState == SystemState.Running;
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
        if (context.ContextVariables.TryGetValue(Consts.KEY_REGISTERED_ACTIONS, out regs))
          registrations = (ICollection<WorkflowAction>) regs;
        else
          context.ContextVariables[Consts.KEY_REGISTERED_ACTIONS] = registrations = new List<WorkflowAction>();
        action.StateChanged += OnMenuActionStateChanged;
        registrations.Add(action);
      }
    }

    protected void UnregisterActionChangeHandlers(NavigationContext context)
    {
      lock (context.SyncRoot)
      {
        ICollection<WorkflowAction> registeredActions =
            (ICollection<WorkflowAction>) context.GetContextVariable(Consts.KEY_REGISTERED_ACTIONS, false);
        if (registeredActions == null)
          return;
        foreach (WorkflowAction action in registeredActions)
          action.StateChanged -= OnMenuActionStateChanged;
        context.ContextVariables.Remove(Consts.KEY_REGISTERED_ACTIONS);
      }
    }

    protected ItemsList GetMenuItems(NavigationContext context)
    {
      return (ItemsList) context.GetContextVariable(Consts.KEY_MENU_ITEMS, false);
    }

    protected ItemsList GetOrCreateMenuItems(NavigationContext context)
    {
      lock (context.SyncRoot)
      {
        ItemsList result = GetMenuItems(context);
        if (result == null)
          context.ContextVariables[Consts.KEY_MENU_ITEMS] = result = new ItemsList();
        return result;
      }
    }

    /// <summary>
    /// Will be called when some actions changed their state. This will rebuild the menus without
    /// discarding the contents.
    /// </summary>
    protected void UpdateMenus()
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
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
    /// Updates the given <paramref name="menuItems"/>, each menu item for itself, to the given list of <paramref name="newActions"/>.
    /// This method only succeeds if the actions given in <paramref name="newActions"/> match the menu items given in
    /// <paramref name="menuItems"/>.
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
    /// <returns><c>true</c>, if the given <paramref name="newActions"/> match the actions behind the menu items in
    /// <paramref name="menuItems"/>, else <c>false</c>.</returns>
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
          if (!action.IsVisible(context))
            continue;
          if (oldNumEntries <= newNumEntries)
            return false;
          bool wasChanged = false;
          ListItem item = menuItems[newNumEntries];
          newNumEntries++;
          // Check if it is still the same action at this place
          if (item.AdditionalProperties[Consts.KEY_ITEM_ACTION] != action)
            return false;
          // Check and update all properties of the current item
          IResourceString rs;
          if (!item.Labels.TryGetValue("Name", out rs) || !Equals(rs, action.DisplayTitle))
          {
            item.SetLabel("Name", action.DisplayTitle);
            wasChanged = true;
          }
          if (!item.Labels.TryGetValue("Help", out rs) || !Equals(rs, action.HelpText))
          {
            item.SetLabel("Help", action.HelpText);
            wasChanged = true;
          }
          // Not easy to check equality of the command - doesn't matter, simply recreate it
          item.Command = new MethodDelegateCommand(action.Execute);
          bool actionEnabled = action.IsEnabled(context);
          if (item.Enabled != actionEnabled)
          {
            item.Enabled = actionEnabled;
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
          if (!action.IsVisible(context))
            continue;
          ListItem item = new ListItem("Name", action.DisplayTitle)
              {
                Command = new MethodDelegateCommand(action.Execute),
                Enabled = action.IsEnabled(context),
              };
          item.AdditionalProperties[Consts.KEY_ITEM_ACTION] = action;
          item.SetLabel("Help", action.HelpText);
          menuItems.Add(item);
        }
      }
    }

    #endregion

    #region Public properties

    public Guid ModelId
    {
      get { return MENU_MODEL_ID; }
    }

    public ItemsList MenuItems
    {
      get
      {
        NavigationContext currentContext = ServiceRegistration.Get<IWorkflowManager>().CurrentNavigationContext;
        lock (currentContext.SyncRoot)
        {
          ItemsList menu = GetMenuItems(currentContext);
          if (menu != null)
            return menu;
          return UpdateMenu(currentContext);
        }
      }
    }

    public AbstractProperty IsMenuOpenProperty
    {
      get { return _isMenuOpenProperty; }
    }

    /// <summary>
    /// Gets or sets an indicator if the menu is open (<c>true</c>) or closed (<c>false</c>).
    /// </summary>
    public bool IsMenuOpen
    {
      get { return (bool) _isMenuOpenProperty.GetValue(); }
      set { _isMenuOpenProperty.SetValue(value); }
    }

    /// <summary>
    /// Toggles the menu state from open to close and back.
    /// </summary>
    public void ToggleMenu()
    {
      IsMenuOpen = !IsMenuOpen;
    }

    /// <summary>
    /// Opens the menu by setting the <see cref="IsMenuOpen"/> to <c>true</c>.
    /// </summary>
    public void OpenMenu()
    {
      IsMenuOpen = true;
    }

    /// <summary>
    /// Closes the menu by setting the <see cref="IsMenuOpen"/> to <c>false</c>.
    /// </summary>
    public void CloseMenu()
    {
      IsMenuOpen = false;
    }

    #endregion
  }
}
