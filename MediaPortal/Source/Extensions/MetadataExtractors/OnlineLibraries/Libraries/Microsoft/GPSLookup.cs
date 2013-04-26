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

#endregion Copyright (C) 2007-2013 Team MediaPortal

using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using System;
using System.Device.Location;
using System.Threading;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Microsoft
{
  public class GPSLookup : ICoordinateResolver, IAddressResolver, IDisposable
  {
    #region Private variables

    private GeoCoordinateWatcher _gps = new GeoCoordinateWatcher(GeoPositionAccuracy.High);
    private GeoCoordinate _coordinates;
    private CivicAddress _address;

    #endregion

    #region Ctor

    public GPSLookup()
    {
    }

    #endregion

    #region Private methods

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
          _gps.Stop();

          if (!tcs.TrySetResult(args.Position.Location))
            _gps.Start();
        };

      _gps.Start();

      if (tcs.Task.Wait(10000))
      {
        _coordinates = tcs.Task.Result;

        CivicAddressResolver resolver = new CivicAddressResolver();
        _address = resolver.ResolveAddress(_coordinates);

        coordinates = _coordinates;
        address = _address;
        return true;
      }

      coordinates = null;
      address = null;
      return false;
    }

    private void DisposeGPS()
    {
      if (_gps != null)
      {
        _gps.Stop();
        _gps.Dispose();
        _gps = null;
      }
    }

    #endregion Private methods

    #region IAddressResolver implementation

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