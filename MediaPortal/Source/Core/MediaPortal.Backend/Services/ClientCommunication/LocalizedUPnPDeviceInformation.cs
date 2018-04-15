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
using MediaPortal.Backend.BackendServer.Settings;
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Settings;
using MediaPortal.Utilities;
using UPnP.Infrastructure.Dv.DeviceTree;

namespace MediaPortal.Backend.Services.ClientCommunication
{
  public class LocalizedUPnPDeviceInformation : ILocalizedDeviceInformation
  {
    public const string RES_DEFAULT_FRIENDLY_NAME = "[UPnPBackendServer.DefaultFriendlyName]";
    public const string RES_MANUFACTURER = "[UPnPBackendServer.Manufacturer]";
    public const string RES_MANUFACTURER_URL = "[UPnPBackendServer.ManufacturerUrl]";
    public const string RES_MODEL_DESCRIPTION = "[UPnPBackendServer.ModelDescription]";
    public const string RES_MODEL_NAME = "[UPnPBackendServer.ModelName]";
    public const string RES_MODEL_NUMBER = "[UPnPBackendServer.ModelNumber]";
    public const string RES_MODEL_URL = "[UPnPBackendServer.ModelUrl]";

    public string GetFriendlyName(CultureInfo culture)
    {
      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
      BackendServerSettings settings = settingsManager.Load<BackendServerSettings>();
      string result = settings.UPnPServerDeviceFriendlyName;
      if (string.IsNullOrEmpty(result))
      {
        result = StringUtils.TrimToNull(ServiceRegistration.Get<ILocalization>().ToString(RES_DEFAULT_FRIENDLY_NAME)) ??
            GetModelName(culture);
        settings.UPnPServerDeviceFriendlyName = result;
        settingsManager.Save(settings);
      }
      return result;
    }

    public string GetManufacturer(CultureInfo culture)
    {
      return StringUtils.TrimToNull(ServiceRegistration.Get<ILocalization>().ToString(RES_MANUFACTURER));
    }

    public string GetManufacturerURL(CultureInfo culture)
    {
      return StringUtils.TrimToNull(ServiceRegistration.Get<ILocalization>().ToString(RES_MANUFACTURER_URL));
    }

    public string GetModelDescription(CultureInfo culture)
    {
      return StringUtils.TrimToNull(ServiceRegistration.Get<ILocalization>().ToString(RES_MODEL_DESCRIPTION));
    }

    public string GetModelName(CultureInfo culture)
    {
      return StringUtils.TrimToNull(ServiceRegistration.Get<ILocalization>().ToString(RES_MODEL_NAME));
    }

    public string GetModelNumber(CultureInfo culture)
    {
      return StringUtils.TrimToNull(ServiceRegistration.Get<ILocalization>().ToString(RES_MODEL_NUMBER));
    }

    public string GetModelURL(CultureInfo culture)
    {
      return StringUtils.TrimToNull(ServiceRegistration.Get<ILocalization>().ToString(RES_MODEL_URL));
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
      return new List<IconDescriptor>();
    }
  }
}
