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
using System.Net;
using System.Threading.Tasks;
using MediaPortal.Common.Async;
using MediaPortal.Common.Services.ServerCommunication;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Microsoft
{
  /// <summary>
  /// Uses Microsofts GeoCoordinateWatcher to resolve the location of the device and lookup the address of the specified coordinates.
  /// </summary>
  public class GPSLookup : ICoordinateResolver, IAddressResolver, IDisposable
  {
    #region Private variables

    private CivicAddress _address;
    private GeoCoordinate _coordinates;
    private GeoCoordinateWatcher _gps = new GeoCoordinateWatcher(GeoPositionAccuracy.High);

    #endregion Private variables

    #region Ctor

    public GPSLookup()
    {
    }

    #endregion Ctor

    #region Private methods

    private void DisposeGPS()
    {
      if (_gps != null)
      {
        _gps.Stop();
        _gps.Dispose();
        _gps = null;
      }
    }

    private async Task<AsyncResult<Tuple<CivicAddress, GeoCoordinate>>> TryLookupInternal()
    {
      // Check if we've already looked up the location.
      if (_coordinates != null && _address != null)
      {
        if (!_coordinates.IsUnknown && !_address.IsUnknown)
          return new AsyncResult<Tuple<CivicAddress, GeoCoordinate>>(true, new Tuple<CivicAddress, GeoCoordinate>(_address, _coordinates));
      }

      TaskCompletionSource<GeoCoordinate> tcs = new TaskCompletionSource<GeoCoordinate>();

      _gps.PositionChanged += (sender, args) =>
        {
          // Need to stop the GPS ASAP otherwise it might trigger again.
          _gps.Stop();

          if (!tcs.TrySetResult(args.Position.Location))
            _gps.Start(suppressPermissionPrompt: true);
        };

      _gps.Start(suppressPermissionPrompt: true);

      if (tcs.Task.Wait(10000)) // 10 seconds.
      {
        _coordinates = tcs.Task.Result;

        CivicAddressResolver resolver = new CivicAddressResolver();
        _address = resolver.ResolveAddress(_coordinates);

        return new AsyncResult<Tuple<CivicAddress, GeoCoordinate>>(true, new Tuple<CivicAddress, GeoCoordinate>(_address, _coordinates));
      }

      _gps.Stop();

      return new AsyncResult<Tuple<CivicAddress, GeoCoordinate>>(false, null);
    }

    #endregion Private methods

    #region IAddressResolver implementation

    /// <summary>
    /// Retrieve the Address based on the coordinates given.
    /// </summary>
    /// <param name="coordinates">Coordinates to lookup.</param>
    /// <returns>Address corresponding to the coordinates or <c>null</c>.</returns>
    public async Task<CivicAddress> TryResolveCivicAddressAsync(GeoCoordinate coordinates)
    {
      if (_address != null)
        return _address;

      var result = await TryLookupInternal().ConfigureAwait(false);
      if (result.Success)
        return result.Result.Item1;
      return null;
    }

    #endregion IAddressResolver implementation

    #region ICoordinateResolver implementation

    /// <summary>
    /// Lookup the coordinates of the current device.
    /// </summary>
    /// <returns>Location of the device if successful.</returns>
    public async Task<GeoCoordinate> TryResolveCoordinatesAsync()
    {
      if (_coordinates != null && !_coordinates.IsUnknown)
        return _coordinates;

      var result = await TryLookupInternal().ConfigureAwait(false);
      if (result.Success)
        return result.Result.Item2;
      return null;
    }

    #endregion ICoordinateResolver implementation

    #region IDisposable implementation

    public void Dispose()
    {
      DisposeGPS();
    }

    #endregion IDisposable implementation
  }
}
