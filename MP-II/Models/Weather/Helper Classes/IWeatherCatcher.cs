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

namespace Models.Weather
{
  /// <summary>
  /// this is an Interface for catching weather data
  /// from a website...
  /// any Implementation will take a City object and populate it with data
  /// </summary>
  public interface IWeatherCatcher
  {
    /// <summary>
    /// downloads data from the internet and populates
    /// the city object with it.
    /// the city object should already hold id and name
    /// (f.e. from FindLoationsByName or Settings)
    /// </summary>
    /// <param name="city"></param>
    /// <returns></returns>
    bool GetLocationData(City city);

    /// <summary>
    /// returns a new List of City objects if the searched
    /// location was found... the unique id (City.Id) and the
    /// location name will be set for each City...
    /// </summary>
    /// <param name="name">name of the location to search for</param>
    /// <returns>new City List</returns>
    List<CitySetupInfo> FindLocationsByName(string name);

    /// <summary>
    /// returns the name of the service used to fetch the data,
    /// f.e. weather.com (used in configuration)
    /// </summary>
    /// <returns></returns>
    string GetServiceName();
  }
}
