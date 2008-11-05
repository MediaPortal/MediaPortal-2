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

using MediaPortal.Core;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.Presentation.Localisation;
using MediaPortal.Presentation.MenuManager;
using MediaPortal.Core.PluginManager;
using MediaPortal.Presentation.Screen;

namespace Models.Settings
{
  public class ScreenSettings
  {
    ItemsCollection _mainMenu;
    ItemsCollection _fullScreen;
    ItemsCollection _aspect;
    
    public ScreenSettings()
    {
      _fullScreen = new ItemsCollection();
      _fullScreen.Add(new ListItem("Name", new StringId("system", "yes")));
      _fullScreen.Add(new ListItem("Name", new StringId("system", "no")));

      _aspect = new ItemsCollection();
      _aspect.Add(new ListItem("Name", new StringId("settings", "aspectnormal")));
      _aspect.Add(new ListItem("Name", new StringId("settings", "aspectwide")));
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
          _mainMenu = MenuHelper.WrapMenu(menuCollect.GetMenu("settings-screen"));
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
        IScreenControl app = ServiceScope.Get<IScreenControl>();
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
      IScreenControl app = ServiceScope.Get<IScreenControl>();
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
    /// exposes the aspect rate options to the skin.
    /// </summary>
    public ItemsCollection Aspect
    {
      get
      {
        return _aspect;
      }
    }
    /// <summary>
    /// method for the skin to set the aspect rate.
    /// </summary>
    /// <param name="item">The item.</param>
    public void SetAspect(ListItem item)
    {

    }
  }
}
