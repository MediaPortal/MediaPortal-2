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

#region Imports

using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Extensions.GeoLocation.IPLookup;
using MediaPortal.Extensions.GeoLocation.IPLookup.Data;
using MediaPortal.Extensions.OnlineLibraries.Libraries.GeoLocation.Data;
using System;
using System.Device.Location;
using System.Threading;

#endregion Imports

namespace MediaPortal.Extensions.GeoLocation
{
  public class GeoLocationService : IGeoLocationService, IDisposable
  {
    #region Private variables

    private GeoCoordinateWatcher _gps = new GeoCoordinateWatcher(GeoPositionAccuracy.High) { MovementThreshold = 1000 };
    private LocationInfo _currentLocation;

    private bool _updateComplete = false;

    #endregion Private variables

    #region Ctor

    public GeoLocationService()
    {
      _gps.PositionChanged += GPSOnPositionChanged;
      _gps.StatusChanged += GPSOnStatusChanged;
    }

    #endregion Ctor

    #region Private methods

    private void GPSOnStatusChanged(object sender, GeoPositionStatusChangedEventArgs geoPositionStatusChangedEventArgs)
    {
      if (geoPositionStatusChangedEventArgs.Status == GeoPositionStatus.Disabled || geoPositionStatusChangedEventArgs.Status == GeoPositionStatus.NoData)
      {
        _gps.Stop();

        _updateComplete = true;
      }
    }

    private void GPSOnPositionChanged(object sender, GeoPositionChangedEventArgs<GeoCoordinate> geoPositionChangedEventArgs)
    {
      _gps.Stop();

      _currentLocation = new LocationInfo()
      {
        Latitude = geoPositionChangedEventArgs.Position.Location.Latitude,
        Longitude = geoPositionChangedEventArgs.Position.Location.Longitude,
      };

      _updateComplete = true;
    }

    private LocationInfo GPSLookupInternal()
    {
      _gps.Start(true);

      while (!_updateComplete)
      {
        Thread.Sleep(60);
      }

      if(_currentLocation == null)
        ServiceRegistration.Get<ILogger>().Debug("GeoLocation: GPS Lookup Failed.");

      return _currentLocation;
    }

    private void DisposeGPS()
    {
      _gps.PositionChanged -= GPSOnPositionChanged;
      _gps.StatusChanged -= GPSOnStatusChanged;

      _gps.Dispose();
      _gps = null;
    }

    private LocationInfo IPLookupInternal()
    {
      TraceRoute routeLookup = new TraceRoute();

      TraceRouteResponse firstRemoteNode = null;
      if (routeLookup.TryLookup("google.com", 30, out firstRemoteNode))
      {
        FreeGeoIPLookup locationLookup = new FreeGeoIPLookup();

        LocationInfo location = null;
        if (locationLookup.TryLookup(firstRemoteNode.RemoteHost, out location))
        {
          return location;
        }
      }

      ServiceRegistration.Get<ILogger>().Debug("GeoLocation: IPLookup Failed.");

      return null;
    }

    #endregion Private methods

    #region IGeoLocationService implementation

    public LocationInfo Lookup()
    {
      return GPSLookupInternal() ?? IPLookupInternal();
    }

    #endregion IGeoLocationService implementation

    #region IDisposable implementation

    public void Dispose()
    {
      DisposeGPS();
    }

    #endregion IDisposable implementation
  }
}