#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

namespace MediaPortal.Plugins.InputDeviceManager.RawInput
{
    public class KeyPressEvent
    {
        public string DeviceName;       // i.e. \\?\HID#VID_045E&PID_00DD&MI_00#8&1eb402&0&0000#{884b96c3-56ef-11d1-bc8c-00a0c91405dd}
        public string DeviceType;       // KEYBOARD or HID
        public IntPtr DeviceHandle;     // Handle to the device that send the input
        public string Name;             // i.e. Microsoft USB Comfort Curve Keyboard 2000 (Mouse and Keyboard Center)
        private string _source;         // Keyboard_XX
        public int VKey;                // Virtual Key. Corrected for L/R keys(i.e. LSHIFT/RSHIFT) and Zoom
        public string VKeyName;         // Virtual Key Name. Corrected for L/R keys(i.e. LSHIFT/RSHIFT) and Zoom
        public uint Message;            // WM_KEYDOWN or WM_KEYUP        
        public string KeyPressState;    // MAKE or BREAK

        public string Source
        {
            get { return _source; }
            set { _source = string.Format("Keyboard_{0}", value.PadLeft(2, '0')); }
        }

        public override string ToString()
        {
            return string.Format("Device\n DeviceName: {0}\n DeviceType: {1}\n DeviceHandle: {2}\n Name: {3}\n", DeviceName, DeviceType, DeviceHandle.ToInt64().ToString("X"), Name);
        }
    }
}
                                         

