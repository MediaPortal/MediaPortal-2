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
using MediaPortal.Interfaces.Core.PluginManager;

namespace Models.Settings
{
  public class RefreshrateSettings : IPlugin
  {
    ItemsCollection _mainMenu;
    ItemsCollection _refreshrates;
    ItemsCollection _refreshRateControl;
    
    #region IPlugin Members
    public void Initialise()
    {
    }

    public void Dispose()
    {
    }
    #endregion

    public RefreshrateSettings()
    {
      IApplication app = ServiceScope.Get<IApplication>();
      _refreshrates = new ItemsCollection();
      foreach (string mode in app.DisplayModes)
        _refreshrates.Add(new ListItem("Name", mode));

      _refreshRateControl = new ItemsCollection();
      _refreshRateControl.Add(new ListItem("Name", new StringId("system", "yes")));
      _refreshRateControl.Add(new ListItem("Name", new StringId("system", "no")));
    }

    /// <summary>
    /// exposes the main video-settings menu to the skin
    /// </summary>
    /// <value>The main menu.</value>
    public ItemsCollection MainMenu
    {
      get
      {
        if (_mainMenu == null)
        {
          IMenuCollection menuCollect = ServiceScope.Get<IMenuCollection>();
          _mainMenu = new ItemsCollection(menuCollect.GetMenu("settings-refreshrate-main"));
        }
        return _mainMenu;
      }
    }


    public ItemsCollection RefreshRateControl
    {
      get
      {
        IApplication app = ServiceScope.Get<IApplication>();
        bool enabled = app.RefreshRateControlEnabled;

        _refreshRateControl[0].Selected = enabled;
        _refreshRateControl[1].Selected = !enabled;
        return _refreshRateControl;
      }
    }

    /// <summary>
    /// method for the skin to enable /disable refresh rate control.
    /// </summary>
    /// <param name="item">The item.</param>
    public void SetRefreshRateControl(ListItem item)
    {
      IApplication app = ServiceScope.Get<IApplication>();
      if (item == _refreshRateControl[0])
      {
        app.RefreshRateControlEnabled = true;
      }
      else
      {
        app.RefreshRateControlEnabled = false;
      }
    }

    public ItemsCollection RefreshRate24
    {
      get
      {
        SetSelectedRefreshRate24();
        return _refreshrates;
      }
    }

    void SetSelectedRefreshRate24()
    {
      IApplication app = ServiceScope.Get<IApplication>();
      foreach (ListItem item in _refreshrates)
      {
        item.Selected = (item.Label("Name", "").Evaluate() == app.GetDisplayMode(FPS.FPS_24));
      }
    }

    public void SetRefreshRate24(ListItem item)
    {
      IApplication app = ServiceScope.Get<IApplication>();
      string refreshrateChosen = item.Label("Name", "").Evaluate();
      app.SetDisplayMode(FPS.FPS_24, refreshrateChosen);
    }

    public ItemsCollection RefreshRate25
    {
      get
      {
        SetSelectedRefreshRate25();
        return _refreshrates;
      }
    }

    void SetSelectedRefreshRate25()
    {
      IApplication app = ServiceScope.Get<IApplication>();
      foreach (ListItem item in _refreshrates)
      {
        item.Selected = (item.Label("Name", "").Evaluate() == app.GetDisplayMode(FPS.FPS_25));
      }
    }

    public void SetRefreshRate25(ListItem item)
    {
      IApplication app = ServiceScope.Get<IApplication>();
      string refreshrateChosen = item.Label("Name", "").Evaluate();
      app.SetDisplayMode(FPS.FPS_25, refreshrateChosen);
    }

    public ItemsCollection RefreshRate30
    {
      get
      {
        SetSelectedRefreshRate30();
        return _refreshrates;
      }
    }

    void SetSelectedRefreshRate30()
    {
      IApplication app = ServiceScope.Get<IApplication>();
      foreach (ListItem item in _refreshrates)
      {
        item.Selected = (item.Label("Name", "").Evaluate() == app.GetDisplayMode(FPS.FPS_30));
      }
    }

    public void SetRefreshRate30(ListItem item)
    {
      IApplication app = ServiceScope.Get<IApplication>();
      string refreshrateChosen = item.Label("Name", "").Evaluate();
      app.SetDisplayMode(FPS.FPS_30, refreshrateChosen);
    }

    public ItemsCollection RefreshRateDefault
    {
      get
      {
        SetSelectedRefreshRateDefault();
        return _refreshrates;
      }
    }

    void SetSelectedRefreshRateDefault()
    {
      IApplication app = ServiceScope.Get<IApplication>();
      foreach (ListItem item in _refreshrates)
      {
        item.Selected = (item.Label("Name", "").Evaluate() == app.GetDisplayMode(FPS.Default));
      }
    }

    public void SetRefreshRateDefault(ListItem item)
    {
      IApplication app = ServiceScope.Get<IApplication>();
      string refreshrateChosen = item.Label("Name", "").Evaluate();
      app.SetDisplayMode(FPS.Default, refreshrateChosen);
    }
  }
}
