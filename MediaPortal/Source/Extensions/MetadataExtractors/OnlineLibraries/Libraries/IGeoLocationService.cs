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

using System.Device.Location;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries
{
  /// <summary>
  /// Service to determine the location of a device.
  /// </summary>
  public interface IGeoLocationService
  {
    /// <summary>
    /// Lookup the location of the current device.
    /// </summary>
    /// <param name="coordinates">Coordinates of the device.</param>
    /// <param name="address">Address of the device.</param>
    /// <returns>If lookup is successful.</returns>
    bool TryLookup(out GeoCoordinate coordinates, out CivicAddress address);

    /// <summary>
    /// Lookup the address of a given location.
    /// </summary>
    /// <param name="coordinates">Coordinates to the location to lookup.</param>
    /// <param name="address">Address to the coordinates passed.</param>
    /// <returns>If lookup is successful.</returns>
    bool TryLookup(GeoCoordinate coordinates, out CivicAddress address);
  }
}