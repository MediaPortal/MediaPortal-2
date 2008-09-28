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

using System.Globalization;
using MediaPortal.Core;
using MediaPortal.Presentation.Localisation;
using MediaPortal.Core.PluginManager;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.Presentation.MenuManager;
using MediaPortal.Presentation.Screen;


namespace Models.Settings
{
  public class Language
  {
    ItemsCollection _mainMenu;
    ItemsCollection _languages;

    public Language()
    {
      ILocalisation localProvider = ServiceScope.Get<ILocalisation>();

      CultureInfo[] langs = localProvider.AvailableLanguages();

      _languages = new ItemsCollection();
      for (int i = 0; i < langs.Length; ++i)
      {
        ListItem item = new ListItem("Name", langs[i].EnglishName);
        _languages.Add(item);
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
      string langChoosen = item.Label("Name", "").Evaluate();
      ILocalisation localProvider = ServiceScope.Get<ILocalisation>();
      CultureInfo[] langs = localProvider.AvailableLanguages();
      for (int i = 0; i < langs.Length; ++i)
      {
        if (langs[i].EnglishName == langChoosen)
        {
          localProvider.ChangeLanguage(langs[i].Name);
          IScreenManager windowMgr = ServiceScope.Get<IScreenManager>();
          windowMgr.Reset();
          return;
        }
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
        item.Selected = (item.Label("Name", "").Evaluate() == localProvider.CurrentCulture.EnglishName);
      }
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
          _mainMenu = MenuHelper.WrapMenu(menuCollect.GetMenu("settings-language"));
        }
        return _mainMenu;
      }
    }
  }
}
