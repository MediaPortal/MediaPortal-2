#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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

using InputDevices.Common.Mapping;
using InputDevices.Settings;
using MediaPortal.Common;
using MediaPortal.Common.Services.Settings;
using MediaPortal.Common.Settings;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace InputDevices.Mapping
{
  public class DeviceMappingWatcher : IDisposable
  {
    protected SettingsChangeWatcher<InputDeviceSettings> _settingsWatcher;
    protected ConcurrentDictionary<string, InputDeviceMapping> _inputDeviceMappings;
    protected bool _updateOnMappingsChanged = true;

    public DeviceMappingWatcher()
    {
      _inputDeviceMappings = new ConcurrentDictionary<string, InputDeviceMapping>();
      InitSettings();
    }

    public bool TryGetMapping(string deviceId, out InputDeviceMapping mapping)
    {
      if (deviceId != null)
        return _inputDeviceMappings.TryGetValue(deviceId, out mapping);

      mapping = null;
      return false;
    }

    public void AddOrUpdateMapping(InputDeviceMapping mapping)
    {
      _inputDeviceMappings[mapping.DeviceId] = mapping;
      SaveMappings();
    }

    protected void InitSettings()
    {
      if (_settingsWatcher != null)
        return;

      _settingsWatcher = new SettingsChangeWatcher<InputDeviceSettings>(true);
      UpdateMappings(_settingsWatcher.Settings.Mappings);
      _settingsWatcher.SettingsChanged += (s, e) => UpdateMappings(_settingsWatcher.Settings.Mappings);
    }

    protected void UpdateMappings(ICollection<InputDeviceMapping> mappings)
    {
      if (!_updateOnMappingsChanged)
      {
        _updateOnMappingsChanged = true;
        return;
      }
      ICollection<InputDeviceMapping> mappingsToRemove = _inputDeviceMappings.Values.Where(existingMapping => !mappings.Any(m=>m.DeviceId == existingMapping.DeviceId)).ToList();
      foreach (InputDeviceMapping mapping in mappings)
        _inputDeviceMappings[mapping.DeviceId] = mapping;
      foreach (InputDeviceMapping mapping in mappingsToRemove)
        _inputDeviceMappings.TryRemove(mapping.DeviceId, out _);
    }

    protected void SaveMappings()
    {
      InputDeviceSettings settings = _settingsWatcher.Settings;
      settings.Mappings = new List<InputDeviceMapping>(_inputDeviceMappings.Values);
      _updateOnMappingsChanged = false;
      ServiceRegistration.Get<ISettingsManager>().Save(settings);
    }

    public void Dispose()
    {
      if (_settingsWatcher != null)
      {
        _settingsWatcher.Dispose();
        _settingsWatcher = null;
      }
    }
  }
}
