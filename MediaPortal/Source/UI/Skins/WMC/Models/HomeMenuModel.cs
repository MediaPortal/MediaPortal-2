#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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
using System.Linq;
using System.Windows.Forms;
using HomeEditor.Groups;
using HomeEditor.Settings;
using MediaPortal.Common;
using MediaPortal.Common.Commands;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.Services.Settings;
using MediaPortal.UiComponents.SkinBase.General;
using MediaPortal.UiComponents.SkinBase.Models;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.UI.SkinEngine.MpfElements.Input;
using MediaPortal.Utilities;
using MediaPortal.Utilities.Events;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.Common.General;

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

    private readonly DelayedEvent _delayedMenuUpdateEvent;
    private readonly DelayedEvent _delayedAnimationEnableEvent;
    private NavigationList<ListItem> _navigationList;
    protected List<HomeMenuGroup> _groups;
    protected Dictionary<Guid, HomeMenuAction> _groupedActions;
    protected Dictionary<Guid, WorkflowAction> _availableActions;
    protected bool _refreshNeeded;
    protected SettingsChangeWatcher<HomeEditorSettings> _settings;

    #endregion

    #region Ctor

    public HomeMenuModel()
    {
      _enableSubMenuAnimationsProperty = new WProperty(typeof(bool), false);
      _enableMainMenuAnimationsProperty = new WProperty(typeof(bool), false);
      _scrollDirectionProperty = new WProperty(typeof(ScrollDirection), ScrollDirection.None);

      _navigationList = new NavigationList<ListItem>();
      _groupedActions = new Dictionary<Guid, HomeMenuAction>();
      _availableActions = new Dictionary<Guid, WorkflowAction>();
      NestedMenuItems = new ItemsList();
      SubItems = new ItemsList();
      _delayedMenuUpdateEvent = new DelayedEvent(200); // Update menu items only if no more requests are following after 200 ms
      _delayedMenuUpdateEvent.OnEventHandler += OnUpdateMenu;
      _delayedAnimationEnableEvent = new DelayedEvent(500);
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

    public ItemsList NestedMenuItems { get; private set; }
    public ItemsList SubItems { get; private set; }

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
      ScrollDirection = newindex > oldindex ? ScrollDirection.Down : ScrollDirection.Up;
      EnableSubMenuAnimations = false;
      UpdateList(false);
    }

    private void OnSettingsChanged(object sender, EventArgs e)
    {
      _refreshNeeded = true;
      UpdateMenu();
    }

    private void OnMenuItemsChanged(IObservable observable)
    {
      var context = ServiceRegistration.Get<IWorkflowManager>().CurrentNavigationContext;
      if (context != null && context.WorkflowState.StateId == HOME_STATE_ID)
        UpdateMenu();
    }

    private void OnUpdateMenu(object sender, EventArgs e)
    {
      if (_settings == null)
      {
        _settings = new SettingsChangeWatcher<HomeEditorSettings>();
        _settings.SettingsChanged += OnSettingsChanged;
        MenuItems.ObjectChanged += OnMenuItemsChanged;
      }
      DisableAnimations();
      UpdateList(true);
      EnableAnimations();
    }

    protected void UpdateList(bool recreateList)
    {
      bool forceSubItemUpdate = true;
      // Get new menu entries from base list
      if (recreateList)
      {
        var previousSelected = _navigationList.Current;
        UpdateAvailableActions();
        UpdateNavigationList();
        if (_navigationList.MoveTo(i => i == previousSelected))
          forceSubItemUpdate = false;
        else
          _navigationList.CurrentIndex = 0;

        NestedMenuItems.Clear();
        CollectionUtils.AddAll(NestedMenuItems, _navigationList);
      }

      bool afterSelected = false;
      for (int i = 0; i < NestedMenuItems.Count; i++)
      {
        var item = (NestedItem)NestedMenuItems[i];
        item.AfterSelected = afterSelected;
        bool selected = item == _navigationList.Current;
        item.Selected = selected;
        afterSelected |= selected;
      }
      if (recreateList)
        NestedMenuItems.FireChange();

      SetSubItems(_navigationList.Current, forceSubItemUpdate);
    }

    protected void SetSubItems(ListItem item, bool forceUpdate)
    {
      if (item == null)
        return;

      HomeMenuGroup group = item.AdditionalProperties[KEY_ITEM_GROUP] as HomeMenuGroup;
      bool fireChange = false;
      List<WorkflowAction> actions = GetGroupActions(group);
      if (forceUpdate || SubItemsNeedUpdate(SubItems, actions))
      {
        SubItems.Clear();
        CollectionUtils.AddAll(SubItems, CreateSubItems(actions));
        fireChange = true;
      }
      FocusCurrentSubItem(item);
      if (fireChange)
        SubItems.FireChange();
    }

    protected void UpdateGroups()
    {
      var settingsGroups = _settings.Settings.Groups;
      _groups = new List<HomeMenuGroup>();
      if (settingsGroups != null && settingsGroups.Count > 0)
        _groups.AddRange(settingsGroups);
      else
        _groups.AddRange(DefaultGroups.Create());
    }

    protected void UpdateAvailableActions()
    {
      UninitializeActions();
      _availableActions.Clear();
      foreach (ListItem item in MenuItems)
      {
        WorkflowAction action;
        if (TryGetAction(item, out action))
          _availableActions[action.ActionId] = action;
      }

      var customActions = ServiceRegistration.Get<IWorkflowManager>().MenuStateActions.Values
        .Where(a => a.SourceStateIds != null && a.SourceStateIds.Contains(CUSTOM_HOME_STATE_ID));
      foreach (WorkflowAction action in customActions)
        _availableActions[action.ActionId] = action;
      InitializeActions();
    }

    protected void InitializeActions()
    {
      foreach (WorkflowAction action in _availableActions.Values)
        action.AddRef();
    }

    protected void UninitializeActions()
    {
      foreach (WorkflowAction action in _availableActions.Values)
        action.RemoveRef();
    }

    protected void UpdateNavigationList()
    {
      if (_groups != null && !_refreshNeeded)
        return;

      _refreshNeeded = false;
      UpdateGroups();
      _groupedActions.Clear();
      _navigationList.Clear();
      foreach (HomeMenuGroup group in _groups)
      {
        foreach (HomeMenuAction action in group.Actions)
          _groupedActions[action.ActionId] = action;
        NestedItem item = new NestedItem(Consts.KEY_NAME, group.DisplayName);
        item.AdditionalProperties[KEY_ITEM_GROUP] = group;
        _navigationList.Add(item);
      }
      //Entry for all actions without a group
      NestedItem extrasItem = new NestedItem(Consts.KEY_NAME, LocalizationHelper.CreateResourceString(_settings.Settings.OthersGroupName).Evaluate());
      _navigationList.Add(extrasItem);
    }

    protected List<WorkflowAction> GetGroupActions(HomeMenuGroup group)
    {
      var availableActions = _availableActions;
      List<WorkflowAction> actions;
      if (group == null)
      {
        actions = _availableActions.Values.Where(a => !_groupedActions.ContainsKey(a.ActionId)).ToList();
        actions.Sort(Compare);
      }
      else
      {
        actions = new List<WorkflowAction>();
        foreach (var actionItem in group.Actions)
        {
          WorkflowAction action;
          if (availableActions.TryGetValue(actionItem.ActionId, out action))
            actions.Add(action);
        }
      }
      return actions;
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

    protected List<ListItem> CreateSubItems(List<WorkflowAction> actions)
    {
      List<ListItem> items = new List<ListItem>();
      foreach (var action in actions)
      {
        WorkflowAction workflowAction = action;
        HomeMenuAction groupedAction;
        ListItem listItem;
        if (_groupedActions.TryGetValue(workflowAction.ActionId, out groupedAction))
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
      for (int i = 0; i < SubItems.Count; i++)
      {
        SubItems[i].Selected = (currentActionId == null && i == 0) ||
          (TryGetAction(SubItems[i], out action) && action.ActionId == currentActionId);
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
