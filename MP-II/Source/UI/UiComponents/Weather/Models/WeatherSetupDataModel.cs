#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace UiComponents.Weather
{

  #region WeatherSetupDataModel

  /// <summary>
  /// Datamodel which holds a list of locations,
  /// either for the Cities that have been found while searching
  /// for a specific location or for the list of the cities
  /// that are being added by the setup
  /// </summary>
  public class WeatherSetupDataModel : INotifyPropertyChanged
  {
    protected List<CitySetupInfo> _locations = new List<CitySetupInfo>();
    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// Gets the programs.
    /// </summary>
    /// <value>IList containing 0 or more City instances.</value>
    public IList Locations
    {
      get { return _locations; }
      set
      {
        if (value != null)
        {
          _locations = (List<CitySetupInfo>) value;
          // update
          if (PropertyChanged != null)
          {
            PropertyChanged(this, new PropertyChangedEventArgs("Locations"));
          }
        }
      }
    }

    /// <summary>
    /// adds a new city to the model
    /// </summary>
    /// <param name="city"></param>
    public void AddCity(CitySetupInfo city)
    {
      // add if not already added
      foreach (CitySetupInfo c in _locations)
      {
        if (c != null && c.Id.Equals(city.Id))
        {
          return;
        }
      }

      _locations.Add(city);

      // update
      if (PropertyChanged != null)
      {
        PropertyChanged(this, new PropertyChangedEventArgs("Locations"));
      }
    }

    /// <summary>
    /// adds a new city to the datamodel
    /// </summary>
    /// <param name="location"></param>
    /// <param name="id"></param>
    public void AddCity(string location, string id)
    {
      AddCity(new CitySetupInfo(location, id));
    }

    /// <summary>
    /// removes a city from the model
    /// </summary>
    /// <param name="city"></param>
    public void RemoveCity(CitySetupInfo city)
    {
      _locations.Remove(city);
      if (PropertyChanged != null)
      {
        PropertyChanged(this, new PropertyChangedEventArgs("Locations"));
      }
    }

    /// <summary>
    /// searches online for available cities
    /// with the given name and lists them up
    /// </summary>
    /// <param name="searchString">city name to search for</param>
    /// <returns></returns>
    /// <param name="grabber"></param>
    public void SearchCity(string searchString, IWeatherCatcher grabber)
    {
      // find the possible cities through the weather catcher
      _locations.Clear();
      // search for the cities
      List<CitySetupInfo> tempList;
      tempList = grabber.FindLocationsByName(searchString);
      // add them to the list
      foreach (CitySetupInfo c in tempList)
      {
        _locations.Add(c);
      }
      // Update the Gui that the locations list might have changed
      if (PropertyChanged != null)
      {
        PropertyChanged(this, new PropertyChangedEventArgs("Locations"));
      }
    }
  }

  #endregion
}
