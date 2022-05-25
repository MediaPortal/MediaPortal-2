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

using HidInput.Inputs;
using InputDevices.Common.Inputs;
using InputDevices.Common.Mapping;
using InputDevices.Common.Messaging;
using System.Collections.Generic;

namespace HidInput.Devices
{
  public abstract class AbstractInputDevice
  {
    protected string _deviceId;
    protected InputCollection _inputCollection;
    protected InputDeviceMapping _defaultMapping;

    public AbstractInputDevice(string deviceId, IEnumerable<MappedAction> defaultMapping = null)
    {
      _deviceId = deviceId;
      _inputCollection = new InputCollection();

      if (defaultMapping != null)
        _defaultMapping = new InputDeviceMapping(deviceId, defaultMapping);
    }

    public string DeviceId 
    { 
      get { return _deviceId; } 
    }

    public void ResetInput()
    {
      _inputCollection.RemoveAll();
    }

    protected virtual bool BroadcastInputPressed(IList<Input> pressedInputs, IDictionary<string, object> additionalData = null)
    {
      return InputDeviceMessaging.BroadcastInputPressedMessage(_deviceId, pressedInputs, _defaultMapping, additionalData);
    }
  }
}
