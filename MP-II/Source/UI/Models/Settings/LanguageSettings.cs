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

using System.Collections.Generic;
using System.Globalization;
using MediaPortal.Core;
using MediaPortal.Presentation.Localization;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.Presentation.MenuManager;
using MediaPortal.Presentation.Screen;


namespace Models.Settings
{
  public class Language
  {
    ItemsList _mainMenu;
    ItemsList _languages;

    public Language()
    {
      ILocalization localProvider = ServiceScope.Get<ILocalization>();

      ICollection<CultureInfo> langs = localProvider.AvailableLanguages;

      _languages = new ItemsList();
      foreach (CultureInfo lang in langs)
      {
        ListItem item = new ListItem("Name", lang.EnglishName);
        _languages.Add(item);
      }
    }

    /// <summary>
    /// Exposes all languages available to the skinengine.
    /// </summary>
    /// <value>The languages.</value>
    public ItemsList Languages
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
      ILocalization localProvider = ServiceScope.Get<ILocalization>();
      ICollection<CultureInfo> langs = localProvider.AvailableLanguages;
      foreach (CultureInfo lang in langs)
      {
        if (lang.EnglishName == langChoosen)
        {
          localProvider.ChangeLanguage(lang.Name);
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

      ILocalization localProvider = ServiceScope.Get<ILocalization>();

      foreach (ListItem item in _languages)
      {
        item.Selected = (item.Label("Name", "").Evaluate() == localProvider.CurrentCulture.EnglishName);
      }
    }

    /// <summary>
    /// exposes the main settings menu to the skin
    /// </summary>
    /// <value>The main menu.</value>
    public ItemsList MainMenu
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
