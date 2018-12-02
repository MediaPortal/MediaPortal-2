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

using MediaPortal.Common;
using MediaPortal.Common.Configuration.ConfigurationClasses;
using MediaPortal.Common.Services.ResourceAccess.Settings;
using MediaPortal.UI.Presentation.UiNotifications;

namespace MediaPortal.UiComponents.SkinBase.Settings.Configuration.General
{
  /// <summary>
  /// Configuration wrapper around the <see cref="ServerSettings.HttpServerPort"/> setting.
  /// </summary>
  public class HttpServerPortSetting : LimitedNumberSelect
  {
    #region Base overrides

    public override void Load()
    {
      _type = NumberType.Integer;
      _step = 1;
      _lowerLimit = 0;
      _upperLimit = 65535;
      _value = SettingsManager.Load<ServerSettings>().HttpServerPort;
    }

    public override void Save()
    {
      var settings = SettingsManager.Load<ServerSettings>();
      var value = (int)_value;
      if (value != settings.HttpServerPort)
      {
        settings.HttpServerPort = value;
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
