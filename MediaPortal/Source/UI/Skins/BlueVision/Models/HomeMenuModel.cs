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
using MediaPortal.Common;
using MediaPortal.Common.Commands;
using MediaPortal.Common.General;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.Services.Settings;
using MediaPortal.Common.Settings;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.BlueVision.Settings;
using MediaPortal.UiComponents.SkinBase.General;
using MediaPortal.UiComponents.SkinBase.Models;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.Utilities.Xml;

namespace MediaPortal.UiComponents.BlueVision.Models
{
  public class HomeMenuModel : MenuModel
  {
    #region Consts

    public const string STR_HOMEMENU_MODEL_ID = "A6C6D5DA-55FE-4b5f-AE83-B03E8BBFA177";
    public static readonly Guid HOMEMENU_MODEL_ID = new Guid(STR_HOMEMENU_MODEL_ID);
    public const string STR_HOME_STATE_ID = "7F702D9C-F2DD-42da-9ED8-0BA92F07787F";
    public static readonly Guid HOME_STATE_ID = new Guid(STR_HOME_STATE_ID);

    #endregion

    #region Fields

    readonly ItemsList _mainMenuGroupList = new ItemsList();
    readonly ItemsList _positionedItems = new ItemsList();
    protected SettingsChangeWatcher<MenuSettings> _menuSettings;
    protected AbstractProperty _lastSelectedItemProperty;
    protected AbstractProperty _lastSelectedItemNameProperty;
    protected AbstractProperty _isHomeProperty;
    protected AbstractProperty _isPlayerActiveProperty;
    protected bool _noSettingsRefresh;
    protected bool _isPlayerActive;
    protected string _lastActiveGroup;

    #endregion

    #region Internal class

    public class GridPosition
    {
      public int Row { get; set; }
      public int RowSpan { get; set; }
      public int Column { get; set; }
      public int ColumnSpan { get; set; }

      public GridPosition()
      {
        RowSpan = 1;
        ColumnSpan = 1;
      }
    }

    /// <summary>
    /// Command that intercepts the menu item command and sets the <see cref="LastSelectedItem"/> property
    /// before the original command is executed.
    /// </summary>
    private class MenuItemCommandInterceptor : ICommand
    {
      public MenuItemCommandInterceptor(HomeMenuModel model, ListItem menuItem)
      {
        Model = model;
        MenuItem = menuItem;
        OriginalCommand = menuItem.Command;
      }

      public HomeMenuModel Model { get; private set; }

      public ListItem MenuItem { get; private set; }

      public ICommand OriginalCommand { get; private set; }

      public void Execute()
      {
        Model.LastSelectedItem = MenuItem;
        Model.LastSelectedItemName = MenuItem["Name"];
        OriginalCommand.Execute();
      }
    }

    #endregion

    #region Properties

    protected string CurrentKey
    {
      get
      {
        if (_menuSettings == null)
          return string.Empty;
        var item = MainMenuGroupList.OfType<GroupMenuListItem>().FirstOrDefault(i => i.IsActive);
        if (item == null)
          return string.Empty;

        var gi = item.AdditionalProperties["Item"] as GroupItemSetting;
        return gi != null ? gi.Name : string.Empty;
      }
    }

    protected IDictionary<Guid, GridPosition> Positions
    {
      get
      {
        SerializableDictionary<Guid, GridPosition> positions;
        if (_menuSettings == null || !_menuSettings.Settings.MenuItems.TryGetValue(CurrentKey, out positions))
          return new Dictionary<Guid, GridPosition>();

        return positions;
      }
    }

    public ItemsList MainMenuGroupList
    {
      get
      {
        lock (_mainMenuGroupList.SyncRoot)
          return _mainMenuGroupList;
      }
    }

    public ItemsList PositionedMenuItems
    {
      get { return _positionedItems; }
    }

    public AbstractProperty LastSelectedItemProperty
    {
      get { return _lastSelectedItemProperty; }
    }

