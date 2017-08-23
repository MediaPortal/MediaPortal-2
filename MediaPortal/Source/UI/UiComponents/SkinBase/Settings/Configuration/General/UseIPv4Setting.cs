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

using MediaPortal.Common;
using MediaPortal.Common.Configuration.ConfigurationClasses;
using MediaPortal.Common.Services.ResourceAccess.Settings;
using MediaPortal.UI.Presentation.UiNotifications;

namespace MediaPortal.UiComponents.SkinBase.Settings.Configuration.General
{
  /// <summary>
  /// Configuration wrapper around the <see cref="ServerSettings.UseIPv4"/> setting.
  /// </summary>
  public class UseIPv4Setting : YesNo
  {
    #region Public Methods

    public override void Load()
    {
      _yes = SettingsManager.Load<ServerSettings>().UseIPv4;
    }

    public override void Save()
    {
      var settings = SettingsManager.Load<ServerSettings>();
      if (_yes != settings.UseIPv4)
      {
        settings.UseIPv4 = _yes;
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
