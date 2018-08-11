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
using MediaPortal.Common.Localization;
using MediaPortal.Extensions.TranscodingService.Interfaces.Settings;
using MediaPortal.Plugins.ServerSettings;
using MediaPortal.Plugins.ServerSettings.Settings;
using System;
using System.Collections.Generic;

namespace MediaPortal.Extensions.TranscodingService.Client.Settings.Configuration
{
  public class HardwareAccelerationSetting : SingleSelectionList, IDisposable
  {
    private readonly List<HWAcceleration> _accellerators = new List<HWAcceleration>()
    {
      HWAcceleration.None,
      HWAcceleration.Auto,
      HWAcceleration.DirectX11,
      HWAcceleration.DXVA2,
      HWAcceleration.Intel,
      HWAcceleration.Nvidia,
      //HWAccelleration.Amd
    };
    private const string RES_NONE = "[Settings.None]";
    private const string RES_AUTO = "[Settings.Auto]";

    public HardwareAccelerationSetting()
    {
      Enabled = false;
      ConnectionMonitor.Instance.RegisterConfiguration(this);
    }

    public override void Load()
    {
      if (!Enabled)
        return;

      IServerSettingsClient serverSettings = ServiceRegistration.Get<IServerSettingsClient>();
      TranscodingServiceSettings settings = serverSettings.Load<TranscodingServiceSettings>();

      _items = new List<IResourceString>(_accellerators.Count);
      for (int i = 0; i < _accellerators.Count; i++)
      {
        if(_accellerators[i] == HWAcceleration.None)
          _items.Add(LocalizationHelper.CreateResourceString(RES_NONE));
        else if (_accellerators[i] == HWAcceleration.Auto)
          _items.Add(LocalizationHelper.CreateResourceString(RES_AUTO));
        else if (_accellerators[i] == HWAcceleration.DirectX11)
          _items.Add(LocalizationHelper.CreateStaticString("DirectX 11"));
        else if (_accellerators[i] == HWAcceleration.DXVA2)
          _items.Add(LocalizationHelper.CreateStaticString("DXVA 2"));
        else if (_accellerators[i] == HWAcceleration.Intel)
          _items.Add(LocalizationHelper.CreateStaticString("Intel"));
        else if (_accellerators[i] == HWAcceleration.Nvidia)
          _items.Add(LocalizationHelper.CreateStaticString("Nvidia"));
        else if (_accellerators[i] == HWAcceleration.Amd)
          _items.Add(LocalizationHelper.CreateStaticString("AMD"));

        if (_accellerators[i] == settings.HardwareAcceleration)
          Selected = i;
      }     
    }

    public override void Save()
    {
      if (!Enabled)
        return;

      base.Save();

      IServerSettingsClient serverSettings = ServiceRegistration.Get<IServerSettingsClient>();
      TranscodingServiceSettings settings = serverSettings.Load<TranscodingServiceSettings>();
      settings.HardwareAcceleration = _accellerators[Selected];
      serverSettings.Save(settings);
    }

    public void Dispose()
    {
      ConnectionMonitor.Instance.UnregisterConfiguration(this);
    }
  }
}
