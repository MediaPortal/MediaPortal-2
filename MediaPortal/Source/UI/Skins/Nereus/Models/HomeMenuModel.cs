#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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
using MediaPortal.Common.Messaging;
using MediaPortal.Common.Services.Settings;
using MediaPortal.Common.Settings;
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
    public static readonly Guid OV_LIST_MODEL_ID = new Guid("AFD048F1-9EBB-4EBC-84C8-B27B561B77D0");
    public static readonly Guid WEBRADIO_LIST_MODEL_ID = new Guid("55623F9E-60EF-4C28-B835-F8E44D9549E7");

    protected const int UPDATE_DELAY_MS = 500;

    protected readonly object _syncObj = new object();

    protected AbstractProperty _content1Property;
    protected AbstractProperty _content2Property;
    protected AbstractProperty[] _contentProperties;

    protected AbstractProperty _contentIndexProperty;
    protected AbstractProperty _selectedItemProperty;

    protected AbstractProperty _isMenuSelectedProperty;

    protected AbstractProperty _menuEditModelProperty;

    protected DelayedEvent _updateEvent;

    protected IDictionary<Guid, object> _homeContent = new Dictionary<Guid, object>();
    protected static readonly DefaultHomeContent DEFAULT_HOME_CONTENT = new DefaultHomeContent();

    protected SettingsChangeWatcher<NereusSkinSettings> _settingsWatcher;
    protected IList<Guid> _currentActionIdSettings;
    protected IList<string> _currentActionMediaListSettings;

    protected ItemsList _allHomeMenuItems;
    protected ItemsList _mainMenuItems = new ItemsList();
    protected ItemsList _otherMenuItems = new ItemsList();

    protected ListItem _otherPluginsMenuItem;

    protected bool _isAttachedToMenuItems = false;

    private const int CONTENT_LIST_LIMIT = 6;

    // We try to focus the last selected item on startup, but this item might not yet be available
    // if it's a server menu item. In this case we have to focus a default item instead.
    // We use these variables to tell if the focus has been changed manually from the initial item,
    // and if not we focus the last menu item if/when it becomes available.
    private bool _hasSelectionChanged = false;
    private Guid? _initialSelectedActionId = null;

    public HomeMenuModel()
    {
      _content1Property = new WProperty(typeof(object), null);
      _content2Property = new WProperty(typeof(object), null);
      _contentProperties = new[] { _content1Property, _content2Property };

      _contentIndexProperty = new WProperty(typeof(int), 0);
      _selectedItemProperty = new WProperty(typeof(ListItem), null);
      _isMenuSelectedProperty = new WProperty(typeof(bool), false);

      _menuEditModelProperty = new WProperty(typeof(MenuEditModel), null);

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
      _homeContent.Add(new Guid("E3BBC989-99DB-40E9-A15F-CCB50B17A4C8"), new RadioHomeContent());
      _homeContent.Add(new Guid("bb49a591-7705-408f-8177-45d633fdfad0"), new NewsHomeContent());
      _homeContent.Add(new Guid("e34fdb62-1f3e-4aa9-8a61-d143e0af77b5"), new WeatherHomeContent());
      _homeContent.Add(new Guid("873eb147-c998-4632-8f86-d5e24062be2e"), new LauncherHomeContent());
      _homeContent.Add(new Guid("c33e39cc-910e-41c8-bffd-9eccd340b569"), new OnlineVideosHomeContent());
      _homeContent.Add(new Guid("2ded75c0-5eae-4e69-9913-6b50a9ab2956"), new WebradioHomeContent());
      _homeContent.Add(new Guid("A24958E2-538A-455E-A1DB-A7BB241AF7EC"), new EmulatorsHomeContent());

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
      // Check whether the configured settings have changed,
      var currentIds = _currentActionIdSettings;
      var currentMediaLists = _currentActionMediaListSettings;
      if ((currentIds == null || currentIds.SequenceEqual(_settingsWatcher.Settings.HomeMenuActionIds)) &&
          (currentMediaLists == null || currentMediaLists.SequenceEqual(_settingsWatcher.Settings.HomeMenuActionMediaLists)))
        return;

      // If so, rebuild the items
      OnHomeMenuItemsChanged(_allHomeMenuItems);
    }

    private IDictionary<Guid, IList<string>> GetActionMediaListDictionary(IEnumerable<string> settings)
    {
      if (settings == null)
        return null;

      Dictionary<Guid, IList<string>> actionMediaLists = new Dictionary<Guid, IList<string>>();
      foreach (var list in settings)
      {
        string[] mainParts = list.Split(':');
        if (mainParts?.Length == 2 && Guid.TryParse(mainParts[0].Trim(), out var g))
        {
          actionMediaLists[g] = new List<string>();
          string[] listParts = mainParts[1].Split(',');
          foreach (var mediaListKey in listParts)
            actionMediaLists[g].Add(mediaListKey.Trim());
        }
      }
      return actionMediaLists;
    }

    private IList<string> GetActionMediaListSetting(IDictionary<Guid, IList<string>> actionMediaLists)
    {
      List<string> lists = new List<string>();
      foreach (var actionMediaList in actionMediaLists)
        lists.Add($"{actionMediaList.Key}:{string.Join(",", actionMediaList.Value)}");
      return lists;
    }

    private void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == WorkflowManagerMessaging.CHANNEL)
      {
        WorkflowManagerMessaging.MessageType messageType = (WorkflowManagerMessaging.MessageType)message.MessageType;
        if (messageType == WorkflowManagerMessaging.MessageType.NavigationComplete)
        {
          var context = ServiceRegistration.Get<IWorkflowManager>().CurrentNavigationContext;

          if (context != null)
          {
            if (context.WorkflowState.StateId == HOME_STATE_ID)
            {
              // If we are returning to the home state then we need to manually
              // attach and refresh the items, as we detached and missed any changes when leaving.
              AttachAndRefreshHomeMenuItems();
              UpdateHomeContentLists();
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

    public AbstractProperty IsMenuSelectedProperty
    {
      get { return _isMenuSelectedProperty; }
    }

    public bool IsMenuSelected
    {
      get { return (bool)_isMenuSelectedProperty.GetValue(); }
      set { _isMenuSelectedProperty.SetValue(value); }
    }

    public AbstractProperty MenuEditModelProperty
    {
      get { return _menuEditModelProperty; }
    }

    public MenuEditModel MenuEditModel
    {
      get { return (MenuEditModel)_menuEditModelProperty.GetValue(); }
      set { _menuEditModelProperty.SetValue(value); }
    }

    public void SetSelectedItem(object sender, SelectionChangedEventArgs e)
    {
      ListItem item = e.FirstAddedItem as ListItem;
      IsMenuSelected = item != null;
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

    public void BeginMenuEdit()
    {
      IDictionary<Guid, IList<string>> actionMediaLists = GetActionMediaListDictionary(_settingsWatcher.Settings.HomeMenuActionMediaLists);
      WorkflowAction action = null;
      ListItem item = SelectedItem;
      if (item != null)
        action = GetAction(item);

      // The dialog binds to the edit model, which handles editing the list 
      MenuEditModel = new MenuEditModel(HOME_STATE_ID, action?.ActionId, _settingsWatcher.Settings.HomeMenuActionIds, actionMediaLists, _homeContent);

      // Show the dialog and set a callback to clear the edit model when it closes
      var sm = ServiceRegistration.Get<IScreenManager>();
      sm.ShowDialog("DialogEditMenu", (n, i) => EndMenuEdit());
    }

    public void SaveMenuEdit()
    {
      // Check we've got a valid edit model
      MenuEditModel editModel = MenuEditModel;
      if (editModel == null)
        return;

      // Get the settings and update them, we'll update
      // the menu automatically when we get the settings changed event.
      var sm = ServiceRegistration.Get<ISettingsManager>();
      var settings = sm.Load<NereusSkinSettings>();
      settings.HomeMenuActionIds = editModel.GetCurrentActionIds().ToArray();
      var mediaLists = GetActionMediaListSetting(editModel.GetMediaLists());
      settings.HomeMenuActionMediaLists = mediaLists.ToArray();

      sm.Save(settings);
    }

    public void EndMenuEdit()
    {
      MenuEditModel = null;
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

    private void UpdateHomeContentLists()
    {
      if (ContentIndex < _contentProperties.Length)
      {
        var content = _contentProperties[ContentIndex].GetValue();
        if (content is AbstractHomeContent ahc)
          ahc.ForceUpdateList();
      }
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
        if (_allHomeMenuItems != null)
          _allHomeMenuItems.ObjectChanged -= OnHomeMenuItemsChanged;
        _isAttachedToMenuItems = false;
      }
    }

    private void OnHomeMenuItemsChanged(IObservable observable)
    {
      var items = observable as ItemsList;
      if (items == null)
        return;

      //Update backing list if changed
      IDictionary<Guid, IList<string>> changedActionMediaLists;
      if (_currentActionMediaListSettings == null)
        changedActionMediaLists = GetActionMediaListDictionary(_settingsWatcher.Settings.HomeMenuActionMediaLists);
      else
        changedActionMediaLists = GetActionMediaListDictionary(_settingsWatcher.Settings.HomeMenuActionMediaLists.Except(_currentActionMediaListSettings));
      foreach (var content in _homeContent)
      {
        if (changedActionMediaLists?.ContainsKey(content.Key) ?? false)
          if (content.Value is AbstractHomeContent ahc)
            ahc.UpdateLists(changedActionMediaLists[content.Key]);
      }
      _currentActionMediaListSettings = new List<string>(_settingsWatcher.Settings.HomeMenuActionMediaLists);

      // Get the action ids that will be visible in the main menu.
      // All other actions will be placed under 'Other'.
      var actionIds = _currentActionIdSettings = new List<Guid>(_settingsWatcher.Settings.HomeMenuActionIds);

      // The list items should be in the same order as the settings, so sort by settings index
      SortedList<int, ListItem> sortedMainItems = new SortedList<int, ListItem>();

      // Sort the changed items into the main menu items and the 'other' items.
      List<ListItem> changedOtherItems = new List<ListItem>();
      lock (items.SyncRoot)
      {
        foreach (var item in items)
        {
          int index;
          // If there's a matching action, insert it into our main items at the same index
          if (TryGetAction(item, out var action) && (index = actionIds.IndexOf(action.ActionId)) >= 0)
            sortedMainItems.Add(index, item); // changedMainItems.Add(item);
          else // no action or it's not in our list of actions to show in the main menu
            changedOtherItems.Add(item);
        }
      }

      // Get the sorted main items into a regular list
      List<ListItem> changedMainItems = new List<ListItem>(sortedMainItems.Values);

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
        ListItem previousSelectedItem = FindListItemByActionId(_settingsWatcher.Settings.LastSelectedHomeMenuActionId);
        WorkflowAction previousSelectedAction = previousSelectedItem != null ? GetAction(previousSelectedItem) : null;

        TryRestoreSelectedAction(previousSelectedAction, changedMainItems);
        _mainMenuItems.FireChange();
      }

      if (RebuildMenuItemsIfNotEqual(_otherMenuItems, changedOtherItems))
        _otherMenuItems.FireChange();
    }

    private ListItem FindListItemByActionId(string lastSelectedHomeMenuActionIdString)
    {
      Guid lastSelectedHomeMenuActionId;
      if (string.IsNullOrEmpty(lastSelectedHomeMenuActionIdString) || !Guid.TryParse(lastSelectedHomeMenuActionIdString, out lastSelectedHomeMenuActionId))
        return null;
      foreach (ListItem mainMenuItem in _mainMenuItems)
      {
        var action = GetAction(mainMenuItem);
        if (action?.ActionId == lastSelectedHomeMenuActionId)
          return mainMenuItem;
      }
      return null;
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
        {
          item.Selected = false;
        }
        else
        {
          Guid? itemActionId = GetAction(item)?.ActionId;
          // Select either the first item if there's no previous action, or the action with the same id as the previous action.
          hasSelected = previousSelectedAction == null || previousSelectedAction.ActionId == itemActionId;
          // Always update the property so previously selected items are reset.
          item.Selected = hasSelected;

          // If previous selected action is null then the actual previous item might not be available yet,
          // e.g. if the server isn't connected yet. Mark this item as the initial selection, so we can try
          // and focus the actual previous item when it becomes available if the selection hasn't been changed from this item.
          if (hasSelected && previousSelectedAction == null)
          {
            _hasSelectionChanged = false;
            _initialSelectedActionId = itemActionId;
          }
        }
      }
    }

    private void OnSelectedItemChanged(AbstractProperty property, object oldValue)
    {
      ListItem item = SelectedItem;
      if (item == null)
        return;      

      // If the selection hasn't been changed yet, see if this changes it.
      if (!_hasSelectionChanged)
        _hasSelectionChanged = GetAction(item)?.ActionId != _initialSelectedActionId;
      // If so, store this item as the last selected item.
      if (_hasSelectionChanged)
        SaveLastSelectedAction(item);

      lock (_mainMenuItems.SyncRoot)
        foreach (var menuItem in _mainMenuItems)
          menuItem.Selected = false;

      item.Selected = true;

      WorkflowAction action = GetAction(item);
      EnqueueUpdate(action);
    }

    private void SaveLastSelectedAction(ListItem item)
    {
      var action = GetAction(item);
      if (action != null)
      {
        // Get the updated action ids and update the settings, we'll update
        // the menu automatically when we get the settings changed event.
        var sm = ServiceRegistration.Get<ISettingsManager>();
        var settings = sm.Load<NereusSkinSettings>();
        settings.LastSelectedHomeMenuActionId = action.ActionId.ToString();
        sm.Save(settings);
      }
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

    protected static IContentListModel GetAppListModel()
    {
      return (IContentListModel)ServiceRegistration.Get<IWorkflowManager>().GetModel(APPS_LIST_MODEL_ID);
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
