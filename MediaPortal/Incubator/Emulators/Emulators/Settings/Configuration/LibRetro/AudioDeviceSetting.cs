using Emulators.LibRetro.Settings;
using MediaPortal.Common.Configuration.ConfigurationClasses;
using MediaPortal.Common.Localization;
using SharpDX.DirectSound;
using System;
using System.Collections.Generic;

namespace Emulators.Settings.Configuration.LibRetro
{
  public class AudioDeviceSetting : SingleSelectionList
  {
    protected List<DeviceInformation> _devices;
    public override void Load()
    {
      Guid currentDeviceId = SettingsManager.Load<LibRetroSettings>().AudioDeviceId;
      _devices = DirectSound.GetDevices();
      _items = new List<IResourceString>(_devices.Count);
      for (int i = 0; i < _devices.Count; i++)
      {
        DeviceInformation device = _devices[i];
        _items.Add(LocalizationHelper.CreateStaticString(device.Description));
        if (device.DriverGuid == currentDeviceId)
          Selected = i;
      }
    }

    public override void Save()
    {
      LibRetroSettings settings = SettingsManager.Load<LibRetroSettings>();
      settings.AudioDeviceId = _devices[Selected].DriverGuid;
      SettingsManager.Save(settings);
    }
  }
}
