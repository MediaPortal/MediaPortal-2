#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Reflection;
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Plugins.MediaServer.ResourceAccess;
using MediaPortal.Utilities;
using UPnP.Infrastructure.Dv.DeviceTree;

namespace MediaPortal.Plugins.MediaServer
{
  public class MediaServerUpnPDeviceInformation : ILocalizedDeviceInformation
  {
    private const string RES_DEFAULT_FRIENDLY_NAME = "[MediaServer.DefaultFriendlyName]";
    private const string RES_MANUFACTURER = "[MediaServer.Manufacturer]";
    private const string RES_MANUFACTURER_URL = "[MediaServer.ManufacturerUrl]";
    private const string RES_MODEL_DESCRIPTION = "[MediaServer.ModelDescription]";
    private const string RES_MODEL_NAME = "[MediaServer.ModelName]";
    private const string RES_MODEL_NUMBER = "[MediaServer.ModelNumber]";
    private const string RES_MODEL_URL = "[MediaServer.ModelUrl]";

    public string FriendlyName = null;
    public string Manufacturer = null;
    public string ManufacturerURL = null;
    public string ModelDescription = null;
    public string ModelName = null;
    public string ModelNumber = null;
    public string ModelURL = null;
    public string SerialNumber = null;
    public string UPC = null;
    public ICollection<IconDescriptor> Icons = null;

    public MediaServerUpnPDeviceInformation()
    {
      FriendlyName = GetFriendlyName(CultureInfo.InvariantCulture);
      Manufacturer = GetManufacturer(CultureInfo.InvariantCulture);
      ManufacturerURL = GetManufacturerURL(CultureInfo.InvariantCulture);
      ModelDescription = GetModelDescription(CultureInfo.InvariantCulture);
      ModelName = GetModelName(CultureInfo.InvariantCulture);
      ModelNumber = GetModelNumber(CultureInfo.InvariantCulture);
      ModelURL = GetModelURL(CultureInfo.InvariantCulture);
      SerialNumber = GetSerialNumber(CultureInfo.InvariantCulture);
      UPC = GetUPC();
      Icons = GetIcons(CultureInfo.InvariantCulture);
    }

    public MediaServerUpnPDeviceInformation(ILocalizedDeviceInformation copyDevice)
    {
      FriendlyName = copyDevice.GetFriendlyName(CultureInfo.InvariantCulture);
      Manufacturer = copyDevice.GetManufacturer(CultureInfo.InvariantCulture);
      ManufacturerURL = copyDevice.GetManufacturerURL(CultureInfo.InvariantCulture);
      ModelDescription = copyDevice.GetModelDescription(CultureInfo.InvariantCulture);
      ModelName = copyDevice.GetModelName(CultureInfo.InvariantCulture);
      ModelNumber = copyDevice.GetModelNumber(CultureInfo.InvariantCulture);
      ModelURL = copyDevice.GetModelURL(CultureInfo.InvariantCulture);
      SerialNumber = copyDevice.GetSerialNumber(CultureInfo.InvariantCulture);
      UPC = copyDevice.GetUPC();
      Icons = GetIcons(CultureInfo.InvariantCulture);
    }

    public string GetFriendlyName(CultureInfo culture)
    {
      string result = null;
      try
      {
          result = StringUtils.TrimToNull(ServiceRegistration.Get<ILocalization>().ToString(RES_DEFAULT_FRIENDLY_NAME)) ?? GetModelName(culture);
      }
      catch { }
      if (string.IsNullOrEmpty(result))
      {
        result = "Media Server";
      }
      result += " (" + Dns.GetHostName() + ")";
      return result;
    }

    public string GetManufacturer(CultureInfo culture)
    {
      string result = null;
      try
      {
        result = StringUtils.TrimToNull(ServiceRegistration.Get<ILocalization>().ToString(RES_MANUFACTURER));
      }
      catch { }
      if (string.IsNullOrEmpty(result))
      {
        result = "Team MediaPortal";
      }
      return result;
    }

