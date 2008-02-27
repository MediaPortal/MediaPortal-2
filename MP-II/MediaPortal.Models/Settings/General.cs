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
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using MediaPortal.Core;
using MediaPortal.Core.Collections;
using MediaPortal.Core.Localisation;
using MediaPortal.Core.WindowManager;
using MediaPortal.Core.MenuManager;
using MediaPortal.Core.PluginManager;

namespace Settings
{
  public class General : IPlugin
  {
    ItemsCollection _mainMenu;

    #region IPlugin Members
    public General()
    {
    }

    public void Initialize(string id)
    {
    }

    public void Dispose()
    {
    }
    #endregion

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
          _mainMenu = new ItemsCollection(menuCollect.GetMenu("settings-main"));
        }
        return _mainMenu;
      }
    }

  }
}