    public ListItem LastSelectedItem
    {
      get { return (ListItem)_lastSelectedItemProperty.GetValue(); }
      set { _lastSelectedItemProperty.SetValue(value); }
    }

    public AbstractProperty LastSelectedItemNameProperty
    {
      get { return _lastSelectedItemNameProperty; }
    }

    public string LastSelectedItemName
    {
      get { return (string)_lastSelectedItemNameProperty.GetValue(); }
      set { _lastSelectedItemNameProperty.SetValue(value); }
    }

    public AbstractProperty IsHomeProperty
    {
      get { return _isHomeProperty; }
    }

    public bool IsHome
    {
      get { return (bool)_isHomeProperty.GetValue(); }
      set { _isHomeProperty.SetValue(value); }
    }

    public AbstractProperty IsPlayerActiveProperty
    {
      get { return _isPlayerActiveProperty; }
    }

    public bool IsPlayerActive
    {
      get { return (bool)_isPlayerActiveProperty.GetValue(); }
      set { _isPlayerActiveProperty.SetValue(value); }
    }

    #endregion

    public HomeMenuModel()
    {
      _lastSelectedItemProperty = new WProperty(typeof(ListItem), null);
      _lastSelectedItemNameProperty = new WProperty(typeof(string), null);
      _isHomeProperty = new WProperty(typeof(bool), false);
      _isPlayerActiveProperty = new WProperty(typeof(bool), false);
      IsHomeProperty.Attach(IsHomeChanged);

      SubscribeToMessages();

      ReadPositions();

      CreateMenuGroupItems();
      CreatePositionedItems();
      MenuItems.ObjectChanged += MenuItemsOnObjectChanged;
    }

    public void CloseTopmostDialog(MouseButtons buttons, float x, float y)
    {
      ServiceRegistration.Get<IScreenManager>().CloseTopmostDialog();
    }

    public void CloseMenu(MouseButtons buttons, float x, float y)
    {
      ToggleMenu();
    }

    public void OnScreenShow()
    {
      // if the home screen is shown, we need to restore the selected group item as LastSelectedItem.
      var screenManager = ServiceRegistration.Get<IScreenManager>();
      // unfortunately we can not access ScreenManager.HOME_SCREEN without adding another reference to the plugin
      if (String.Equals(screenManager.ActiveScreenName, "home"))
      {
        lock (_mainMenuGroupList.SyncRoot)
          foreach (GroupMenuListItem listItem in _mainMenuGroupList)
          {
            if (listItem.IsActive)
            {
              LastSelectedItem = listItem;
              LastSelectedItemName = listItem[Consts.KEY_NAME];
              break;
            }
          }
      }
    }

    private void OnSettingsChanged(object sender, EventArgs e)
    {
      // Invoked from internal update, so skip refreshs
      if (_noSettingsRefresh)
        return;
      CreateMenuGroupItems();
      CreatePositionedItems();
    }

    protected void MenuItemsOnObjectChanged(IObservable observable)
    {
      CreatePositionedItems();
    }

    protected void CreateMenuGroupItems()
    {
      lock (_mainMenuGroupList.SyncRoot)
      {
        _mainMenuGroupList.Clear();
        if (_menuSettings != null)
        {
          CreateRegularGroupItems();
          CreateShortcutItems();
          SetFallbackSelection();
        }
      }
      _mainMenuGroupList.FireChange();
    }

    private void CreateRegularGroupItems()
    {
      var defaultMenuGroupId = _menuSettings.Settings.DefaultMenuGroupId;
      foreach (var group in _menuSettings.Settings.MainMenuGroupNames)
      {
        string groupId = group.Id.ToString();
        bool isHome = groupId.Equals(MenuSettings.MENU_ID_HOME, StringComparison.CurrentCultureIgnoreCase);
        if (isHome && _menuSettings.Settings.DisableHomeTab)
          continue;

        string groupName = group.Name;
        var groupItem = new GroupMenuListItem(Consts.KEY_NAME, groupName);
        if (_menuSettings.Settings.DisableAutoSelection)
          groupItem.Command = new MethodDelegateCommand(() => SetGroup(groupId));

        groupItem.AdditionalProperties["Id"] = groupId;
        groupItem.AdditionalProperties["Item"] = group;
        if (groupId == defaultMenuGroupId)
        {
          IsHome = isHome;
          groupItem.IsActive = true;
          groupItem.Selected = true;
        }
        _mainMenuGroupList.Add(groupItem);
      }
    }

