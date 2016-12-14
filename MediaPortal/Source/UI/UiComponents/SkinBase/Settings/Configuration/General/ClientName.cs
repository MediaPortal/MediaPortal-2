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
using MediaPortal.UI.FrontendServer;
using MediaPortal.UI.ServerCommunication.Settings;

namespace MediaPortal.UiComponents.SkinBase.Settings.Configuration.General
{
  public class ClientName : Entry
  {
    #region Base overrides

    public override void Load()
    {
      _value = SettingsManager.Load<FrontendServerSettings>().UPnPServerDeviceFriendlyName;
    }

    public override void Save()
    {
      FrontendServerSettings settings = SettingsManager.Load<FrontendServerSettings>();
      settings.UPnPServerDeviceFriendlyName = _value;
      SettingsManager.Save(settings);
      ServiceRegistration.Get<IFrontendServer>().UpdateUPnPConfiguration();
    }

    public override int DisplayLength
    {
      get { return 20; }
    }

    #endregion
  }
}
