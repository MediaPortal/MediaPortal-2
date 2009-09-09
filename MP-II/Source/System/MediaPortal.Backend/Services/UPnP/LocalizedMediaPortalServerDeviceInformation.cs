#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Collections.Generic;
using System.Globalization;
using UPnP.Infrastructure.Dv.DeviceTree;

namespace MediaPortal.Services.UPnP
{
Rename to LocalizedUPnPDeviceInformation
  public class LocalizedMediaPortalServerDeviceInformation : ILocalizedDeviceInformation
  {
weiter:
 - Language-Ressourcen-Plugin für Server erstellen
 - Folgende Language-Ressourcen aufnehmen
    public const string RES_FRIENDLY_NAME = "FriendlyName";
    public const string RES_MANUFACTURER = "Manufacturer";
    public const string RES_MANUFACTURER_URL = "ManufacturerUrl";
    public const string RES_MODEL_DESCRIPTION = "ModelDescription";
    public const string RES_MODEL_NAME = "ModelName";
    public const string RES_MODEL_NUMBER = "ModelNumber";
    public const string RES_MODEL_URL = "ModelUrl";
    public const string RES_UPC = "UPC";
    
    public string GetFriendlyName(CultureInfo culture)
    {
      return ServiceScope.Get<ILocalization>.ToString(culture, UPNPSERVER_SECTION, RES_FRIENDLY_NAME);
    }

    public string GetManufacturer(CultureInfo culture)
    {
      return ServiceScope.Get<ILocalization>.ToString(culture, UPNPSERVER_SECTION, RES_MANUFACTURER);
    }

    public string GetManufacturerURL(CultureInfo culture)
    {
      return ServiceScope.Get<ILocalization>.ToString(culture, UPNPSERVER_SECTION, RES_MANUFACTURER_URL);
    }

    public string GetModelDescription(CultureInfo culture)
    {
      return ServiceScope.Get<ILocalization>.ToString(culture, UPNPSERVER_SECTION, RES_MODEL_DESCRIPTION);
    }

    public string GetModelName(CultureInfo culture)
    {
      return ServiceScope.Get<ILocalization>.ToString(culture, UPNPSERVER_SECTION, RES_MODEL_NAME);
    }

    public string GetModelNumber(CultureInfo culture)
    {
      return ServiceScope.Get<ILocalization>.ToString(culture, UPNPSERVER_SECTION, RES_MODEL_NUMBER);
    }

    public string GetModelURL(CultureInfo culture)
    {
      return ServiceScope.Get<ILocalization>.ToString(culture, UPNPSERVER_SECTION, RES_MODEL_URL);
    }

    public string GetSerialNumber(CultureInfo culture)
    {
      TODO: Where do we get a serial number for an MP-II-Installation?
    }

    public string GetUPC()
    {
      return ServiceScope.Get<ILocalization>.ToString(culture, UPNPSERVER_SECTION, RES_UPC);
    }

    public ICollection<IconDescriptor> GetIcons(CultureInfo culture)
    {
      TODO: Register icon(s) at web service, create icon descriptor
    }
  }
}
