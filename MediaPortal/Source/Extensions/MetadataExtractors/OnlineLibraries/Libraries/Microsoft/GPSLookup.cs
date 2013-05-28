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
      _gps.PositionChanged += GPSOnPositionChanged;
      _gps.StatusChanged += GPSOnStatusChanged;
      _gps.Start(true);
    }

    #endregion

    #region Private methods

    private void GPSOnStatusChanged(object sender, GeoPositionStatusChangedEventArgs geoPositionStatusChangedEventArgs)
    {
      if (geoPositionStatusChangedEventArgs.Status == GeoPositionStatus.Disabled || geoPositionStatusChangedEventArgs.Status == GeoPositionStatus.NoData)
      {
        _gps.Stop();
      }
    }

    private void GPSOnPositionChanged(object sender, GeoPositionChangedEventArgs<GeoCoordinate> geoPositionChangedEventArgs)
    {
      _gps.Stop();

      _coordinates = new GeoCoordinate(
        geoPositionChangedEventArgs.Position.Location.Latitude,
        geoPositionChangedEventArgs.Position.Location.Longitude,
        geoPositionChangedEventArgs.Position.Location.Altitude,
        geoPositionChangedEventArgs.Position.Location.HorizontalAccuracy,
        geoPositionChangedEventArgs.Position.Location.VerticalAccuracy,
        geoPositionChangedEventArgs.Position.Location.Speed,
        geoPositionChangedEventArgs.Position.Location.Course);

      CivicAddressResolver resolver = new CivicAddressResolver();
      _address = resolver.ResolveAddress(_coordinates);
    }

    private bool TryGPSLookupInternal(out GeoCoordinate coordinates, out CivicAddress address)
    {
      if (_coordinates.IsUnknown)
      {
        coordinates = null;
        address = null;
        return false;
      }

      CivicAddressResolver resolver = new CivicAddressResolver();
      _address = resolver.ResolveAddress(_coordinates);

      coordinates = _coordinates;
      address = _address;

      return true;
    }

    private void DisposeGPS()
    {
      _gps.PositionChanged -= GPSOnPositionChanged;
      _gps.StatusChanged -= GPSOnStatusChanged;

      _gps.Dispose();
      _gps = null;
    }

    #endregion Private methods

    #region ICivicAddressResolver implementation

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

    #endregion ICivicAddressResolver implementation

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