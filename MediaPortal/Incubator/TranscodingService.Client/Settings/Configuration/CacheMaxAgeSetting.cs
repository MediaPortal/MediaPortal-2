#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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
using MediaPortal.Extensions.TranscodingService.Interfaces.Settings;
using MediaPortal.Plugins.ServerSettings;
using MediaPortal.Plugins.ServerSettings.Settings;
using System;

namespace MediaPortal.Extensions.TranscodingService.Client.Settings.Configuration
{
  public class CacheMaxAgeSetting : LimitedNumberSelect, IDisposable
  {
    public CacheMaxAgeSetting()
    {
      Enabled = false;
      UpperLimit = 365;
      LowerLimit = 0;
      ValueType = NumberType.Integer;
      MaxNumDigits = 0;
      Step = 1;
      ConnectionMonitor.Instance.RegisterConfiguration(this);
    }

    public override void Load()
    {
      if (!Enabled)
        return;

      IServerSettingsClient serverSettings = ServiceRegistration.Get<IServerSettingsClient>();
      TranscodingServiceSettings settings = serverSettings.Load<TranscodingServiceSettings>();
      Value = settings.CacheMaximumAgeInDays;
    }

    public override void Save()
    {
      if (!Enabled)
        return;

      base.Save();

      IServerSettingsClient serverSettings = ServiceRegistration.Get<IServerSettingsClient>();
      TranscodingServiceSettings settings = serverSettings.Load<TranscodingServiceSettings>();
      settings.CacheMaximumAgeInDays = Convert.ToInt64(Value);
      serverSettings.Save(settings);
    }

    public void Dispose()
    {
      ConnectionMonitor.Instance.UnregisterConfiguration(this);
    }
  }
}
