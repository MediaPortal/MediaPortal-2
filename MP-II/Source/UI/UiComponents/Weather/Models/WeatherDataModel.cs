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
using MediaPortal.Core;
using MediaPortal.Core.Settings;
using UiComponents.Weather.Grabbers;

namespace UiComponents.Weather
{
  /// <summary>
  ///  WeatherDataModel
  /// </summary>
  public class WeatherDataModel
  {
    /// <summary>
    /// construct the datamodel
    /// </summary>
    /// <param name="catcher"></param>
    public WeatherDataModel() {}

    /// <summary>
    /// this will retrieve data for all locations
    /// that can be found in the configuration file
    /// (configuration.xml)
    /// </summary>
    /// <returns></returns>
    public List<City> LoadLocationsData()
    {
      // look in the settings what locations are configured
      // and create a new list full of data by downloading and setting
      // it...
      WeatherSettings settings = ServiceScope.Get<ISettingsManager>().Load<WeatherSettings>();
      // check if loading went well, if not return null
      if (settings.LocationsList == null)
      {
        return null;
      }
      // now we got the provider info with id and name already filled in... lets get the
      // rest of the data! first turn the CityProviderInfo List into a City List
      List<City> citiesList = Helper.CityInfoListToCityObjectList(settings.LocationsList);
      // Get the WeatherCatchers that should be used from the Plugins (if availale)
      /******* TEMPORILY ADD EXISTING CATCHERS *****/
      //List<IWeatherCatcher> catchers = ServiceScope.Get<IPluginManager>().BuildItems<IWeatherCatcher>("/Models.Weather/Grabbers");
      List<IWeatherCatcher> catchers = new List<IWeatherCatcher>();
      catchers.Add(new WeatherDotComCatcher());

      // do this for all cities
      foreach (City c in citiesList)
      {
        // get the correct weather catcher for this town
        foreach (IWeatherCatcher grabber in catchers)
        {
          // look if we have the right catcher for this town
          if (grabber.GetServiceName().Equals(c.Grabber))
          {
            grabber.GetLocationData(c);
            break;
          }
        }
      }
      // nice, we should have it now... cities with none-successful update
      // will have the City.HasData attribute set to false
      return citiesList;
    }
  }
}
