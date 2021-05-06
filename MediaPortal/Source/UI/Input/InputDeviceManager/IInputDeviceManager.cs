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

using System;
using System.Collections.Generic;

namespace MediaPortal.Plugins.InputDeviceManager
{
  public delegate bool ExternalKeyPressHandler(object sender, KeyPressHandlerEventArgs e);

  public class KeyPressHandlerEventArgs : EventArgs
  {
    public KeyPressHandlerEventArgs(string deviceName, string deviceFriendlyName, string deviceId, IDictionary<string, long> pressedKeys, bool isRepeat)
    {
      DeviceName = deviceName;
      DeviceFriendlyName = deviceFriendlyName;
      DeviceId = deviceId;
      PressedKeys = pressedKeys;
      IsRepeat = isRepeat;
    }

    public string DeviceName { get; protected set; }
    public string DeviceFriendlyName { get; protected set; }
    public string DeviceId { get; protected set; }
    public IDictionary<string, long> PressedKeys { get; protected set; }
    public bool IsRepeat { get; protected set; }
  }

  public interface IInputDeviceManager
  {
    /// <summary>
    /// Gets a dictionary of known input devices by type.
    /// </summary>
    IDictionary<string, InputDevice> InputDevices { get; }

    /// <summary>
    /// Registers an external key handler that will be called instead of any mapped keys/actions.
    /// </summary>
    /// <param name="keyPressHandler">Key press handler to call on a HID event.</param>
    /// <returns><c>True</c> if the key handler was added.</returns>
    bool RegisterExternalKeyHandling(ExternalKeyPressHandler keyPressHandler);

    /// <summary>
    /// Unregisters an external key handler previously added with a call to <see cref="RegisterExternalKeyHandling"/>.
    /// </summary>
    /// <param name="keyPressHandler">Key press handler to remove.</param>
    /// <returns><c>True</c> if the key handler was removed.</returns>
    bool UnRegisterExternalKeyHandling(ExternalKeyPressHandler keyPressHandler);

    /// <summary>
    /// Updates the inpute device configuration using the specified <paramref name="settings"/>.
    /// </summary>
    /// <param name="settings">The settings to use to update the input device configuration.</param>
    void UpdateLoadedSettings(InputManagerSettings settings);
  }
}
