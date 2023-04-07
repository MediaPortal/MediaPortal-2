using InputDevices.Common.Mapping;
using System;
using System.Collections.Generic;

namespace InputDevices.Common.Devices
{
  public class DeviceMetadata
  {
    public DeviceMetadata(string id, string friendlyName, IEnumerable<MappedAction> defaultMapping = null)
    {
      Id = id ?? throw new ArgumentNullException(nameof(id));
      FriendlyName = friendlyName;
      if (defaultMapping != null)
        DefaultMapping = new InputDeviceMapping(id, defaultMapping);
    }

    public string Id { get; }
    public string FriendlyName { get; }
    public InputDeviceMapping DefaultMapping { get; }
  }
}
