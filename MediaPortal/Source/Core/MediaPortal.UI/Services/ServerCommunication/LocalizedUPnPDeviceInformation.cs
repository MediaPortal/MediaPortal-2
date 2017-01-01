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
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Settings;
using MediaPortal.UI.ServerCommunication.Settings;
using MediaPortal.Utilities;
using UPnP.Infrastructure.Dv.DeviceTree;

namespace MediaPortal.UI.Services.ServerCommunication
{
  public class LocalizedUPnPDeviceInformation : ILocalizedDeviceInformation
  {
    public const string RES_DEFAULT_FRIENDLY_NAME = "[UPnPFrontendServer.DefaultFriendlyName]";
    public const string RES_MANUFACTURER ="[UPnPFrontendServer.Manufacturer]";
    public const string RES_MANUFACTURER_URL ="[UPnPFrontendServer.ManufacturerUrl]";
    public const string RES_MODEL_DESCRIPTION ="[UPnPFrontendServer.ModelDescription]";
    public const string RES_MODEL_NAME ="[UPnPFrontendServer.ModelName]";
    public const string RES_MODEL_NUMBER ="[UPnPFrontendServer.ModelNumber]";
    public const string RES_MODEL_URL ="[UPnPFrontendServer.ModelUrl]";

    protected static readonly ICollection<IconDescriptor> EMPTY_ICON_COLLECTION = new List<IconDescriptor>();

    public string GetFriendlyName(CultureInfo culture)
    {
      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
      FrontendServerSettings settings = settingsManager.Load<FrontendServerSettings>();
      string result = settings.UPnPServerDeviceFriendlyName;
      if (string.IsNullOrEmpty(result))
      {
        result = StringUtils.TrimToNull(ServiceRegistration.Get<ILocalization>().ToString(
          RES_DEFAULT_FRIENDLY_NAME)) ?? GetModelName(culture);
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
      return EMPTY_ICON_COLLECTION;
    }
  }
}
