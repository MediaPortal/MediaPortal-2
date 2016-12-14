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

using HomeEditor.Groups;
using MediaPortal.Common;
using MediaPortal.Common.Commands;
using MediaPortal.Common.General;
using MediaPortal.Common.Messaging;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.UI.SkinEngine.MpfElements.Input;
using MediaPortal.UiComponents.SkinBase.General;
using MediaPortal.UiComponents.SkinBase.Models;
using MediaPortal.Utilities;
using MediaPortal.Utilities.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace MediaPortal.UiComponents.WMCSkin.Models
{
  public enum ScrollDirection
  {
    None,
    Up,
    Down
  }

  public class HomeMenuModel : MenuModel
  {
    #region Protected Members

    public static readonly Guid MODEL_ID = new Guid("2EAA2DAB-241F-432F-A487-CDD35CCD4309");
    public static readonly Guid HOME_STATE_ID = new Guid("7F702D9C-F2DD-42da-9ED8-0BA92F07787F");
    public static readonly Guid CUSTOM_HOME_STATE_ID = new Guid("B285DC02-AA8C-47F2-8795-0B13B6E66306");
    protected const string KEY_ITEM_GROUP = "HomeMenuModel: Group";
    protected const string KEY_ITEM_SELECTED_ACTION_ID = "HomeMenuModel: SelectedActionId";
    
    protected AbstractProperty _enableSubMenuAnimationsProperty;
    protected AbstractProperty _enableMainMenuAnimationsProperty;
    protected AbstractProperty _scrollDirectionProperty;

    protected readonly object _syncOb = new object();
    protected HomeMenuActionProxy _homeProxy;
    protected bool _updatingMenu;
    protected bool _attachedToMenuItems;
    protected NavigationList<ListItem> _navigationList;
    protected ItemsList _mainItems;
    protected ItemsList _subItems;

    private readonly DelayedEvent _delayedMenuUpdateEvent;
    private readonly DelayedEvent _delayedAnimationEnableEvent;

    #endregion

    #region Ctor

    public HomeMenuModel()
    {
      _enableSubMenuAnimationsProperty = new WProperty(typeof(bool), false);
      _enableMainMenuAnimationsProperty = new WProperty(typeof(bool), false);
      _scrollDirectionProperty = new WProperty(typeof(ScrollDirection), ScrollDirection.None);

      _homeProxy = new HomeMenuActionProxy();
      _navigationList = new NavigationList<ListItem>();
      _mainItems = new ItemsList();
      _subItems = new ItemsList();
      _delayedMenuUpdateEvent = new DelayedEvent(200); // Update menu items only if no more requests are following after 200 ms
      _delayedMenuUpdateEvent.OnEventHandler += OnUpdateMenu;
      _delayedAnimationEnableEvent = new DelayedEvent(200);
      _delayedAnimationEnableEvent.OnEventHandler += OnEnableAnimations;
      _navigationList.OnCurrentChanged += OnNavigationListCurrentChanged;
      SubscribeToMessages();
    }

    #endregion

    #region Message Handling

    private void SubscribeToMessages()
    {
      if (_messageQueue == null)
        return;
      _messageQueue.MessageReceived += OnMessageReceived;
    }

    private void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == WorkflowManagerMessaging.CHANNEL)
      {
        WorkflowManagerMessaging.MessageType messageType = (WorkflowManagerMessaging.MessageType)message.MessageType;
        if (messageType == WorkflowManagerMessaging.MessageType.StatePushed)
        {
          if (((NavigationContext)message.MessageData[WorkflowManagerMessaging.CONTEXT]).WorkflowState.StateId == HOME_STATE_ID)
            UpdateMenu();
        }
        else if (messageType == WorkflowManagerMessaging.MessageType.NavigationComplete)
        {
          var context = ServiceRegistration.Get<IWorkflowManager>().CurrentNavigationContext;
          if (context != null && context.WorkflowState.StateId == HOME_STATE_ID)
            UpdateMenu();
        }
      }
    }

    #endregion

    #region Public Properties

    public ItemsList NestedMenuItems
    {
      get { return _mainItems; }
    }

    public ItemsList SubItems
    {
      get { return _subItems; }
    }

    public AbstractProperty EnableSubMenuAnimationsProperty
    {
      get { return _enableSubMenuAnimationsProperty; }
    }

    public bool EnableSubMenuAnimations
    {
      get { return (bool)_enableSubMenuAnimationsProperty.GetValue(); }
      set { _enableSubMenuAnimationsProperty.SetValue(value); }
    }

    public AbstractProperty EnableMainMenuAnimationsProperty
    {
      get { return _enableMainMenuAnimationsProperty; }
    }

    public bool EnableMainMenuAnimations
    {
      get { return (bool)_enableMainMenuAnimationsProperty.GetValue(); }
      set { _enableMainMenuAnimationsProperty.SetValue(value); }
    }

    public AbstractProperty ScrollDirectionProperty
    {
      get { return _scrollDirectionProperty; }
    }

    public ScrollDirection ScrollDirection
    {
      get { return (ScrollDirection)_scrollDirectionProperty.GetValue(); }
      set { _scrollDirectionProperty.SetValue(value); }
    }

    #endregion

    #region Public Methods

    public void MoveNext()
    {
      _navigationList.MoveNext();
    }

    public void MovePrevious()
    {
      _navigationList.MovePrevious();
    }

    public void SetSelectedItem(object sender, SelectionChangedEventArgs e)
    {
      var item = e.FirstAddedItem as ListItem;
      if (item != null)
      {
        SetCurrentSubItem(item);
        if (EnableMainMenuAnimations)
          EnableSubMenuAnimations = true;
      }
    }

    public void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
      if (e.NumDetents > 0)
        _navigationList.MovePrevious(e.NumDetents);
      else
        _navigationList.MoveNext(-e.NumDetents);
    }

    public void CloseTopmostDialog(MouseButtons buttons, float x, float y)
    {
      ServiceRegistration.Get<IScreenManager>().CloseTopmostDialog();
    }

    #endregion

    #region Enable/Disable Animations

    public void EnableAnimations()
    {
      _delayedAnimationEnableEvent.EnqueueEvent(this, EventArgs.Empty);
    }

    public void DisableAnimations()
    {
      EnableSubMenuAnimations = false;
      EnableMainMenuAnimations = false;
      ScrollDirection = ScrollDirection.None;
    }

    protected void OnEnableAnimations(object sender, EventArgs e)
    {
      EnableSubMenuAnimations = true;
      EnableMainMenuAnimations = true;
    }

    #endregion

    #region Menu Update

    protected void UpdateMenu()
    {
      _delayedMenuUpdateEvent.EnqueueEvent(this, EventArgs.Empty);
    }

    private void OnNavigationListCurrentChanged(int oldindex, int newindex)
    {
      lock (_syncOb)
      {
        if (_updatingMenu)
          return;
        _updatingMenu = true;
      }
      try
      {
        ScrollDirection = newindex > oldindex ? ScrollDirection.Down : ScrollDirection.Up;
        EnableSubMenuAnimations = false;
        UpdateList(false);
      }
      finally
      {
        _updatingMenu = false;
      }
    }

    private void OnUpdateMenu(object sender, EventArgs e)
    {
      lock (_syncOb)
      {
        if (_updatingMenu)
          return;
        _updatingMenu = true;
      }
      try
      {
        AttachToMenuItems();
        UpdateList(true);
        EnableAnimations();
      }
      finally
      {
        _updatingMenu = false;
      }
    }

    protected void AttachToMenuItems()
    {
      if (_attachedToMenuItems)
        return;
      MenuItems.ObjectChanged += OnMenuItemsChanged;
      _attachedToMenuItems = true;
    }

    private void OnMenuItemsChanged(IObservable observable)
    {
      var context = ServiceRegistration.Get<IWorkflowManager>().CurrentNavigationContext;
      if (context != null && context.WorkflowState.StateId == HOME_STATE_ID)
        UpdateMenu();
    }

    protected void UpdateList(bool recreateList)
    {
      bool forceSubItemUpdate = true;
      // Get new menu entries from base list
      if (recreateList)
      {
        if (UpdateNavigationList())
        {
          _mainItems.Clear();
          CollectionUtils.AddAll(_mainItems, _navigationList);
          _navigationList.CurrentIndex = 0;
        }
        else
        {
          forceSubItemUpdate = false;
          recreateList = false;
        }
      }

      bool afterSelected = false;
      for (int i = 0; i < _mainItems.Count; i++)
      {
        var item = (NestedItem)_mainItems[i];
        item.AfterSelected = afterSelected;
        bool selected = item == _navigationList.Current;
        item.Selected = selected;
        afterSelected |= selected;
      }
      if (recreateList)
        _mainItems.FireChange();
      SetSubItems(_navigationList.Current, forceSubItemUpdate);
    }

    protected void SetSubItems(ListItem item, bool forceUpdate)
    {
      if (item == null)
        return;

      HomeMenuGroup group = item.AdditionalProperties[KEY_ITEM_GROUP] as HomeMenuGroup;
      bool fireChange = false;
      var actions = _homeProxy.GetGroupActions(group);
      if (forceUpdate || SubItemsNeedUpdate(_subItems, actions))
      {
        _subItems.Clear();
        CollectionUtils.AddAll(_subItems, CreateSubItems(actions));
        fireChange = true;
      }
      FocusCurrentSubItem(item);
      if (fireChange)
        _subItems.FireChange();
    }

    protected bool UpdateNavigationList()
    {
      _homeProxy.UpdateActions(MenuItems);
      if (!_homeProxy.GroupsUpdated)
        return false; ;
      _navigationList.Clear();
      foreach (HomeMenuGroup group in _homeProxy.Groups)
      {
        NestedItem item = new NestedItem(Consts.KEY_NAME, group.DisplayName);
        item.AdditionalProperties[KEY_ITEM_GROUP] = group;
        _navigationList.Add(item);
      }
      //Entry for all actions without a group
      NestedItem extrasItem = new NestedItem(Consts.KEY_NAME, _homeProxy.OthersName.Evaluate());
      _navigationList.Add(extrasItem);
      return true;
    }

    protected bool SubItemsNeedUpdate(IList<ListItem> currentItems, IList<WorkflowAction> actions)
    {
      if (currentItems.Count != actions.Count)
        return true;
      if (currentItems.Count == 0)
        return false;

      int currentIndex = 0;
      foreach (ListItem item in currentItems)
      {
        WorkflowAction action = actions[currentIndex++];
        if (item.AdditionalProperties[Consts.KEY_ITEM_ACTION] != action)
          return true;
      }
      return false;
    }

    protected List<ListItem> CreateSubItems(IList<WorkflowAction> actions)
    {
      var groupedActions = _homeProxy.GroupedActions;
      List<ListItem> items = new List<ListItem>();
      foreach (var action in actions)
      {
        WorkflowAction workflowAction = action;
        HomeMenuAction groupedAction;
        ListItem listItem;
        if (groupedActions.TryGetValue(workflowAction.ActionId, out groupedAction))
          listItem = new ListItem(Consts.KEY_NAME, groupedAction.DisplayName);
        else
          listItem = new ListItem(Consts.KEY_NAME, workflowAction.DisplayTitle);
        listItem.AdditionalProperties[Consts.KEY_ITEM_ACTION] = workflowAction;
        listItem.Command = new MethodDelegateCommand(workflowAction.Execute);
        items.Add(listItem);
      }
      return items;
    }

    protected void SetCurrentSubItem(ListItem item)
    {
      ListItem currentItem = _navigationList.Current;
      WorkflowAction action;
      if (currentItem != null && TryGetAction(item, out action))
        currentItem.AdditionalProperties[KEY_ITEM_SELECTED_ACTION_ID] = action.ActionId;
    }

    protected void FocusCurrentSubItem(ListItem parentItem)
    {
      Guid? currentActionId = null;
      if (parentItem != null)
        currentActionId = parentItem.AdditionalProperties[KEY_ITEM_SELECTED_ACTION_ID] as Guid?;

      WorkflowAction action;
      for (int i = 0; i < _subItems.Count; i++)
      {
        _subItems[i].Selected = (currentActionId == null && i == 0) ||
          (TryGetAction(_subItems[i], out action) && action.ActionId == currentActionId);
      }
    }

    protected static bool TryGetAction(ListItem item, out WorkflowAction action)
    {
      if (item != null)
      {
        action = item.AdditionalProperties[Consts.KEY_ITEM_ACTION] as WorkflowAction;
        return action != null;
      }
      action = null;
      return false;
    }

    #endregion
  }
}
