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
using System;
using System.Net;
using System.Text;
using MediaPortal.Common;
using MediaPortal.Common.Configuration.ConfigurationClasses;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Services.ResourceAccess.Settings;
using MediaPortal.UI.Presentation.UiNotifications;

namespace MediaPortal.UiComponents.SkinBase.Settings.Configuration.General
{
  /// <summary>
  /// Configuration wrapper around the <see cref="ServerSettings.IPAddressBindings"/> setting.
  /// </summary>
  public class IPAddressBindingsSetting : MultipleEntryList
  {
    #region Public Methods

    public override int DisplayLength
    {
      get { return 40; }
    }

    public override int DisplayHeight
    {
      get { return 10; }
    }

    public override void Load()
    {
      _lines = SettingsManager.Load<ServerSettings>().IPAddressBindingsList;
    }

    public override void Save()
    {
      string newValue = null;
      if (_lines != null && _lines.Count > 0)
      {
        var sb = new StringBuilder();
        foreach (var line in _lines)
        {
          var ipAddress = line.Trim();
          IPAddress ip;
          if (!IPAddress.TryParse(ipAddress, out ip))
          {
            //TODO: make notification message localizable
            ServiceRegistration.Get<INotificationService>().EnqueueNotification(NotificationType.Error,
              ServiceRegistration.Get<ILocalization>().ToString("[Settings.General.Connectivity.IPAddressBindings]"),
              String.Format("The IP address '{0}' is invalid", ipAddress),
            true);
          }
          if (sb.Length > 0)
          {
            sb.Append(',');
          }
          sb.Append(ipAddress);
        }
        newValue = sb.ToString();
      }

      var settings = SettingsManager.Load<ServerSettings>();
      if (!String.Equals(newValue, settings.IPAddressBindings))
      {
        settings.IPAddressBindings = newValue;
        SettingsManager.Save(settings);
        //TODO: make notification texts localizable or handle rester notification differently
        ServiceRegistration.Get<INotificationService>().EnqueueNotification(NotificationType.UserInteractionRequired,
          "Restart required",
          "A restart of Media Portal is required before the modifications are accepted.",
          true);
      }
    }

    #endregion
  }
}
