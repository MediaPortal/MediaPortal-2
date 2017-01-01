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
using MediaPortal.Common;
using MediaPortal.Common.Commands;
using MediaPortal.Helpers.SkinHelper.General;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.SkinResources;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.SkinEngine.SkinManagement;

namespace MediaPortal.Helpers.SkinHelper.Models
{
  public class LoadSkinThemeModel
  {
    #region Consts

    public const string LST_MODEL_ID_STR = "68E0A3EE-56BD-45E0-BACC-F614C278B4CD";
    public static Guid LST_MODEL_ID = new Guid(LST_MODEL_ID_STR);

    public const string DIALOG_LOAD_SKIN_THEME = "DialogLoadSkinTheme";

    #endregion

    #region Protected fields

    protected string _dialogTitle;
    protected ItemsList _skinsThemesItemsList = null;

    #endregion

    protected void Initialize(string dialogTitle, ItemsList items)
    {
      _dialogTitle = dialogTitle;
      _skinsThemesItemsList = items;
    }

    #region Members to be accessed from the GUI

    public ItemsList SkinsThemesItemsList
    {
      get { return _skinsThemesItemsList; }
    }

    public string DialogTitle
    {
      get { return _dialogTitle; }
    }

    public void Select(ListItem item)
    {
      ICommand command = item.Command;
      if (command != null)
        command.Execute();
    }

    #endregion

    public static void ShowLoadSkinDialog()
    {
      IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
      SkinManager skinManager = ServiceRegistration.Get<ISkinResourceManager>() as SkinManager;
      if (skinManager == null)
        return;
      ItemsList skinItems = new ItemsList();
      foreach (Skin skin in skinManager.Skins.Values)
      {
        if (!skin.IsValid)
          continue;
        string skinName = skin.Name;
        ListItem skinItem = new ListItem(Consts.KEY_NAME, skinName)
          {
              Command = new MethodDelegateCommand(() => screenManager.SwitchSkinAndTheme(skinName, null))
          };
        skinItems.Add(skinItem);
      }
      ShowDialog(Consts.RES_LOAD_SKIN_TITLE, skinItems);
    }

    public static void ShowLoadThemeDialog()
    {
      IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
      SkinManager skinManager = ServiceRegistration.Get<ISkinResourceManager>() as SkinManager;
      if (skinManager == null)
        return;
      string currentSkinName = screenManager.CurrentSkinResourceBundle.SkinName;
      Skin currentSkin;
      if (!skinManager.Skins.TryGetValue(currentSkinName, out currentSkin))
        return;
      ItemsList themeItems = new ItemsList();
      foreach (Theme theme in currentSkin.Themes.Values)
      {
        if (!theme.IsValid)
          continue;
        string themeName = theme.Name;
        ListItem themeItem = new ListItem(Consts.KEY_NAME, themeName)
          {
              Command = new MethodDelegateCommand(() => screenManager.SwitchSkinAndTheme(null, themeName))
          };
        themeItems.Add(themeItem);
      }
      ShowDialog(Consts.RES_LOAD_THEME_TITLE, themeItems);
    }

    protected static void ShowDialog(string title, ItemsList items)
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      LoadSkinThemeModel model = (LoadSkinThemeModel) workflowManager.GetModel(LST_MODEL_ID);
      model.Initialize(title, items);
      IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
      screenManager.ShowDialog(DIALOG_LOAD_SKIN_THEME);
    }
  }
}
