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

#region Copyright (C) 2007-2009 Team MediaPortal

/* 
 *	Copyright (C) 2007-2009 Team MediaPortal
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

namespace Models.Weather.Grabbers
{
  /// <summary>
  /// Implementation of the IWeatherCatcher
  /// Interface which grabs the Data from
  /// www.weather.com
  /// </summary>
  public class TestCatcher : IWeatherCatcher
  {
    #region IWeatherCatcher Members

    /// <summary>
    /// returns the name of the service used to fetch the data,
    /// f.e. weather.com
    /// </summary>
    /// <returns></returns>
    public string GetServiceName()
    {
      return "TestCatcher.de";
    }

    /// <summary>
    /// downloads data from the internet and populates
    /// the city object with it.
    /// the searchId is the unique Id at the special
    /// service to identify the city
    /// </summary>
    /// <param name="city"></param>
    /// <returns>true if successful, false otherwise</returns>
    public bool GetLocationData(City city)
    {
      city.ForecastCollection = new DayForeCastCollection();
      city.ForecastCollection.Add(new DayForeCast());
      city.ForecastCollection.Add(new DayForeCast());
      city.ForecastCollection.Add(new DayForeCast());
      city.ForecastCollection.Add(new DayForeCast());
      city.Condition = new CurrentCondition();
      city.Condition.FillWithDummyData();
      city.LocationInfo = new LocInfo();
      return true;
    }

    /// <summary>
    /// returns a new List of City objects if the searched
    /// location was found... the unique id (City.Id) and the
    /// location name will be set for each City...
    /// </summary>
    /// <param name="locationName">name of the location to search for</param>
    /// <returns>new City List</returns>
    public List<CitySetupInfo> FindLocationsByName(string locationName)
    {
      // create list that will hold all found cities
      List<CitySetupInfo> locations = new List<CitySetupInfo>();
      locations.Add(new CitySetupInfo("Dresden, Germany", "DRDGE"));
      locations.Add(new CitySetupInfo("Amsterdamm, Netherlands", "AMSNL"));
      return locations;
    }

    #endregion
  }
}