    // Look for "shortcut items" that will be placed next to the regular groups
    private void ReCreateShortcutItems()
    {
      // Do not remove the "CP" button, because when the WF state is active, the menu item will not be part of available menu items.
      if (!IsCurrentPlaying())
      {
        foreach (var shortutItem in _mainMenuGroupList.Where(groupItem => ((GroupMenuListItem)groupItem).AdditionalProperties.ContainsKey("ActionId")).ToList())
        {
          _mainMenuGroupList.Remove(shortutItem);
        }
      }

      CreateShortcutItems();
      _mainMenuGroupList.FireChange();
    }

    private void CreateShortcutItems()
    {
      foreach (var menuItem in MenuItems)
      {
        object action;
        if (!menuItem.AdditionalProperties.TryGetValue(Consts.KEY_ITEM_ACTION, out action))
          continue;
        WorkflowAction wfAction = action as WorkflowAction;
        if (wfAction == null)
          continue;

        var shortCut = _menuSettings.Settings.MainMenuShortCuts.FirstOrDefault(sc => sc.ActionId == wfAction.ActionId);
        if (shortCut == null)
          continue;

        string groupId = shortCut.Id.ToString();
        string groupName = shortCut.Name;
        var groupItem = new GroupMenuListItem(Consts.KEY_NAME, groupName);
        if (_menuSettings.Settings.DisableAutoSelection)
          groupItem.Command = new MethodDelegateCommand(() =>
          {
            wfAction.Execute();
            SetGroup(groupId, true);
          });

        groupItem.AdditionalProperties["Id"] = groupId;
        groupItem.AdditionalProperties["ActionId"] = wfAction.ActionId;
        _mainMenuGroupList.Add(groupItem);
      }
    }

    /// <summary>
    /// Makes sure at least one tab is active.
    /// </summary>
    private void SetFallbackSelection()
    {
      if (_mainMenuGroupList.OfType<GroupMenuListItem>().Any(item => item.IsActive))
        return;

      var defaultItem = _mainMenuGroupList.OfType<GroupMenuListItem>().FirstOrDefault(item => string.Equals(item.AdditionalProperties["Id"] as string, MenuSettings.MENU_ID_MEDIAHUB, StringComparison.InvariantCultureIgnoreCase));
      if (defaultItem == null)
        return;
      SetGroup((string)defaultItem.AdditionalProperties["Id"]);
    }

    public void OnGroupItemSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (_menuSettings.Settings.DisableAutoSelection)
        return;
      var item = e.FirstAddedItem as GroupMenuListItem;
      if (item != null)
        SetGroup((string)item.AdditionalProperties["Id"]);
    }

    protected void CreatePositionedItemsIfEmpty()
    {
      if (_positionedItems.Count == 0)
        CreatePositionedItems();
    }

