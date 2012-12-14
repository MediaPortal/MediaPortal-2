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

using System.Globalization;
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Common;
using MediaPortal.Extensions.OnlineLibraries.Libraries.GeoLocation.Data;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.GeoLocation
{
  public class OsmNominatim : IGeolocationLookup
  {
    #region Fields

    protected Downloader _downloader = new Downloader();

    #endregion

    public bool TryLookup(double latitude, double longitude, out LocationInfo locationInfo)
    {
      ReverseGeoCodeResult result = _downloader.Download<ReverseGeoCodeResult>(BuildUrl(latitude, longitude), null, false);
      if (result == null)
      {
        locationInfo = null;
        return false;
      }
      locationInfo = result.ToLocation();
      return true;
    }

    protected string BuildUrl(double latitude, double longitude)
    {
      var mpLocal = ServiceRegistration.Get<ILocalization>().CurrentCulture.TwoLetterISOLanguageName;
      return
        string.Format(
          "http://nominatim.openstreetmap.org/reverse?format=json&lat={0}&lon={1}&zoom=10&accept-language={2}",
          latitude.ToString(CultureInfo.InvariantCulture),
          longitude.ToString(CultureInfo.InvariantCulture),
          mpLocal);
    }
  }
}
