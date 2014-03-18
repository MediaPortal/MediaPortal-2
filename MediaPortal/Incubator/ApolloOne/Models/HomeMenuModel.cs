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
using MediaPortal.Common;
using MediaPortal.Common.Commands;
using MediaPortal.Common.General;
using MediaPortal.Common.Settings;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.ApolloOne.Settings;
using MediaPortal.UiComponents.SkinBase.General;
using MediaPortal.UiComponents.SkinBase.Models;

namespace MediaPortal.UiComponents.ApolloOne.Models
{
  public class HomeMenuModel : MenuModel
  {
    #region Consts

    public const string STR_HOMEMENU_MODEL_ID = "EBA16B93-B669-4162-9CA2-CB1D5E267EC3";
    public static readonly Guid HOMEMENU_MODEL_ID = new Guid(STR_HOMEMENU_MODEL_ID);

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
        Dictionary<Guid, GridPosition> positions;
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

    protected void MenuItemsOnObjectChanged(IObservable observable)
    {
      CreatePositionedItems();
    }

    protected void CreateMenuGroupItems()
    {
      _mainMenuGroupList.Clear();
      if (_menuSettings != null)
      {
        foreach (string group in _menuSettings.MainMenuGroupNames)
        {
          string groupName = group;
          var groupItem = new ListItem(Consts.KEY_NAME, groupName)
          {
            Command = new MethodDelegateCommand(() => SetGroup(groupName))
          };
          _mainMenuGroupList.Add(groupItem);
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
        if (CurrentKey == MenuSettings.OTHERS_MENU_NAME)
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
      CreatePositionedItems();
    }

    /// <summary>
    /// Reads actions/positon from settings.
    /// </summary>
    private void ReadPositions()
    {
      var menuSettings = ServiceRegistration.Get<ISettingsManager>().Load<MenuSettings>();
      if (menuSettings.MenuItems.Count == 0)
      {
        var positions = new Dictionary<Guid, GridPosition>();
        positions[new Guid("A4DF2DF6-8D66-479a-9930-D7106525EB07")] = new GridPosition { Column = 0, ColumnSpan = MenuSettings.DEFAULT_COLSPAN_NORMAL, Row = 0, RowSpan = MenuSettings.DEFAULT_ROWSPAN_NORMAL }; // Videos
        positions[new Guid("80D2E2CC-BAAA-4750-807B-F37714153751")] = new GridPosition { Column = 0, ColumnSpan = MenuSettings.DEFAULT_COLSPAN_NORMAL, Row = MenuSettings.DEFAULT_ROWSPAN_NORMAL, RowSpan = MenuSettings.DEFAULT_ROWSPAN_NORMAL }; // Movies
        positions[new Guid("30F57CBA-459C-4202-A587-09FFF5098251")] = new GridPosition { Column = MenuSettings.DEFAULT_COLSPAN_NORMAL, ColumnSpan = MenuSettings.DEFAULT_COLSPAN_NORMAL, Row = 0, RowSpan = MenuSettings.DEFAULT_ROWSPAN_NORMAL }; // Series
        positions[new Guid("C33E39CC-910E-41C8-BFFD-9ECCD340B569")] = new GridPosition { Column = MenuSettings.DEFAULT_COLSPAN_NORMAL, ColumnSpan = MenuSettings.DEFAULT_COLSPAN_NORMAL, Row = MenuSettings.DEFAULT_ROWSPAN_NORMAL, RowSpan = MenuSettings.DEFAULT_ROWSPAN_NORMAL }; // OnlineVideos

        positions[new Guid("93442DF7-186D-42e5-A0F5-CF1493E68F49")] = new GridPosition { Column = 3 * MenuSettings.DEFAULT_ROWSPAN_NORMAL + 1, ColumnSpan = MenuSettings.DEFAULT_COLSPAN_LARGE, Row = 0, RowSpan = MenuSettings.DEFAULT_ROWSPAN_LARGE }; // Browse Media
        positions[new Guid("17D2390E-5B05-4fbd-89F6-24D60CEB427F")] = new GridPosition { Column = 3 * MenuSettings.DEFAULT_ROWSPAN_NORMAL + 1, ColumnSpan = MenuSettings.DEFAULT_COLSPAN_LARGE, Row = 0, RowSpan = MenuSettings.DEFAULT_ROWSPAN_LARGE }; // Browse Local (exclusive)

        menuSettings.MainMenuGroupNames = new List<string> { "[Menu.Images]", "[Menu.Audio]", "[Menu.MediaHub]", "[Menu.TV]", "[Menu.Weather]", "[Menu.News]", "[Menu.Settings]", "[Menu.More]" };
        menuSettings.MenuItems["[Menu.MediaHub]"] = positions;
        menuSettings.DefaultIndex = 2;
        ServiceRegistration.Get<ISettingsManager>().Save(menuSettings);
      }
      _menuSettings = menuSettings;
      if (!_menuSettings.MainMenuGroupNames.Contains(MenuSettings.OTHERS_MENU_NAME))
      {
        _menuSettings.MainMenuGroupNames.Add(MenuSettings.OTHERS_MENU_NAME);
        ServiceRegistration.Get<ISettingsManager>().Save(menuSettings);
      }
    }
  }
}