    protected void CreatePositionedItems()
    {
      _positionedItems.Clear();
      int x = 0;
      foreach (var menuItem in MenuItems)
      {
        object action;
        if (!menuItem.AdditionalProperties.TryGetValue(Consts.KEY_ITEM_ACTION, out action))
          continue;
        WorkflowAction wfAction = action as WorkflowAction;
        if (wfAction == null)
          continue;

        // intercept the menu item commands, so we can set the item as LastSelectedItem
        // since the menu items are recreated when an sub screen is opened we have to check if the item is already intercepted each time
        if (!(menuItem.Command is MenuItemCommandInterceptor))
        {
          menuItem.Command = new MenuItemCommandInterceptor(this, menuItem);
        }

        // Under "others" all items are places, that do not fit into any other category
        if (CurrentKey == MenuSettings.MENU_NAME_OTHERS)
        {
          bool found = IsManuallyPositioned(wfAction);
          if (!found)
          {
            GridListItem gridItem = new GridListItem(menuItem)
            {
              GridColumn = x % MenuSettings.DEFAULT_NUM_COLS,
              GridRow = (x / MenuSettings.DEFAULT_NUM_COLS) * MenuSettings.DEFAULT_ROWSPAN_SMALL,
              GridRowSpan = MenuSettings.DEFAULT_ROWSPAN_SMALL,
              GridColumnSpan = MenuSettings.DEFAULT_COLSPAN_SMALL,
            };
            _positionedItems.Add(gridItem);
            x += MenuSettings.DEFAULT_COLSPAN_SMALL;
          }
        }
        else
        {
          GridPosition gridPosition;
          if (Positions.TryGetValue(wfAction.ActionId, out gridPosition))
          {
            GridListItem gridItem = new GridListItem(menuItem)
            {
              GridRow = gridPosition.Row,
              GridColumn = gridPosition.Column,
              GridRowSpan = gridPosition.RowSpan,
              GridColumnSpan = gridPosition.ColumnSpan,
            };
            _positionedItems.Add(gridItem);
          }
        }
      }
      _positionedItems.FireChange();
    }

    private bool IsManuallyPositioned(WorkflowAction wfAction)
    {
      return _menuSettings.Settings.MenuItems.Keys.Any(key => _menuSettings.Settings.MenuItems[key].ContainsKey(wfAction.ActionId));
    }

    private void SetGroup(string groupId, bool isShortCut = false)
    {
      if (groupId.Equals(_menuSettings.Settings.DefaultMenuGroupId, StringComparison.CurrentCultureIgnoreCase))
        return;
      IsHome = groupId.Equals(MenuSettings.MENU_ID_HOME, StringComparison.CurrentCultureIgnoreCase);

      if (!IsCurrentPlaying())
        _lastActiveGroup = _menuSettings.Settings.DefaultMenuGroupId;

      _menuSettings.Settings.DefaultMenuGroupId = groupId;

      try
      {
        _noSettingsRefresh = true;
        ServiceRegistration.Get<ISettingsManager>().Save(_menuSettings.Settings);
        if (isShortCut)
        {
          UpdateSelectedGroup();
        }
        else
          if (NavigateToHome())
          {
            UpdateSelectedGroup();
            CreatePositionedItems();
          }
      }
      finally
      {
        _noSettingsRefresh = false;
      }
    }

    private bool IsCurrentPlaying()
    {
      Guid? fullscreenContentWfStateId;
      Guid? currentlyPlayingWorkflowStateId;
      if (!GetPlayerWorkflowStates(out fullscreenContentWfStateId, out currentlyPlayingWorkflowStateId))
        return false;

      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      return workflowManager.CurrentNavigationContext.WorkflowState.StateId == currentlyPlayingWorkflowStateId;
    }

    private void IsHomeChanged(AbstractProperty property, object oldvalue)
    {
      if (!IsHome)
        return;
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      LatestMediaModel lmm = workflowManager.GetModel(LatestMediaModel.LATEST_MEDIA_MODEL_ID) as LatestMediaModel;
      if (lmm != null)
      {
        lmm.UpdateItems();
      }
    }

    //private bool NavigateBack()
    //{
    //  IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
    //  if (workflowManager == null)
    //    return false;

    //  workflowManager.NavigatePop(1);
    //  return true;
    //}

    private bool NavigateToHome()
    {
      try
      {
        IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
        if (workflowManager == null)
          return false;

        if (workflowManager.CurrentNavigationContext.WorkflowState.StateId != HOME_STATE_ID)
          workflowManager.NavigatePopToState(HOME_STATE_ID, false);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Warn("HomeMenuModel: Failed to navigate.", ex);
        return false;
      }
      return true;
    }

