#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

using System;
using System.Device.Location;
using System.Threading.Tasks;
using MediaPortal.Common.Async;
using MediaPortal.Common.Services.ServerCommunication;

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
    /// <returns>
    /// AsyncResult.Success = <c>true</c> if successful.
    /// AsyncResult.Result.Item1 : Coordinates of the device.
    /// AsyncResult.Result.Item2 : Address of the device.
    /// </returns>
    Task<AsyncResult<Tuple<GeoCoordinate, CivicAddress>>> TryLookupAsync();

    /// <summary>
    /// Lookup the address of a given location.
    /// </summary>
    /// <param name="coordinates">Coordinates to the location to lookup.</param>
    /// <returns>
    /// AsyncResult.Success = <c>true</c> if successful.
    /// AsyncResult.Result : Address of the device.
    /// </returns>
    Task<AsyncResult<CivicAddress>> TryLookupAsync(GeoCoordinate coordinates);
  }
}
