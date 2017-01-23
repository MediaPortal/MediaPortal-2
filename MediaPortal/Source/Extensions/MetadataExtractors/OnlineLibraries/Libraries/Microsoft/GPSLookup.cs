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

using System;
using System.Device.Location;
using System.Threading.Tasks;

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

    private bool TryGPSLookupInternal(out GeoCoordinate coordinates, out CivicAddress address)
    {
      // Check if we've already looked up the location.
      if (_coordinates != null && _address != null)
      {
        if (!_coordinates.IsUnknown && !_address.IsUnknown)
        {
          coordinates = _coordinates;
          address = _address;
          return true;
        }
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

        coordinates = _coordinates;
        address = _address;
        return true;
      }

      _gps.Stop();

      coordinates = null;
      address = null;
      return false;
    }

    #endregion Private methods

    #region IAddressResolver implementation

    /// <summary>
    /// Retrieve the Address based on the coordinates given.
    /// </summary>
    /// <param name="coordinates">Location to lookup.</param>
    /// <param name="address">Resultant address of the lookup.</param>
    /// <returns>If lookup is successful.</returns>
    public bool TryResolveCivicAddress(GeoCoordinate coordinates, out CivicAddress address)
    {
      if (_address != null)
      {
        address = _address;
        return true;
      }

      GeoCoordinate temp;
      return TryGPSLookupInternal(out temp, out address);
    }

    #endregion IAddressResolver implementation

    #region ICoordinateResolver implementation

    /// <summary>
    /// Lookup the coordinates of the current device.
    /// </summary>
    /// <param name="coordinates">Location of the device.</param>
    /// <returns>If lookup is successful.</returns>
    public bool TryResolveCoordinates(out GeoCoordinate coordinates)
    {
      if (_coordinates != null)
      {
        if (!_coordinates.IsUnknown)
        {
          coordinates = _coordinates;
          return true;
        }
      }

      CivicAddress temp;
      return TryGPSLookupInternal(out coordinates, out temp);
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