    private void UpdateSelectedGroup()
    {
      List<string> groups = new List<string>();
      if (IsCurrentPlaying() && MenuSettings.MENU_ID_PLAYING.Equals(_menuSettings.Settings.DefaultMenuGroupId, StringComparison.OrdinalIgnoreCase) ||
        !MenuSettings.MENU_ID_PLAYING.Equals(_menuSettings.Settings.DefaultMenuGroupId, StringComparison.OrdinalIgnoreCase))
        groups.Add(_menuSettings.Settings.DefaultMenuGroupId);
      if (!string.IsNullOrEmpty(_lastActiveGroup))
        groups.Add(_lastActiveGroup);
      foreach (var group in groups)
      {
        if (UpdateSelectedGroup(group))
          break;
      }
    }
    private bool UpdateSelectedGroup(string groupId)
    {
      bool anyActive = false;
      lock (_mainMenuGroupList.SyncRoot)
      {
        foreach (GroupMenuListItem listItem in _mainMenuGroupList)
        {
          listItem.Selected = listItem.IsActive = string.Equals(listItem.AdditionalProperties["Id"] as string, groupId, StringComparison.InvariantCultureIgnoreCase);
          // if the group is selected, it is the LastSelectedItem now.
          if (listItem.IsActive)
          {
            LastSelectedItem = listItem;
            LastSelectedItemName = listItem[Consts.KEY_NAME];
            anyActive = true;
          }
        }
      }
      return anyActive;
    }

