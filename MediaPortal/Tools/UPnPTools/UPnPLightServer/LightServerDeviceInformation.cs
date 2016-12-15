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

using System.Collections.Generic;
using System.Globalization;
using UPnP.Infrastructure.Dv.DeviceTree;

namespace UPnPLightServer
{
  public class LightServerDeviceInformation : ILocalizedDeviceInformation
  {
    public string GetFriendlyName(CultureInfo culture)
    {
      return "Light Server";
    }

    public string GetManufacturer(CultureInfo culture)
    {
      return "Team MediaPortal";
    }

    public string GetManufacturerURL(CultureInfo culture)
    {
      return "http://www.team-mediaportal.com";
    }

    public string GetModelDescription(CultureInfo culture)
    {
      return null;
    }

    public string GetModelName(CultureInfo culture)
    {
      return "Basic";
    }

    public string GetModelNumber(CultureInfo culture)
    {
      return "1";
    }

    public string GetModelURL(CultureInfo culture)
    {
      return null;
    }

    public string GetSerialNumber(CultureInfo culture)
    {
      return null;
    }

    public string GetUPC()
    {
      return null;
    }

    public ICollection<IconDescriptor> GetIcons(CultureInfo culture)
    {
      return null;
    }
  }
}