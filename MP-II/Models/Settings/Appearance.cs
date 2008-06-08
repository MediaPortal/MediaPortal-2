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
using System.IO;
using System.Globalization;
using MediaPortal.Core;
using MediaPortal.Core.Localisation;
using MediaPortal.Core.PluginManager;
using MediaPortal.Core.PathManager;
using MediaPortal.Presentation.Collections;
using MediaPortal.Presentation.WindowManager;
using MediaPortal.Presentation.MenuManager;


namespace Models.Settings
{
  // FIXME Albert78: Move this into the SkinEngine and integrate the settings into the
  // main settings
  // Rework the whole class
  public class Appearance : IPlugin
  {
    ItemsCollection _mainMenu;
    ItemsCollection _languages;
    ItemsCollection _fullScreen;
    ItemsCollection _skins;
    ItemsCollection _themes;

    #region IPlugin Members
    public void Initialize(string id)
    {
    }

    public void Dispose()
    {
    }
    #endregion

    public Appearance()
    {
      ILocalisation localProvider = ServiceScope.Get<ILocalisation>();

      CultureInfo[] langs = localProvider.AvailableLanguages();

      _languages = new ItemsCollection();
      for (int i = 0; i < langs.Length; ++i)
      {
        ListItem item = new ListItem("Name", langs[i].EnglishName);
        _languages.Add(item);
      }

      _skins = new ItemsCollection();
      string[] skins = Directory.GetDirectories("skin");
      for (int i = 0; i < skins.Length; ++i)
      {
        ListItem item = new ListItem("Name", skins[i].Substring(5));
        // FIXME Albert78: Use the SkinContext resolving mechanism here, after this class was moved
        // to SkinEngine project
        item.Add("CoverArt", String.Format("preview.png"));
        item.Add("defaulticon", String.Format("preview.png"));
        _skins.Add(item);
      }

      _fullScreen = new ItemsCollection();
      _fullScreen.Add(new ListItem("Name", new StringId("system", "yes")));
      _fullScreen.Add(new ListItem("Name", new StringId("system", "no")));

      _themes = new ItemsCollection();
      GetThemes();
    }

    void GetThemes()
    {
      IWindowManager mgr = ServiceScope.Get<IWindowManager>();
      string skinPath = ServiceScope.Get<IPathManager>().GetPath("<SKIN>");
      string[] themes = Directory.GetDirectories(String.Format(@"{0}\{1}\themes", skinPath, mgr.SkinName));
      
      _themes.Clear();
      for (int i = 0; i < themes.Length; ++i)
      {
        string themeName = themes[i];
        int pos = themeName.LastIndexOf(@"\");
        if (pos > 0) themeName = themeName.Substring(pos + 1);

        ListItem item = new ListItem("Name", themeName);
        item.Add("CoverArt", String.Format(@"{0}\media\preview.png", Path.GetFullPath(themes[i])));
        _themes.Add(item);
      }
    }

    /// <summary>
    /// Sets the current skin in use.
    /// </summary>
    void SetSelectedSkin()
    {
      IWindowManager mgr = ServiceScope.Get<IWindowManager>();
      foreach (ListItem item in _skins)
      {
        item.Selected = (item.Label("Name").Evaluate(null, null) == mgr.SkinName);
      }

    }

    /// <summary>
    /// Sets the current language used.
    /// </summary>
    void SetSelectedLanguage()
    {

      ILocalisation localProvider = ServiceScope.Get<ILocalisation>();

      foreach (ListItem item in _languages)
      {
        item.Selected = (item.Label("Name").Evaluate(null, null) == localProvider.CurrentCulture.EnglishName);
      }
    }

    /// <summary>
    /// Exposes all languages available to the skinengine.
    /// </summary>
    /// <value>The languages.</value>
    public ItemsCollection Languages
    {
      get
      {
        SetSelectedLanguage();
        return _languages;
      }
    }

    /// <summary>
    /// Method for the skin to set the language.
    /// </summary>
    /// <param name="item">The item.</param>
    public void SetLanguage(ListItem item)
    {
      if (item == null) return;
      string langChoosen = item.Label("Name").Evaluate(null, null);
      ILocalisation localProvider = ServiceScope.Get<ILocalisation>();
      CultureInfo[] langs = localProvider.AvailableLanguages();
      for (int i = 0; i < langs.Length; ++i)
      {
        if (langs[i].EnglishName == langChoosen)
        {
          localProvider.ChangeLanguage(langs[i].Name);
          IWindowManager windowMgr = ServiceScope.Get<IWindowManager>();
          windowMgr.CurrentWindow.Reset();
          return;
        }
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
      string skinChoosen = item.Label("Name").Evaluate(null, null);

      IWindowManager windowMgr = ServiceScope.Get<IWindowManager>();
      windowMgr.SwitchSkin(skinChoosen);
      GetThemes();
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
    /// exposes the fullscreen options to the skin
    /// </summary>
    /// <value>The full screen.</value>
    public ItemsCollection FullScreen
    {
      get
      {
        IApplication app = ServiceScope.Get<IApplication>();
        _fullScreen[0].Selected = app.IsFullScreen;
        _fullScreen[1].Selected = !app.IsFullScreen;
        return _fullScreen;
      }
    }
    /// <summary>
    /// method for the skin to set fullscreen/windowed mode.
    /// </summary>
    /// <param name="item">The item.</param>
    public void SetFullScreen(ListItem item)
    {
      IApplication app = ServiceScope.Get<IApplication>();
      if (item == _fullScreen[0])
      {
        app.SwitchMode(ScreenMode.FullScreenWindowed, FPS.None);
      }
      else
      {
        app.SwitchMode(ScreenMode.NormalWindowed, FPS.None);
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
      IWindowManager mgr = ServiceScope.Get<IWindowManager>();
      foreach (ListItem item in _themes)
      {
        item.Selected = (item.Label("Name").Evaluate(null, null) == mgr.ThemeName);
      }

    }

    /// <summary>
    /// Method for the skin to set the skin.
    /// </summary>
    /// <param name="item">The item.</param>
    public void SetTheme(ListItem item)
    {
      if (item == null) return;
      string themeChoosen = item.Label("Name").Evaluate(null, null);

      IWindowManager windowMgr = ServiceScope.Get<IWindowManager>();
      windowMgr.SwitchTheme(themeChoosen);
    }
  }
}