    /// <summary>
    /// Reads actions/positon from settings.
    /// </summary>
    private void ReadPositions()
    {
      if (_menuSettings == null)
      {
        _menuSettings = new SettingsChangeWatcher<MenuSettings>();
        _menuSettings.SettingsChanged += OnSettingsChanged;
      }
      var menuSettings = _menuSettings.Settings;
      if (menuSettings.MainMenuShortCuts.Count == 0)
      {
        menuSettings.MainMenuShortCuts = new List<GroupItemSetting>
        {
          new GroupItemSetting { Name = MenuSettings.MENU_NAME_PLAYING, Id = new Guid(MenuSettings.MENU_ID_PLAYING), ActionId = MenuSettings.WF_ACTION_CP },
          //new GroupItemSetting { Name = MenuSettings.MENU_NAME_HOME, ActionId = MenuSettings.WF_ACTION_FS },
        };
        ServiceRegistration.Get<ISettingsManager>().Save(menuSettings);
      }
      if (menuSettings.MenuItems.Count == 0)
      {
        menuSettings.MainMenuGroupNames = new List<GroupItemSetting>
        {
          new GroupItemSetting { Name = MenuSettings.MENU_NAME_HOME,        Id = new Guid(MenuSettings.MENU_ID_HOME)},
          new GroupItemSetting { Name = MenuSettings.MENU_NAME_IMAGE,       Id = new Guid(MenuSettings.MENU_ID_IMAGE)},
          new GroupItemSetting { Name = MenuSettings.MENU_NAME_AUDIO,       Id = new Guid(MenuSettings.MENU_ID_AUDIO)},
          new GroupItemSetting { Name = MenuSettings.MENU_NAME_MEDIAHUB,    Id = new Guid(MenuSettings.MENU_ID_MEDIAHUB)},
          new GroupItemSetting { Name = MenuSettings.MENU_NAME_TV,          Id = new Guid(MenuSettings.MENU_ID_TV)},
          new GroupItemSetting { Name = MenuSettings.MENU_NAME_NEWS,        Id = new Guid(MenuSettings.MENU_ID_NEWS)},
          new GroupItemSetting { Name = MenuSettings.MENU_NAME_SETTINGS,    Id = new Guid(MenuSettings.MENU_ID_SETTINGS)},
          new GroupItemSetting { Name = MenuSettings.MENU_NAME_OTHERS,      Id = new Guid(MenuSettings.MENU_ID_OTHERS)}
        };
        menuSettings.DefaultMenuGroupId = MenuSettings.MENU_ID_MEDIAHUB;

        var positions = new SerializableDictionary<Guid, GridPosition>();
        positions[new Guid("A4DF2DF6-8D66-479a-9930-D7106525EB07")] = new GridPosition { Column = 0, ColumnSpan = MenuSettings.DEFAULT_COLSPAN_NORMAL, Row = 0, RowSpan = MenuSettings.DEFAULT_ROWSPAN_NORMAL }; // Videos
        positions[new Guid("80D2E2CC-BAAA-4750-807B-F37714153751")] = new GridPosition { Column = 0, ColumnSpan = MenuSettings.DEFAULT_COLSPAN_NORMAL, Row = MenuSettings.DEFAULT_ROWSPAN_NORMAL, RowSpan = MenuSettings.DEFAULT_ROWSPAN_NORMAL }; // Movies
        positions[new Guid("30F57CBA-459C-4202-A587-09FFF5098251")] = new GridPosition { Column = MenuSettings.DEFAULT_COLSPAN_NORMAL, ColumnSpan = MenuSettings.DEFAULT_COLSPAN_NORMAL, Row = 0, RowSpan = MenuSettings.DEFAULT_ROWSPAN_NORMAL }; // Series
        positions[new Guid("C33E39CC-910E-41C8-BFFD-9ECCD340B569")] = new GridPosition { Column = MenuSettings.DEFAULT_COLSPAN_NORMAL, ColumnSpan = MenuSettings.DEFAULT_COLSPAN_NORMAL, Row = MenuSettings.DEFAULT_ROWSPAN_NORMAL, RowSpan = MenuSettings.DEFAULT_ROWSPAN_NORMAL }; // OnlineVideos

        positions[new Guid("93442DF7-186D-42e5-A0F5-CF1493E68F49")] = new GridPosition { Column = 2 * MenuSettings.DEFAULT_COLSPAN_NORMAL, ColumnSpan = MenuSettings.DEFAULT_COLSPAN_LARGE, Row = 0, RowSpan = MenuSettings.DEFAULT_ROWSPAN_LARGE }; // Browse Media
        positions[new Guid("17D2390E-5B05-4fbd-89F6-24D60CEB427F")] = new GridPosition { Column = 2 * MenuSettings.DEFAULT_COLSPAN_NORMAL, ColumnSpan = MenuSettings.DEFAULT_COLSPAN_LARGE, Row = 0, RowSpan = MenuSettings.DEFAULT_ROWSPAN_LARGE }; // Browse Local (exclusive)
        menuSettings.MenuItems[MenuSettings.MENU_NAME_MEDIAHUB] = positions;

        positions = new SerializableDictionary<Guid, GridPosition>();
        positions[new Guid("55556593-9FE9-436c-A3B6-A971E10C9D44")] = new GridPosition { Column = 2 * MenuSettings.DEFAULT_COLSPAN_NORMAL, ColumnSpan = MenuSettings.DEFAULT_COLSPAN_LARGE, Row = 0, RowSpan = MenuSettings.DEFAULT_ROWSPAN_LARGE }; // Images
        menuSettings.MenuItems[MenuSettings.MENU_NAME_IMAGE] = positions;

        positions = new SerializableDictionary<Guid, GridPosition>();
        positions[new Guid("94961A9E-4C81-4bf7-9EE4-DF9712C3DCF2")] = new GridPosition { Column = 2 * MenuSettings.DEFAULT_COLSPAN_NORMAL, ColumnSpan = MenuSettings.DEFAULT_COLSPAN_LARGE, Row = 0, RowSpan = MenuSettings.DEFAULT_ROWSPAN_LARGE }; // Images
        menuSettings.MenuItems[MenuSettings.MENU_NAME_HOME] = positions;

        positions = new SerializableDictionary<Guid, GridPosition>();
        positions[new Guid("30715D73-4205-417f-80AA-E82F0834171F")] = new GridPosition { Column = 0, ColumnSpan = MenuSettings.DEFAULT_COLSPAN_NORMAL, Row = 0, RowSpan = MenuSettings.DEFAULT_ROWSPAN_NORMAL }; // Audio
        positions[new Guid("E00B8442-8230-4D7B-B871-6AC77755A0D5")] = new GridPosition { Column = MenuSettings.DEFAULT_COLSPAN_NORMAL, ColumnSpan = MenuSettings.DEFAULT_COLSPAN_LARGE, Row = 0, RowSpan = MenuSettings.DEFAULT_ROWSPAN_LARGE }; // PartyMusicPlayer
        positions[new Guid("2DED75C0-5EAE-4E69-9913-6B50A9AB2956")] = new GridPosition { Column = MenuSettings.DEFAULT_COLSPAN_NORMAL + MenuSettings.DEFAULT_COLSPAN_LARGE, ColumnSpan = MenuSettings.DEFAULT_COLSPAN_NORMAL, Row = 0, RowSpan = MenuSettings.DEFAULT_ROWSPAN_NORMAL }; // WebRadio
        menuSettings.MenuItems[MenuSettings.MENU_NAME_AUDIO] = positions;

        positions = new SerializableDictionary<Guid, GridPosition>();
        positions[new Guid("B4A9199F-6DD4-4bda-A077-DE9C081F7703")] = new GridPosition { Column = 0, ColumnSpan = MenuSettings.DEFAULT_COLSPAN_LARGE, Row = 0, RowSpan = MenuSettings.DEFAULT_ROWSPAN_LARGE }; // TV Home
        positions[new Guid("A298DFBE-9DA8-4C16-A3EA-A9B354F3910C")] = new GridPosition { Column = MenuSettings.DEFAULT_COLSPAN_LARGE, ColumnSpan = MenuSettings.DEFAULT_COLSPAN_NORMAL, Row = 0, RowSpan = MenuSettings.DEFAULT_ROWSPAN_NORMAL }; // Apollo EPG Link
        positions[new Guid("7F52D0A1-B7F8-46A1-A56B-1110BBFB7D51")] = new GridPosition { Column = MenuSettings.DEFAULT_COLSPAN_LARGE + MenuSettings.DEFAULT_COLSPAN_NORMAL, ColumnSpan = MenuSettings.DEFAULT_COLSPAN_NORMAL, Row = 0, RowSpan = MenuSettings.DEFAULT_ROWSPAN_NORMAL }; // Apollo Recordings Link
        positions[new Guid("87355E05-A15B-452A-85B8-98D4FC80034E")] = new GridPosition { Column = MenuSettings.DEFAULT_COLSPAN_LARGE, ColumnSpan = MenuSettings.DEFAULT_COLSPAN_NORMAL, Row = MenuSettings.DEFAULT_ROWSPAN_NORMAL, RowSpan = MenuSettings.DEFAULT_ROWSPAN_NORMAL }; // Apollo Schedules Link
        menuSettings.MenuItems[MenuSettings.MENU_NAME_TV] = positions;

        positions = new SerializableDictionary<Guid, GridPosition>();
        positions[new Guid("BB49A591-7705-408F-8177-45D633FDFAD0")] = new GridPosition { Column = 0, ColumnSpan = MenuSettings.DEFAULT_COLSPAN_LARGE, Row = 0, RowSpan = MenuSettings.DEFAULT_ROWSPAN_NORMAL }; // News
        positions[new Guid("BD93C5B3-402C-40A2-B323-DA891ED5F50E")] = new GridPosition { Column = 0, ColumnSpan = MenuSettings.DEFAULT_COLSPAN_LARGE, Row = MenuSettings.DEFAULT_ROWSPAN_NORMAL, RowSpan = MenuSettings.DEFAULT_ROWSPAN_NORMAL }; // Kino
        positions[new Guid("E34FDB62-1F3E-4aa9-8A61-D143E0AF77B5")] = new GridPosition { Column = 2 * MenuSettings.DEFAULT_COLSPAN_NORMAL, ColumnSpan = MenuSettings.DEFAULT_COLSPAN_LARGE, Row = 0, RowSpan = MenuSettings.DEFAULT_ROWSPAN_LARGE }; // Weather
        menuSettings.MenuItems[MenuSettings.MENU_NAME_NEWS] = positions;

        positions = new SerializableDictionary<Guid, GridPosition>();
        positions[new Guid("F6255762-C52A-4231-9E67-14C28735216E")] = new GridPosition { Column = 2 * MenuSettings.DEFAULT_COLSPAN_NORMAL, ColumnSpan = MenuSettings.DEFAULT_COLSPAN_LARGE, Row = 0, RowSpan = MenuSettings.DEFAULT_ROWSPAN_LARGE }; // Configuration
        menuSettings.MenuItems[MenuSettings.MENU_NAME_SETTINGS] = positions;

        ServiceRegistration.Get<ISettingsManager>().Save(menuSettings);
      }
      //_menuSettings = menuSettings;
      if (_menuSettings.Settings.MainMenuGroupNames.All(key => key.Name != MenuSettings.MENU_NAME_OTHERS))
      {
        _menuSettings.Settings.MainMenuGroupNames.Add(new GroupItemSetting { Name = MenuSettings.MENU_NAME_OTHERS, Id = new Guid(MenuSettings.MENU_ID_OTHERS) });
        ServiceRegistration.Get<ISettingsManager>().Save(menuSettings);
      }
    }