    public string GetManufacturerURL(CultureInfo culture)
    {
      string result = null;
      try
      {
        result = StringUtils.TrimToNull(ServiceRegistration.Get<ILocalization>().ToString(RES_MANUFACTURER_URL));
      }
      catch { }
      if (string.IsNullOrEmpty(result))
      {
        result = "www.team-mediaportal.com";
      }
      return result;
    }

    public string GetModelDescription(CultureInfo culture)
    {
      string result = null;
      try
      {
        result = StringUtils.TrimToNull(ServiceRegistration.Get<ILocalization>().ToString(RES_MODEL_DESCRIPTION));
      }
      catch { }
      if (string.IsNullOrEmpty(result))
      {
        result = "DLNA Media Server for MediaPortal 2";
      }
      return result;
    }

    public string GetModelName(CultureInfo culture)
    {
      string result = null;
      try
      {
        result = StringUtils.TrimToNull(ServiceRegistration.Get<ILocalization>().ToString(RES_MODEL_NAME));
      }
      catch { }
      if (string.IsNullOrEmpty(result))
      {
        result = "MediaPortal 2 Media Server";
      }
      return result;
    }

    public string GetModelNumber(CultureInfo culture)
    {
      string result = null;
      try
      {
        result = StringUtils.TrimToNull(ServiceRegistration.Get<ILocalization>().ToString(RES_MODEL_NUMBER));
      }
      catch { }
      if (string.IsNullOrEmpty(result))
      {
        result = Assembly.GetExecutingAssembly().GetName().Version.ToString();
      }
      return result;
    }

    public string GetModelURL(CultureInfo culture)
    {
      string result = null;
      try
      {
        result = StringUtils.TrimToNull(ServiceRegistration.Get<ILocalization>().ToString(RES_MODEL_URL));
      }
      catch { }
      if (string.IsNullOrEmpty(result))
      {
        result = "www.team-mediaportal.com";
      }
      return result;
    }

    public string GetSerialNumber(CultureInfo culture)
    {
      return null; // No serial number available
    }

    public string GetUPC()
    {
      return null; // No UPC available
    }

    public ICollection<IconDescriptor> GetIcons(CultureInfo culture)
    {
      if (Icons == null)
      {
        IconDescriptor png48 = new IconDescriptor { ColorDepth = 32, Width = 48, Height = 48, MimeType = "image/png", GetIconURLDelegate = (address, info) => GetIconUrl(48, "png") };
        IconDescriptor png120 = new IconDescriptor { ColorDepth = 32, Width = 120, Height = 120, MimeType = "image/png", GetIconURLDelegate = (address, info) => GetIconUrl(120, "png") };
        IconDescriptor png256 = new IconDescriptor { ColorDepth = 32, Width = 256, Height = 256, MimeType = "image/png", GetIconURLDelegate = (address, info) => GetIconUrl(256, "png") };
        IconDescriptor jpg48 = new IconDescriptor { ColorDepth = 32, Width = 48, Height = 48, MimeType = "image/jpg", GetIconURLDelegate = (address, info) => GetIconUrl(48, "jpg") };
        IconDescriptor jpg120 = new IconDescriptor { ColorDepth = 32, Width = 120, Height = 120, MimeType = "image/jpg", GetIconURLDelegate = (address, info) => GetIconUrl(120, "jpg") };
        IconDescriptor jpg256 = new IconDescriptor { ColorDepth = 32, Width = 256, Height = 256, MimeType = "image/jpg", GetIconURLDelegate = (address, info) => GetIconUrl(256, "jpg") };
        Icons = new List<IconDescriptor> { png48, png120, png256, jpg48, jpg120, jpg256 };
      }
      return Icons;
    }

    private string GetIconUrl(int size, string type)
    {
      return string.Format("{0}{1}/{2}?aspect=ICON&size={3}&type={4}", DlnaResourceAccessUtils.GetBaseResourceURL(), DlnaResourceAccessUtils.RESOURCE_ACCESS_PATH, Guid.Empty, size, type);
    }
  }
}
