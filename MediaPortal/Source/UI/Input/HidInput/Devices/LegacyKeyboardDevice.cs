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
using InputDevices.Common.Devices;
using InputDevices.Common.Inputs;
using System.Collections.Generic;
using System.Windows.Forms;

namespace HidInput.Devices
{
  public class LegacyKeyboardDevice : AbstractInputDevice
  {
    const string DEVICE_ID = "LegacyKeyboardDevice";
    const string FRIENDLY_NAME = "Keyboard";

    public LegacyKeyboardDevice()
      : base(new DeviceMetadata(DEVICE_ID, FRIENDLY_NAME))
    { }

    public bool HandleKeyEvent(ref Message message)
    {
      bool hasInputChanged = KeyboardInput.TryDecodeMessage(ref message, _inputCollection);
      if(!hasInputChanged)
        return false;
      IList<Input> currentInputs = _inputCollection.CurrentInputs;
      if (currentInputs.Count > 0)
        return BroadcastInputPressed(currentInputs);
      return true;
    }
  }
}
