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
using System.Linq;
using System.Windows.Forms;
using MediaPortal.Common;
using MediaPortal.Common.Commands;
using MediaPortal.Common.General;
using MediaPortal.Common.Settings;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.ApolloOne.Settings;
using MediaPortal.UiComponents.SkinBase.General;
using MediaPortal.UiComponents.SkinBase.Models;
using MediaPortal.Utilities.Xml;

namespace MediaPortal.UiComponents.ApolloOne.Models
{
  public class HomeMenuModel : MenuModel
  {
    #region Consts

    public const string STR_HOMEMENU_MODEL_ID = "EBA16B93-B669-4162-9CA2-CB1D5E267EC3";
    public static readonly Guid HOMEMENU_MODEL_ID = new Guid(STR_HOMEMENU_MODEL_ID);
    public const string STR_HOME_STATE_ID = "7F702D9C-F2DD-42da-9ED8-0BA92F07787F";
    public static readonly Guid HOME_STATE_ID = new Guid(STR_HOME_STATE_ID);

    #endregion

    #region Fields

    readonly ItemsList _mainMenuGroupList = new ItemsList();
    readonly ItemsList _positionedItems = new ItemsList();
    protected MenuSettings _menuSettings;

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

    #endregion

    #region Properties

    protected string CurrentKey
    {
      get
      {
        if (_menuSettings == null || _menuSettings.DefaultIndex >= _menuSettings.MainMenuGroupNames.Count)
          return string.Empty;
        return _menuSettings.MainMenuGroupNames[_menuSettings.DefaultIndex];
      }
    }

    protected IDictionary<Guid, GridPosition> Positions
    {
      get
      {
        SerializableDictionary<Guid, GridPosition> positions;
        if (_menuSettings == null || !_menuSettings.MenuItems.TryGetValue(CurrentKey, out positions))
          return new Dictionary<Guid, GridPosition>();

        return positions;
      }
    }

    public ItemsList MainMenuGroupList
    {
      get { return _mainMenuGroupList; }
    }

    public ItemsList PositionedMenuItems
    {
      get { return _positionedItems; }
    }

    #endregion

    public HomeMenuModel()
    {
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

    protected void MenuItemsOnObjectChanged(IObservable observable)
    {
      CreatePositionedItems();
    }

    protected void CreateMenuGroupItems()
    {
      _mainMenuGroupList.Clear();
      if (_menuSettings != null)
      {
        int idx = 0;
        foreach (string group in _menuSettings.MainMenuGroupNames)
        {
          string groupName = group;
          var groupItem = new ListItem(Consts.KEY_NAME, groupName)
          {
            Command = new MethodDelegateCommand(() => SetGroup(groupName)),
            Selected = idx == _menuSettings.DefaultIndex
          };
          _mainMenuGroupList.Add(groupItem);
          idx ++;
        }
      }
      _mainMenuGroupList.FireChange();
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

        // Under "others" all items are places, that do not fit into any other category
        if (CurrentKey == MenuSettings.MENU_NAME_OTHERS)
        {
          bool found = _menuSettings.MenuItems.Keys.Any(key => _menuSettings.MenuItems[key].ContainsKey(wfAction.ActionId));
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

    private void SetGroup(string groupName)
    {
      _menuSettings.DefaultIndex = _menuSettings.MainMenuGroupNames.IndexOf(groupName);
      ServiceRegistration.Get<ISettingsManager>().Save(_menuSettings);
      NavigateToHome();
      CreatePositionedItems();
      UpdateSelectedGroup();
    }

    private void NavigateToHome()
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      if (workflowManager == null)
        return;

      if (workflowManager.CurrentNavigationContext.WorkflowState.StateId != HOME_STATE_ID)
        workflowManager.NavigatePopToState(HOME_STATE_ID, false);
    }

    private void UpdateSelectedGroup()
    {
      int idx = 0;
      foreach (ListItem listItem in MainMenuGroupList)
        listItem.Selected = (idx++) == _menuSettings.DefaultIndex;
    }

    /// <summary>
    /// Reads actions/positon from settings.
    /// </summary>
    private void ReadPositions()
    {
      var menuSettings = ServiceRegistration.Get<ISettingsManager>().Load<MenuSettings>();
      if (menuSettings.MenuItems.Count == 0)
      {
        menuSettings.MainMenuGroupNames = new List<string> { MenuSettings.MENU_NAME_IMAGE, MenuSettings.MENU_NAME_AUDIO, MenuSettings.MENU_NAME_MEDIAHUB, MenuSettings.MENU_NAME_TV, MenuSettings.MENU_NAME_NEWS, MenuSettings.MENU_NAME_SETTINGS, MenuSettings.MENU_NAME_OTHERS };
        menuSettings.DefaultIndex = 2;

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
      _menuSettings = menuSettings;
      if (!_menuSettings.MainMenuGroupNames.Contains(MenuSettings.MENU_NAME_OTHERS))
      {
        _menuSettings.MainMenuGroupNames.Add(MenuSettings.MENU_NAME_OTHERS);
        ServiceRegistration.Get<ISettingsManager>().Save(menuSettings);
      }
    }
  }
}