    private void SubscribeToMessages()
    {
      if (_messageQueue == null)
        return;
      _messageQueue.SubscribeToMessageChannel(MenuModelMessaging.CHANNEL);
      _messageQueue.SubscribeToMessageChannel(WorkflowManagerMessaging.CHANNEL);
      _messageQueue.MessageReceived += OnMessageReceived;
    }

    private void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == MenuModelMessaging.CHANNEL)
      {
        if (((MenuModelMessaging.MessageType)message.MessageType) == MenuModelMessaging.MessageType.UpdateMenu)
          UpdateShortcuts();
      }
      if (message.ChannelName == WorkflowManagerMessaging.CHANNEL)
      {
        if (((WorkflowManagerMessaging.MessageType)message.MessageType) == WorkflowManagerMessaging.MessageType.StatePushed)
        {
          if (!string.Equals(_menuSettings.Settings.DefaultMenuGroupId, MenuSettings.MENU_ID_PLAYING, StringComparison.OrdinalIgnoreCase))
            _lastActiveGroup = _menuSettings.Settings.DefaultMenuGroupId;
          UpdateSelectedGroup();
        }
        if (((WorkflowManagerMessaging.MessageType)message.MessageType) == WorkflowManagerMessaging.MessageType.StatesPopped)
        {
          UpdateSelectedGroup();
        }
        if (((WorkflowManagerMessaging.MessageType)message.MessageType) == WorkflowManagerMessaging.MessageType.NavigationComplete)
        {
          CheckShortCutsWorkflows();
          UpdateSelectedGroup();
        }
      }
    }

    private void CheckShortCutsWorkflows()
    {
      if (IsCurrentPlaying())
      {
        SetGroup(MenuSettings.MENU_ID_PLAYING, true);
      }
    }

    private static bool GetPlayerWorkflowStates(out Guid? fullscreenContentWfStateId, out Guid? currentlyPlayingWorkflowStateId)
    {
      IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();
      IPlayerContext playerContext = playerContextManager.GetPlayerContext(PlayerChoice.CurrentPlayer);

      if (playerContext == null)
      {
        fullscreenContentWfStateId = null;
        currentlyPlayingWorkflowStateId = null;
        return false;
      }

      fullscreenContentWfStateId = playerContext.FullscreenContentWorkflowStateId;
      currentlyPlayingWorkflowStateId = playerContext.CurrentlyPlayingWorkflowStateId;
      return true;
    }

    //protected override void UpdateMenus()
    //{
    //  base.UpdateMenus();
    //  ReCreateShortcutItems();
    //}

    private void UpdateShortcuts()
    {
      ReCreateShortcutItems();
      //CreateMenuGroupItems();
      //CreatePositionedItems();
    }
  }
}
