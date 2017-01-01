#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Collections.Generic;

namespace MediaPortal.UiComponents.Weather
{
  /// <summary>
  /// Interface for catching weather data from a given website or collection of websites.
  /// Any Implementation will take a City object and populate it with data.
  /// </summary>
  public interface IWeatherCatcher
  {
    /// <summary>
    /// Downloads data from the internet and populates the city object with it.
    /// The city object should already hold id and name (f.e. from FindLoationsByName or Settings).
    /// </summary>
    /// <param name="city">City object to populate</param>
    /// <returns><c>true</c>, if the update was successful, else <c>false</c>.</returns>
    bool GetLocationData(City city);

    /// <summary>
    /// Returns a new List of City objects for the searched location. The unique id (City.Id) and the
    /// location name will be set for each City.
    /// </summary>
    /// <param name="name">Name of the location to search for.</param>
    /// <returns>New City List.</returns>
    List<CitySetupInfo> FindLocationsByName(string name);

    /// <summary>
    /// Teturns the name of the service used to fetch data, e.g. weather.com (used in configuration).
    /// </summary>
    /// <returns></returns>
    string GetServiceName();
  }
}
