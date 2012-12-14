#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Extensions.OnlineLibraries.Libraries.GeoLocation;
using MediaPortal.Extensions.OnlineLibraries.Libraries.GeoLocation.Data;

namespace MediaPortal.Extensions.OnlineLibraries
{
  /// <summary>
  /// <see cref="GeoLocationMatcher"/> is used to lookup geographic locations from given coordinates (latitude, longitude).
  /// </summary>
  public class GeoLocationMatcher
  {
    #region Static instance

    public static GeoLocationMatcher Instance
    {
      get { return ServiceRegistration.Get<GeoLocationMatcher>(); }
    }

    #endregion

    public bool TryLookup(double latitude, double longitude, out LocationInfo locationInfo)
    {
      try
      {
        IGeolocationLookup lookup = new OsmNominatim();
        return lookup.TryLookup(latitude, longitude, out locationInfo);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("Error while executing reverse geocoding.", ex);
        locationInfo = null;
        return false;
      }
    }
  }
}
