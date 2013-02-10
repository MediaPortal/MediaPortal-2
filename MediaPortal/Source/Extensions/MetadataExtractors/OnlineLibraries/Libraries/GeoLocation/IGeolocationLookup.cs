#region Copyright (C) 2007-2013 Team MediaPortal

/*
    Copyright (C) 2007-2013 Team MediaPortal
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

using MediaPortal.Extensions.OnlineLibraries.Libraries.GeoLocation.Data;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.GeoLocation
{
  /// <summary>
  /// <see cref="IGeolocationLookup"/> provides features to translate geographic coordinates into location names (city, state, country...)
  /// </summary>
  public interface IGeolocationLookup
  {
    /// <summary>
    /// Tries to lookup a location based on the <paramref name="latitude"/> and <paramref name="longitude"/>.
    /// </summary>
    /// <param name="latitude">Latitude</param>
    /// <param name="longitude">Longitude</param>
    /// <param name="locationInfo">Returns the <see cref="LocationInfo"/> if successful</param>
    /// <returns><c>true</c> if successful</returns>
    bool TryLookup(double latitude, double longitude, out LocationInfo locationInfo);
  }
}
