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
using System.Diagnostics;
using System.Globalization;
using System.ComponentModel;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace MediaPortal.Plugins.InputDeviceManager.RawInput
{
	public sealed class RawKeyboard
	{
		private readonly Dictionary<IntPtr,KeyPressEvent> _deviceList = new Dictionary<IntPtr,KeyPressEvent>();
		public delegate void DeviceEventHandler(object sender, RawInputEventArg e);
		public event DeviceEventHandler KeyPressed;
		readonly object _padLock = new object();
		public int NumberOfKeyboards { get; private set; }
		static InputData _rawBuffer;

		public RawKeyboard(IntPtr hwnd, bool captureOnlyInForeground)
		{
			var rid = new RawInputDevice[1];

			rid[0].UsagePage = HidUsagePage.GENERIC;       
			rid[0].Usage = HidUsage.Keyboard;              
            rid[0].Flags = (captureOnlyInForeground ? RawInputDeviceFlags.NONE : RawInputDeviceFlags.INPUTSINK) | RawInputDeviceFlags.DEVNOTIFY;
			rid[0].Target = hwnd;

			if(!Win32.RegisterRawInputDevices(rid, (uint)rid.Length, (uint)Marshal.SizeOf(rid[0])))
			{
				throw new ApplicationException("Failed to register raw input device(s).");
			}
		}

		public void EnumerateDevices()
		{
			lock (_padLock)
			{
				_deviceList.Clear();

				var keyboardNumber = 0;

				var globalDevice = new KeyPressEvent
				{
					DeviceName = "Global Keyboard",
					DeviceHandle = IntPtr.Zero,
					DeviceType = Win32.GetDeviceType(DeviceType.RimTypekeyboard),
					Name = "Fake Keyboard. Some keys (ZOOM, MUTE, VOLUMEUP, VOLUMEDOWN) are sent to rawinput with a handle of zero.",
					Source = keyboardNumber++.ToString(CultureInfo.InvariantCulture)
				};

				_deviceList.Add(globalDevice.DeviceHandle, globalDevice);
				
				var numberOfDevices = 0;
				uint deviceCount = 0;
				var dwSize = (Marshal.SizeOf(typeof(Rawinputdevicelist)));

				if (Win32.GetRawInputDeviceList(IntPtr.Zero, ref deviceCount, (uint)dwSize) == 0)
				{
					var pRawInputDeviceList = Marshal.AllocHGlobal((int)(dwSize * deviceCount));
					Win32.GetRawInputDeviceList(pRawInputDeviceList, ref deviceCount, (uint)dwSize);

					for (var i = 0; i < deviceCount; i++)
					{
						uint pcbSize = 0;

						// On Window 8 64bit when compiling against .Net > 3.5 using .ToInt32 you will generate an arithmetic overflow. Leave as it is for 32bit/64bit applications
						var rid = (Rawinputdevicelist)Marshal.PtrToStructure(new IntPtr((pRawInputDeviceList.ToInt64() + (dwSize * i))), typeof(Rawinputdevicelist));

						Win32.GetRawInputDeviceInfo(rid.hDevice, RawInputDeviceInfo.RIDI_DEVICENAME, IntPtr.Zero, ref pcbSize);

						if (pcbSize <= 0) continue;

						var pData = Marshal.AllocHGlobal((int)pcbSize);
						Win32.GetRawInputDeviceInfo(rid.hDevice, RawInputDeviceInfo.RIDI_DEVICENAME, pData, ref pcbSize);
						var deviceName = Marshal.PtrToStringAnsi(pData);

                        if (rid.dwType == DeviceType.RimTypekeyboard || rid.dwType == DeviceType.RimTypeHid)
						{
							var deviceDesc = Win32.GetDeviceDescription(deviceName);

							var dInfo = new KeyPressEvent
							{
								DeviceName = Marshal.PtrToStringAnsi(pData),
								DeviceHandle = rid.hDevice,
								DeviceType = Win32.GetDeviceType(rid.dwType),
								Name = deviceDesc,
								Source = keyboardNumber++.ToString(CultureInfo.InvariantCulture)
							};
						   
							if (!_deviceList.ContainsKey(rid.hDevice))
							{
								numberOfDevices++;
								_deviceList.Add(rid.hDevice, dInfo);
							}
						}

						Marshal.FreeHGlobal(pData);
					}

					Marshal.FreeHGlobal(pRawInputDeviceList);

					NumberOfKeyboards = numberOfDevices;
					Debug.WriteLine("EnumerateDevices() found {0} Keyboard(s)", NumberOfKeyboards);
					return;
				}
			}
			
			throw new Win32Exception(Marshal.GetLastWin32Error());
		}
	   
		public bool ProcessRawInput(IntPtr hdevice)
		{
			//Debug.WriteLine(_rawBuffer.data.keyboard.ToString());
			//Debug.WriteLine(_rawBuffer.data.hid.ToString());
			//Debug.WriteLine(_rawBuffer.header.ToString());

			if (_deviceList.Count == 0) return false;

			var dwSize = 0;
			Win32.GetRawInputData(hdevice, DataCommand.RID_INPUT, IntPtr.Zero, ref dwSize, Marshal.SizeOf(typeof(Rawinputheader)));

			if (dwSize != Win32.GetRawInputData(hdevice, DataCommand.RID_INPUT, out _rawBuffer, ref dwSize, Marshal.SizeOf(typeof (Rawinputheader))))
			{
				Debug.WriteLine("Error getting the rawinput buffer");
            return false;
			}

			int virtualKey = _rawBuffer.data.keyboard.VKey;
			int makeCode = _rawBuffer.data.keyboard.Makecode;
			int flags = _rawBuffer.data.keyboard.Flags;

			if (virtualKey == Win32.KEYBOARD_OVERRUN_MAKE_CODE) return false; 
			
			var isE0BitSet = ((flags & Win32.RI_KEY_E0) != 0);

			KeyPressEvent keyPressEvent;

			if (_deviceList.ContainsKey(_rawBuffer.header.hDevice))
			{
				lock (_padLock)
				{
					keyPressEvent = _deviceList[_rawBuffer.header.hDevice];
				}
			}
			else
			{
				Debug.WriteLine("Handle: {0} was not in the device list.", _rawBuffer.header.hDevice);
				return false;
			}

			var isBreakBitSet = ((flags & Win32.RI_KEY_BREAK) != 0);
			
			keyPressEvent.KeyPressState = isBreakBitSet ? "BREAK" : "MAKE"; 
			keyPressEvent.Message = _rawBuffer.data.keyboard.Message;
			keyPressEvent.VKeyName = KeyMapper.GetKeyName(VirtualKeyCorrection(virtualKey, isE0BitSet, makeCode)).ToUpper();
			keyPressEvent.VKey = virtualKey;
		   
			if (KeyPressed != null)
			{
			   var e = new RawInputEventArg(keyPressEvent);
            KeyPressed(this, e);
			   return e.Handled;
			}
		   return false;
		}

		private static int VirtualKeyCorrection(int virtualKey, bool isE0BitSet, int makeCode)
		{
			var correctedVKey = virtualKey;

			if (_rawBuffer.header.hDevice == IntPtr.Zero)
			{
				// When hDevice is 0 and the vkey is VK_CONTROL indicates the ZOOM key
				if (_rawBuffer.data.keyboard.VKey == Win32.VK_CONTROL)
				{
					correctedVKey = Win32.VK_ZOOM;
				}
			}
			else
			{
				switch (virtualKey)
				{
					// Right-hand CTRL and ALT have their e0 bit set 
					case Win32.VK_CONTROL:
						correctedVKey = isE0BitSet ? Win32.VK_RCONTROL : Win32.VK_LCONTROL;
						break;
					case Win32.VK_MENU:
						correctedVKey = isE0BitSet ? Win32.VK_RMENU : Win32.VK_LMENU;
						break;
					case Win32.VK_SHIFT:
						correctedVKey = makeCode == Win32.SC_SHIFT_R ? Win32.VK_RSHIFT : Win32.VK_LSHIFT;
						break;
					default:
						correctedVKey = virtualKey;
						break;
				}
			}

			return correctedVKey;
		}
	}
}
