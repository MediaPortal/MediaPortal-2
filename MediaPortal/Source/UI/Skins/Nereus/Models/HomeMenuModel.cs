#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

using MediaPortal.Common;
using MediaPortal.Common.Commands;
using MediaPortal.Common.General;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.Services.Settings;
using MediaPortal.Extensions.UserServices.FanArtService.Client.Models;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.UiComponents.Media.Models;
using MediaPortal.UiComponents.Nereus.Actions;
using MediaPortal.UiComponents.Nereus.Models.HomeContent;
using MediaPortal.UiComponents.Nereus.Settings;
using MediaPortal.UiComponents.SkinBase.General;
using MediaPortal.UiComponents.SkinBase.Models;
using MediaPortal.Utilities;
using MediaPortal.Utilities.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace MediaPortal.UiComponents.Nereus.Models
{
  public class HomeMenuModel : BaseMessageControlledModel
  {
    protected class ActionEventArgs : EventArgs
    {
      public WorkflowAction Action { get; set; }
    }

    public static readonly Guid MODEL_ID = new Guid("CED34107-565C-48D9-BEC8-195F7969F90F");
    public static readonly Guid HOME_STATE_ID = new Guid("7F702D9C-F2DD-42da-9ED8-0BA92F07787F");
    public static readonly Guid APPS_LIST_MODEL_ID = new Guid("E35E2C12-1B97-43EE-B7A2-D1527DF41D89");

    protected const int UPDATE_DELAY_MS = 500;

    protected readonly object _syncObj = new object();

    protected AbstractProperty _content1Property;
    protected AbstractProperty _content2Property;
    protected AbstractProperty[] _contentProperties;

    protected AbstractProperty _contentIndexProperty;
    protected AbstractProperty _selectedItemProperty;
    
    protected DelayedEvent _updateEvent;

    protected IDictionary<Guid, object> _homeContent = new Dictionary<Guid, object>();
    protected static readonly DefaultHomeContent DEFAULT_HOME_CONTENT = new DefaultHomeContent();

    protected SettingsChangeWatcher<NereusSkinSettings> _settingsWatcher;

    protected ItemsList _allHomeMenuItems;
    protected ItemsList _mainMenuItems = new ItemsList();
    protected ItemsList _otherMenuItems = new ItemsList();

    protected ListItem _otherPluginsMenuItem;

    protected bool _isAttachedToMenuItems = false;

    private const int CONTENT_LIST_LIMIT = 6;

    public HomeMenuModel()
    {
      _content1Property = new WProperty(typeof(object), null);
      _content2Property = new WProperty(typeof(object), null);
      _contentProperties = new[] { _content1Property, _content2Property };

      _contentIndexProperty = new WProperty(typeof(int), 0);
      _selectedItemProperty = new WProperty(typeof(ListItem), null);

      _updateEvent = new DelayedEvent(UPDATE_DELAY_MS);
      _updateEvent.OnEventHandler += OnUpdate;
      _selectedItemProperty.Attach(OnSelectedItemChanged);

      GetMediaListModel().Limit = CONTENT_LIST_LIMIT;
      GetAppListModel().Limit = CONTENT_LIST_LIMIT;

      _homeContent.Add(new Guid("80d2e2cc-baaa-4750-807b-f37714153751"), new MovieHomeContent());
      _homeContent.Add(new Guid("30f57cba-459c-4202-a587-09fff5098251"), new SeriesHomeContent());
      _homeContent.Add(new Guid("30715d73-4205-417f-80aa-e82f0834171f"), new AudioHomeContent());
      _homeContent.Add(new Guid("55556593-9fe9-436c-a3b6-a971e10c9d44"), new ImageHomeContent());
      _homeContent.Add(new Guid("A4DF2DF6-8D66-479a-9930-D7106525EB07"), new VideoHomeContent());
      _homeContent.Add(new Guid("b4a9199f-6dd4-4bda-a077-de9c081f7703"), new TVHomeContent());
      _homeContent.Add(new Guid("bb49a591-7705-408f-8177-45d633fdfad0"), new NewsHomeContent());
      _homeContent.Add(new Guid("e34fdb62-1f3e-4aa9-8a61-d143e0af77b5"), new WeatherHomeContent());
      _homeContent.Add(new Guid("873eb147-c998-4632-8f86-d5e24062be2e"), new LauncherHomeContent());

      // Home content for displaying a list of all other plugins
      _homeContent.Add(OtherPluginsAction.ACTION_ID, new OtherPluginsHomeContent(_otherMenuItems));
      
      SubscribeToMessages();
    }

    #region Message Handling

    private void SubscribeToMessages()
    {
      _settingsWatcher = new SettingsChangeWatcher<NereusSkinSettings>(true);
      _settingsWatcher.SettingsChanged += OnSettingsChanged;

      _messageQueue.SubscribeToMessageChannel(WorkflowManagerMessaging.CHANNEL);
      _messageQueue.MessageReceived += OnMessageReceived;
    }

    private void OnSettingsChanged(object sender, EventArgs e)
    {
    }

    private void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == WorkflowManagerMessaging.CHANNEL)
      {
        WorkflowManagerMessaging.MessageType messageType = (WorkflowManagerMessaging.MessageType)message.MessageType;
        if (messageType == WorkflowManagerMessaging.MessageType.NavigationComplete)
        {
          var context = ServiceRegistration.Get<IWorkflowManager>().CurrentNavigationContext;

          if (context != null && context.WorkflowState.StateId == HOME_STATE_ID)
          {
            if (context.WorkflowState.StateId == HOME_STATE_ID)
            {
              // If we are returning to the home state then we need to manually
              // attach and refresh the items, as we detached and missed any changes when leaving.
              AttachAndRefreshHomeMenuItems();
            }
            else
            {
              // The items can change outside of the home state if a different WF state adds some temporary actions to the WF manager.
              // Usually these temporary actions will be removed again before returning to the home state. To ensure that we don't
              // unnecessarily update our items, which can prevent restoring focus correctly, don't listen to changes outside of the home state.
              DetachHomeMenuItems();
            }
          }
        }
      }
    }

    #endregion

    #region Members to be accessed from the GUI

    public ItemsList MainMenuItems
    {
      get
      {
        CheckHomeMenuItems();
        return _mainMenuItems;
      }
    }

    public ItemsList OtherMenuItems
    {
      get
      {
        CheckHomeMenuItems();
        return _otherMenuItems;
      }
    }

    public AbstractProperty Content1Property
    {
      get { return _content1Property; }
    }

    public object Content1
    {
      get { return _content1Property.GetValue(); }
      set { _content1Property.SetValue(value); }
    }

    public AbstractProperty Content2Property
    {
      get { return _content2Property; }
    }

    public object Content2
    {
      get { return _content2Property.GetValue(); }
      set { _content2Property.SetValue(value); }
    }

    public AbstractProperty ContentIndexProperty
    {
      get { return _contentIndexProperty; }
    }

    public int ContentIndex
    {
      get { return (int)_contentIndexProperty.GetValue(); }
      protected set { _contentIndexProperty.SetValue(value); }
    }

    public AbstractProperty SelectedItemProperty
    {
      get { return _selectedItemProperty; }
    }

    public ListItem SelectedItem
    {
      get { return (ListItem)_selectedItemProperty.GetValue(); }
      set { _selectedItemProperty.SetValue(value); }
    }

    public void SetSelectedItem(object sender, SelectionChangedEventArgs e)
    {
      ListItem item = e.FirstAddedItem as ListItem;
      if (item != null)
        SelectedItem = item;
    }

    public void SetSelectedHomeTile(object item)
    {
      UpdateSelectedFanArtItem(item as ListItem);
    }

    public void ClearSelectedHomeTile()
    {
      UpdateSelectedFanArtItem(null);
    }

    public void CloseTopmostDialog(MouseButtons buttons, float x, float y)
    {
      ServiceRegistration.Get<IScreenManager>().CloseTopmostDialog();
    }

    #endregion

    protected bool CheckHomeMenuItems()
    {
      lock (_syncObj)
        if (_allHomeMenuItems != null)
          return false;

      NavigationContext currentContext = ServiceRegistration.Get<IWorkflowManager>().CurrentNavigationContext;
      if (currentContext == null || currentContext.WorkflowState.StateId != HOME_STATE_ID)
        return false;

      ItemsList items = GetMenuModel().MenuItems;
      lock (_syncObj)
      {
        if (_allHomeMenuItems != null)
          return false;
        _allHomeMenuItems = items;
      }
      AttachAndRefreshHomeMenuItems();
      return true;
    }

    protected ItemsList GetHomeMenuItems()
    {
      NavigationContext currentContext = ServiceRegistration.Get<IWorkflowManager>().CurrentNavigationContext;
      if (currentContext == null || currentContext.WorkflowState.StateId != HOME_STATE_ID)
        return null;

      return GetMenuModel().MenuItems;
    }

    private void AttachAndRefreshHomeMenuItems()
    {
      lock (_syncObj)
      {
        if (_isAttachedToMenuItems || _allHomeMenuItems == null)
          return;
        _allHomeMenuItems.ObjectChanged += OnHomeMenuItemsChanged;
        _isAttachedToMenuItems = true;
      }
      OnHomeMenuItemsChanged(_allHomeMenuItems);
    }

    private void DetachHomeMenuItems()
    {
      lock (_syncObj)
      {
        if (!_isAttachedToMenuItems)
          return;
        if(_allHomeMenuItems != null)
          _allHomeMenuItems.ObjectChanged -= OnHomeMenuItemsChanged;
        _isAttachedToMenuItems = false;
      }
    }

    private void OnHomeMenuItemsChanged(IObservable observable)
    {
      var items = observable as ItemsList;
      if (items == null)
        return;

      // Get the currently selected item so we can try
      // and focus it again if the list is rebuilt
      ListItem previousSelectedItem = SelectedItem;

      // Get the action ids that will be visible in the main menu.
      // All other actions will be placed under 'Other'.
      var actionIds = new HashSet<Guid>(_settingsWatcher.Settings.HomeMenuActionIds);
            
      // Sort the changed items into the main menu items and the 'other' items.
      List<ListItem> changedMainItems = new List<ListItem>();
      List<ListItem> changedOtherItems = new List<ListItem>();
      lock (items.SyncRoot)
      {
        foreach (var item in items)
        {
          if (TryGetAction(item, out var action) && actionIds.Contains(action.ActionId))
            changedMainItems.Add(item);
          else // no action or it's not in our list of actions to show in the main menu
            changedOtherItems.Add(item);
        }
      }

      // We need to create the other plugins menu item if necessary and
      // add it to the changed main items manually to ensure that the
      // current and changed items are compared correctly.
      if (_otherPluginsMenuItem == null)
      {
        WorkflowAction action = new OtherPluginsAction();
        ListItem item = new ListItem("Name", action.DisplayTitle)
        {
          Command = new MethodDelegateCommand(action.Execute)
        };
        item.AdditionalProperties[Consts.KEY_ITEM_ACTION] = action;
        item.SetLabel("Help", action.HelpText);
        _otherPluginsMenuItem = item;
      }
      changedMainItems.Add(_otherPluginsMenuItem);

      // Rebuild the items lists only if the actions have actually changed
      if (RebuildMenuItemsIfNotEqual(_mainMenuItems, changedMainItems))
      {
        // The list has been rebuilt, try and set focus on the previously selected action
        WorkflowAction previousSelectedAction = previousSelectedItem != null ? GetAction(previousSelectedItem) : null;
        TryRestoreSelectedAction(previousSelectedAction, changedMainItems);
        _mainMenuItems.FireChange();
      }

      if (RebuildMenuItemsIfNotEqual(_otherMenuItems, changedOtherItems))
        _otherMenuItems.FireChange();
    }

    protected bool RebuildMenuItemsIfNotEqual(ItemsList current, IList<ListItem> updated)
    {
      lock (current.SyncRoot)
      {
        // Actions are equal so no need to rebuild
        if (ActionIdsAreEqual(current, updated))
          return false;
        
        current.Clear();
        CollectionUtils.AddAll(current, updated);
        return true;
      }
    }

    protected bool ActionIdsAreEqual(IList<ListItem> current, IList<ListItem> updated)
    {
      if (current.Count != updated.Count)
        return false;

      // Check that we have the same actions in the same order
      for (int i = 0; i < current.Count; i++)
        if (GetAction(current[i])?.ActionId != GetAction(updated[i])?.ActionId)
          return false;
      return true;
    }

    protected void TryRestoreSelectedAction(WorkflowAction previousSelectedAction, IList<ListItem> items)
    {
      // The list has been rebuilt, try and set focus on the previously selected action
      bool hasSelected = false;
      foreach (ListItem item in items)
      {
        // Shortcut if we've already selected an item
        if (hasSelected)
          item.Selected = false;
        else
        {
          // Select either the first item if there's no previous action, or the action with the same id as the previous action. 
          hasSelected = previousSelectedAction == null || previousSelectedAction.ActionId == GetAction(item)?.ActionId;
          // Always update the property so previously selected items are reset.
          item.Selected = hasSelected;
        }
      }
    }

    private void OnSelectedItemChanged(AbstractProperty property, object oldValue)
    {
      ListItem item = SelectedItem;
      if (item == null)
        return;
      WorkflowAction action = GetAction(item);
      EnqueueUpdate(action);
    }

    private void EnqueueUpdate(WorkflowAction action)
    {
      _updateEvent.EnqueueEvent(this, new ActionEventArgs { Action = action });
    }

    private void OnUpdate(object sender, EventArgs e)
    {
      UpdateContent(((ActionEventArgs)e).Action);
    }

    protected void UpdateContent(WorkflowAction action)
    {
      object nextContent;
      if (action == null || !_homeContent.TryGetValue(action.ActionId, out nextContent))
        nextContent = DEFAULT_HOME_CONTENT;

      int currentIndex = ContentIndex;
      if (ReferenceEquals(_contentProperties[currentIndex].GetValue(), nextContent))
        return;

      int nextIndex = (currentIndex + 1) % _contentProperties.Length;
      _contentProperties[nextIndex].SetValue(nextContent);
      ContentIndex = nextIndex;
    }

    protected void UpdateSelectedFanArtItem(ListItem item)
    {
      //if (item == null)
        //return;
      var fm = GetFanArtBackgroundModel();
      fm.SelectedItem = item;
    }

    protected static FanArtBackgroundModel GetFanArtBackgroundModel()
    {
      return (FanArtBackgroundModel)ServiceRegistration.Get<IWorkflowManager>().GetModel(FanArtBackgroundModel.FANART_MODEL_ID);
    }

    protected static MediaListModel GetMediaListModel()
    {
      return (MediaListModel)ServiceRegistration.Get<IWorkflowManager>().GetModel(MediaListModel.MEDIA_LIST_MODEL_ID);
    }

    protected static BaseContentListModel GetAppListModel()
    {
      return (BaseContentListModel)ServiceRegistration.Get<IWorkflowManager>().GetModel(APPS_LIST_MODEL_ID);
    }

    protected static MenuModel GetMenuModel()
    {
      return (MenuModel)ServiceRegistration.Get<IWorkflowManager>().GetModel(MenuModel.MENU_MODEL_ID);
    }

    protected static WorkflowAction GetAction(ListItem item)
    {
      return item.AdditionalProperties[Consts.KEY_ITEM_ACTION] as WorkflowAction;
    }

    protected static bool TryGetAction(ListItem item, out WorkflowAction action)
    {
      action = item.AdditionalProperties[Consts.KEY_ITEM_ACTION] as WorkflowAction;
      return action != null;
    }
  }
}
