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

using System.Collections.Concurrent;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Extensions.OnlineLibraries.Libraries;
using MediaPortal.Extensions.OnlineLibraries.Libraries.FreeGeoIP;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Google;
using MediaPortal.Extensions.OnlineLibraries.Libraries.OpenStreetMap;
using MediaPortal.Utilities.Network;
using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Linq;
using System.Threading.Tasks;
using MediaPortal.Common.Async;
using MediaPortal.Common.Services.ServerCommunication;

namespace MediaPortal.Extensions.OnlineLibraries
{
  /// <summary>
  /// Service to lookup the location of a device.
  /// </summary>
  public class GeoLocationService : IGeoLocationService
  {
    #region Static instance

    public static IGeoLocationService Instance
    {
      get { return ServiceRegistration.Get<IGeoLocationService>(); }
    }

    #endregion Static instance

    #region Const

    /// <summary>
    /// The maximum distance in km where two locations are considered as equal (to avoid unneeded online lookups).
    /// </summary>
    private const double CACHE_MAX_DISANCE_KM = 1.0;

    #endregion Const

    #region Private variables

    private readonly IDictionary<GeoCoordinate, CivicAddress> _locationCache = new ConcurrentDictionary<GeoCoordinate, CivicAddress>();
    private IList<IAddressResolver> _addressResolvers;
    private IList<ICoordinateResolver> _coordinateResolvers;

    #endregion Private variables

    #region Ctor

    public GeoLocationService()
    {
    }

    #endregion Ctor

    #region Public methods

    public IList<IAddressResolver> GetCivicResolverServices()
    {
      if (_addressResolvers != null)
        return _addressResolvers;

      // Valk 2013-05-22: temporary removed because GPSLookup triggers an enternal security prompt which breaks the user experience.
      //return _addressResolvers = new List<IAddressResolver> { new GPSLookup(), new Maps(), new Geocoder(), new IPLookup() };
      return _addressResolvers = new List<IAddressResolver> { new Maps(), new Geocoder(), new IPLookup() };
    }

    public IList<ICoordinateResolver> GetCoordinateResolverServices()
    {
      if (_coordinateResolvers != null)
        return _coordinateResolvers;

      // Valk 2013-05-22: temporary removed because GPSLookup triggers an enternal security prompt which breaks the user experience.
      //return _coordinateResolvers = new List<ICoordinateResolver> { new GPSLookup(), new IPLookup() };
      return _coordinateResolvers = new List<ICoordinateResolver> { new IPLookup() };
    }

    #endregion Public methods

    #region Private methods

    private static double CalculateDistance(GeoCoordinate location1, GeoCoordinate location2)
    {
      const double EARTH_RADIUS_KM = 6376.5;
      double lat1InRad = DegreesToRadians(location1.Latitude);
      double long1InRad = DegreesToRadians(location1.Longitude);
      double lat2InRad = DegreesToRadians(location2.Latitude);
      double long2InRad = DegreesToRadians(location2.Longitude);

      double dLongitude = long2InRad - long1InRad;
      double dLatitude = lat2InRad - lat1InRad;
      double a = Math.Pow(Math.Sin(dLatitude / 2), 2) + Math.Cos(lat1InRad) * Math.Cos(lat2InRad) * Math.Pow(Math.Sin(dLongitude / 2), 2);
      double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
      return EARTH_RADIUS_KM * c;
    }

    private static double CalculateDistance(params GeoCoordinate[] locations)
    {
      double totalDistance = 0.0;

      for (int i = 0; i < locations.Length - 1; i++)
      {
        GeoCoordinate current = locations[i];
        GeoCoordinate next = locations[i + 1];

        totalDistance += CalculateDistance(current, next);
      }

      return totalDistance;
    }

    private static double DegreesToRadians(double degrees)
    {
      return degrees * Math.PI / 180.0;
    }

    private bool GetFromCache(GeoCoordinate coordinates, out CivicAddress address)
    {
      foreach (KeyValuePair<GeoCoordinate, CivicAddress> location in _locationCache.Where(info => CalculateDistance(info.Key, coordinates) <= CACHE_MAX_DISANCE_KM))
      {
        address = location.Value;
        return true;
      }

      address = null;
      return false;
    }

    #endregion Private methods

    #region IGeoLocationService implemention

    /// <summary>
    /// Lookup the location of the current device.
    /// </summary>
    /// <returns>
    /// AsyncResult.Success = <c>true</c> if successful.
    /// AsyncResult.Result.Item1 : Coordinates of the device.
    /// AsyncResult.Result.Item2 : Address of the device.
    /// </returns>
    public async Task<AsyncResult<Tuple<GeoCoordinate, CivicAddress>>> TryLookupAsync()
    {
      var falseResult = new AsyncResult<Tuple<GeoCoordinate, CivicAddress>>(false, null);
      if (!NetworkConnectionTracker.IsNetworkConnected)
        return falseResult;

      foreach (ICoordinateResolver coordinateResolverService in GetCoordinateResolverServices())
      {
        try
        {
          var coordinates = await coordinateResolverService.TryResolveCoordinatesAsync().ConfigureAwait(false);
          if (coordinates != null)
          {
            CivicAddress address;
            if (GetFromCache(coordinates, out address))
              return new AsyncResult<Tuple<GeoCoordinate, CivicAddress>>(true, new Tuple<GeoCoordinate, CivicAddress>(coordinates, address));

            foreach (IAddressResolver civicResolverService in GetCivicResolverServices())
            {
              try
              {
                address = await civicResolverService.TryResolveCivicAddressAsync(coordinates).ConfigureAwait(false);
                if (address != null)
                {
                  _locationCache[coordinates] = address;
                  return new AsyncResult<Tuple<GeoCoordinate, CivicAddress>>(true, new Tuple<GeoCoordinate, CivicAddress>(coordinates, address));
                }
              }
              catch (Exception ex)
              {
                ServiceRegistration.Get<ILogger>().Error("Error while executing IAddressResolver {0}.", ex, civicResolverService.GetType().Name);
              }
            }
          }
        }
        catch (Exception ex)
        {
          ServiceRegistration.Get<ILogger>().Error("Error while executing ICoordinateResolver {0}.", ex, coordinateResolverService.GetType().Name);
        }
      }
      return falseResult;
    }

    /// <summary>
    /// Lookup the address of a given location.
    /// </summary>
    /// <param name="coordinates">Coordinates to the location to lookup.</param>
    /// <returns>
    /// AsyncResult.Success = <c>true</c> if successful.
    /// AsyncResult.Result : Address of the device.
    /// </returns>
    public async Task<AsyncResult<CivicAddress>> TryLookupAsync(GeoCoordinate coordinates)
    {
      CivicAddress address;
      try
      {
        if (GetFromCache(coordinates, out address))
          return new AsyncResult<CivicAddress>(true, address);

        foreach (IAddressResolver civicResolverService in GetCivicResolverServices())
        {
          address = await civicResolverService.TryResolveCivicAddressAsync(coordinates).ConfigureAwait(false);
          if (address != null)
          {
            _locationCache[coordinates] = address;
            return new AsyncResult<CivicAddress>(true, address);
          }
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("Error while executing reverse geocoding.", ex);
        throw;
      }

      return new AsyncResult<CivicAddress>(false, null);
    }

    #endregion IGeoLocationService implemention
  }
}
