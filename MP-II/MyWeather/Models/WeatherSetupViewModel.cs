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

#region Copyright (C) 2005-2008 Team MediaPortal

/* 
 *	Copyright (C) 2005-2008 Team MediaPortal
 *	http://www.team-mediaportal.com
 *
 *  This Program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2, or (at your option)
 *  any later version.
 *   
 *  This Program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *   
 *  You should have received a copy of the GNU General Public License
 *  along with GNU Make; see the file COPYING.  If not, write to
 *  the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA. 
 *  http://www.gnu.org/copyleft/gpl.html
 *
 */

#endregion

using System.Collections.Generic;
using MediaPortal.Core;
using MediaPortal.Core.Collections;
using MediaPortal.Core.Settings;
using MediaPortal.Core.WindowManager;
using MyWeather.Grabbers;

namespace MyWeather
{
  public class WeatherSetupViewModel
  {
    private List<CitySetupInfo> _locations; // locations that are already in the list
    private List<CitySetupInfo> _locationsSearch; // locations that return as result of searching for a city
    private readonly ItemsCollection _locationsExposed = new ItemsCollection(); // Listcollection to expose _locations

    private readonly ItemsCollection _locationsSearchExposed = new ItemsCollection();
    // Listcollection to expose _locationsExposed

    /// <summary>
    /// constructor
    /// </summary>
    public WeatherSetupViewModel()
    {
      // see if we already have a weather catcher in servicescope, if not, add one (for testing purposes)
      if (!ServiceScope.IsRegistered<IWeatherCatcher>())
      {
        ServiceScope.Add<IWeatherCatcher>(new WeatherDotComCatcher());
      }
      // load settings
      GetLocationsFromSettings();
    }

    /// <summary>
    /// load settings
    /// </summary>
    private void GetLocationsFromSettings()
    {
      WeatherSettings settings = new WeatherSettings();
      ServiceScope.Get<ISettingsManager>().Load(settings);
      Locations = settings.LocationsList;
    }

    /// <summary>
    /// search for a location name and fill up the _locationsSearch list
    /// </summary>
    /// <param name="name"></param>
    public void SearchLocations(string name)
    {
      // get a grabber to search for citys
      ServiceScope.Get<IWindowManager>().CurrentWindow.WaitCursorVisible = true;
      LocationsSearch = ServiceScope.Get<IWeatherCatcher>().FindLocationsByName(name);
      ServiceScope.Get<IWindowManager>().CurrentWindow.WaitCursorVisible = false;
    }

    /// <summary>
    /// saves the current state to the settings
    /// </summary>
    public void SaveSettings()
    {
      ServiceScope.Get<IWindowManager>().CurrentWindow.WaitCursorVisible = true;
      WeatherSettings settings = new WeatherSettings();
      // Load Settings
      ServiceScope.Get<ISettingsManager>().Load(settings);
      // apply new locations list
      settings.LocationsList = Locations;
      // save
      ServiceScope.Get<ISettingsManager>().Save(settings);
      ServiceScope.Get<IWindowManager>().CurrentWindow.WaitCursorVisible = false;
    }

    /// <summary>
    /// this will add a location
    /// to the _locationsExposed and _locations list
    /// </summary>
    /// <param name="item"></param>
    public void AddLocation(ListItem item)
    {
      // we don't add it if it's already in there
      foreach (ListItem i in _locationsExposed)
      {
        if (i.Labels["Id"].Evaluate(null, null).Equals(item.Labels["Id"].Evaluate(null, null)))
        {
          return;
        }
      }
      _locationsExposed.Add(item);
      // create a CitySetupObject and add it to the loctions list
      CitySetupInfo c =
        new CitySetupInfo(item.Labels["Name"].Evaluate(null, null), item.Labels["Id"].Evaluate(null, null));
      _locations.Add(c);
      _locationsExposed.FireChange();
    }

    /// <summary>
    /// this will delete a location from the _locationsExposed list
    /// </summary>
    /// <param name="item"></param>
    public void Delete(ListItem item)
    {
      if (_locationsExposed.Contains(item))
      {
        _locationsExposed.Remove(item);
        _locationsExposed.FireChange();
        string id = item.Labels["Id"].Evaluate(null, null);
        foreach (CitySetupInfo info in _locations)
        {
          if (info.Id == id)
          {
            _locations.Remove(info);
            return;
          }
        }
      }
    }

    /// <summary>
    /// gets or sets the Locations
    /// </summary>
    public List<CitySetupInfo> Locations
    {
      get { return _locations; }

      set
      {
        _locations = value;
        if (_locations == null)
        {
          return;
        }

        _locationsExposed.Clear();
        ListItem buff;

        foreach (CitySetupInfo c in _locations)
        {
          if (c != null)
          {
            buff = new ListItem();
            buff.Add("Name", c.Name);
            buff.Add("Id", c.Id);
            _locationsExposed.Add(buff);
          }
        }
        _locationsExposed.FireChange();
      }
    }

    /// <summary>
    /// gets or sets the found Locations
    /// </summary>
    public List<CitySetupInfo> LocationsSearch
    {
      get { return _locationsSearch; }

      set
      {
        _locationsSearch = value;
        if (_locationsSearch == null)
        {
          return;
        }

        _locationsSearchExposed.Clear();
        ListItem buff;
        foreach (CitySetupInfo c in _locationsSearch)
        {
          if (c != null)
          {
            buff = new ListItem();
            buff.Add("Name", c.Name);
            buff.Add("Id", c.Id);
            _locationsSearchExposed.Add(buff);
          }
        }
        _locationsSearchExposed.FireChange();
      }
    }

    public ItemsCollection SetupLocations
    {
      get { return _locationsExposed; }
    }

    public ItemsCollection SetupSearchLocations
    {
      get { return _locationsSearchExposed; }
    }
  }
}
