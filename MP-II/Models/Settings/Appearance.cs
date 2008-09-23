#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using MediaPortal.Core;
using MediaPortal.Core.PathManager;
using MediaPortal.Core.PluginManager;
using MediaPortal.Core.Services.PluginManager;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.Presentation.MenuManager;
using MediaPortal.Presentation.Screen;


namespace Models.Settings
{

  public class Appearance
  {
    ItemsCollection _mainMenu;

    ItemsCollection _skins;
    ItemsCollection _themes;

    public Appearance()
    {
      _skins = new ItemsCollection();
      // FIXME Albert78: Use the SkinResources lookup mechanism here, after this class was moved
      // to SkinEngine project.
      // THIS IS A HACK!
      IPluginManager pluginManager = ServiceScope.Get<IPluginManager>();
      IPluginItemStateTracker stateTracker = new FixedItemStateTracker();
      foreach (PluginResource rootDirectoryResource in pluginManager.RequestAllPluginItems<PluginResource>(
          "/Resources/Skin", stateTracker))
        foreach (DirectoryInfo skinDirectory in rootDirectoryResource.Location.GetDirectories())
        {
          string skinName = skinDirectory.Name;
          ListItem item = new ListItem("Name", skinName);
          string previewImagePath = String.Format("{0}\\themes\\default\\media\\preview.png", skinDirectory.FullName);
          item.Add("CoverArt", previewImagePath);
          item.Add("defaulticon", previewImagePath);
          _skins.Add(item);
        }
      pluginManager.RevokeAllPluginItems("/Resources/Skin", stateTracker);

      _themes = new ItemsCollection();
      UpdateThemes();
    }

    void UpdateThemes()
    {
      // HACK!!!! Has to be replaced by the SkinResources lookup mechanism
      _themes.Clear();
      IScreenManager mgr = ServiceScope.Get<IScreenManager>();
      IResourceAccessor ra = mgr.SkinResourceContext;
      foreach (FileInfo file in ra.GetResourceFiles("^themes\\\\[\\w]*\\\\theme.xml$").Values)
      {
        Regex re = new Regex(".*\\\\themes\\\\([\\w]*)\\\\theme.xml");
        string themeName = re.Matches(file.FullName)[0].Groups[1].ToString();

        ListItem item = new ListItem("Name", themeName);
        FileInfo previewImage = ra.GetResourceFile("themes\\" + themeName + "\\media\\preview.png");
        if (previewImage != null)
          item.Add("CoverArt", previewImage.FullName);
        _themes.Add(item);
      }
    }

    /// <summary>
    /// Sets the current skin in use.
    /// </summary>
    void SetSelectedSkin()
    {
      IScreenManager mgr = ServiceScope.Get<IScreenManager>();
      foreach (ListItem item in _skins)
      {
        item.Selected = (item.Label("Name", "").Evaluate() == mgr.SkinName);
      }

    }

    /// <summary>
    /// Exposes all skins available to the skinengine.
    /// </summary>
    /// <value>The skins.</value>
    public ItemsCollection Skins
    {
      get
      {
        SetSelectedSkin();
        return _skins;
      }
    }
    /// <summary>
    /// Method for the skin to set the skin.
    /// </summary>
    /// <param name="item">The item.</param>
    public void SetSkin(ListItem item)
    {
      if (item == null) return;
      string skinChoosen = item.Label("Name", "").Evaluate();

      IScreenManager windowMgr = ServiceScope.Get<IScreenManager>();
      windowMgr.SwitchSkin(skinChoosen);
      UpdateThemes();
    }

    
    /// <summary>
    /// exposes the main settings menu to the skin
    /// </summary>
    /// <value>The main menu.</value>
    public ItemsCollection MainMenu
    {
      get
      {
        if (_mainMenu == null)
        {
          IMenuCollection menuCollect = ServiceScope.Get<IMenuCollection>();
          _mainMenu = new ItemsCollection(menuCollect.GetMenu("settings-appearance"));
        }
        return _mainMenu;
      }
    }
    /// <summary>
    /// Exposes all skins available to the skinengine.
    /// </summary>
    /// <value>The skins.</value>
    public ItemsCollection Themes
    {
      get
      {
        SetSelectedTheme();
        return _themes;
      }
    }

    /// <summary>
    /// Sets the current skin in use.
    /// </summarySetSelectedTheme
    void SetSelectedTheme()
    {
      IScreenManager mgr = ServiceScope.Get<IScreenManager>();
      foreach (ListItem item in _themes)
      {
        item.Selected = (item.Label("Name", "").Evaluate() == mgr.ThemeName);
      }

    }

    /// <summary>
    /// Method for the skin to set the skin.
    /// </summary>
    /// <param name="item">The item.</param>
    public void SetTheme(ListItem item)
    {
      if (item == null) return;
      string themeChoosen = item.Label("Name", "").Evaluate();

      IScreenManager windowMgr = ServiceScope.Get<IScreenManager>();
      windowMgr.SwitchTheme(themeChoosen);
    }
  }
}
