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
using InputDevices.Common.Mapping;
using SharpLib.Hid;
using SharpLib.Hid.Usage;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace HidInput.Devices
{
  public class HidInputDevice : AbstractInputDevice
  {
    public static string GetDeviceId(Device device)
    {
      return GetDeviceId(device?.VendorId, device?.ProductId);
    }

    public static string GetDeviceId(ushort? vendorId, ushort? productId)
    {
      return "HidDevice:" + (vendorId?.ToString() ?? string.Empty) + ":" + (productId?.ToString() ?? string.Empty);
    }

    protected string _deviceName;
    protected HashSet<Keys> _expectedKeyDowns = new HashSet<Keys>();
    protected HashSet<Keys> _expectedKeyUps = new HashSet<Keys>();

    public HidInputDevice(Device device, IEnumerable<MappedAction> defaultMapping = null)
      : this(GetDeviceId(device), device.Name, device.FriendlyName, defaultMapping)
    { }

    public HidInputDevice(string deviceId, string deviceName, string friendlyName, IEnumerable<MappedAction> defaultMapping = null)
    : base(new DeviceMetadata(deviceId, friendlyName, defaultMapping))
    {
      _deviceName = deviceName;
    }

    public string DeviceName
    { 
      get { return _deviceName; } 
    }

    /// <summary>
    /// Tries to handle the usages contained in the <paramref name="hidEvent"/>.
    /// </summary>
    /// <param name="hidEvent">The event containing the usages.</param>
    /// <returns><c>true</c> if the usages were handled; else <c>false</c>.</returns>
    public bool HandleHidEvent(Event hidEvent)
    {
      if (GetDeviceId(hidEvent.Device) != _metadata.Id)
        return false;

      bool hasInputChanged = false;

      if (hidEvent.IsKeyboard)
        return HandleKeyboardEvent(hidEvent);

      if (hidEvent.IsMouse)
        hasInputChanged |= MouseInput.TryDecodeEvent(hidEvent, _inputCollection);

      if (hidEvent.IsGeneric)
      {
        if (hidEvent.Device?.IsGamePad == true)
          hasInputChanged |= GamePadInput.TryDecodeEvent(hidEvent, _inputCollection);
        else
        {
          // If this is a Consumer usage then we might not handle it directly here and instead will handle
          // the subsequent keyboard event that the system automatically generates for some consumer usages to take advantage
          // of the system's keyboard repeat handling. This method will keep track of which subsequent keyboard events should
          // be handled and filter out the usages that generate them.
          UpdateKeyboardEventsToHandleForConsumerUsages(hidEvent, out IEnumerable<ushort> unhandledUsages);
          hasInputChanged |= GenericInput.TryDecodeEvent(hidEvent, _inputCollection, unhandledUsages);
        }
      }

      if (!hasInputChanged)
        return false;

      IList<Input> currentInputs = _inputCollection.CurrentInputs;
      return hasInputChanged && (currentInputs.Count == 0 || BroadcastInputPressed(currentInputs));
    }

    /// <summary>
    /// Tries to handle any keyboard usages contained in the <paramref name="hidEvent"/>, this will handle events that are generated directly by this device or events with no device
    /// but which are expected by this device because a <see cref="ConsumerControl"/> that automatically generates the keyboard event is currently pressed on this device.
    /// </summary>
    /// <param name="hidEvent">The event containing the keyboard usage.</param>
    /// <returns><c>true</c> if this device handled the keyboard event; else <c>false</c>.</returns>
    public bool HandleKeyboardEvent(Event hidEvent)
    {      
      if (!hidEvent.IsKeyboard)
        return false;
      // Check if this keyboard event was generated in response to a consumer usage being pressed on this device, i.e. the key is contained in our expected keyboard events, the generated
      // keyboard event might not have any device associated with it if Windows automatically generated the event. For key up events, additionally remove the key from the expected key up
      // events (key downs may get repeated and will only be removed when the associated consumer usage is released).
      // If the key is not expected but the device id matches this device then it is a keyboard event generated directly by the device (rather than by Windows in response to a Consumer Control usage)
      // so it should also be handled.
      if ((hidEvent.IsButtonDown && _expectedKeyDowns.Contains(hidEvent.VirtualKey)) || (hidEvent.IsButtonUp && _expectedKeyUps.Remove(hidEvent.VirtualKey)) || GetDeviceId(hidEvent.Device) == _metadata.Id)
        KeyboardInput.TryDecodeEvent(hidEvent, _inputCollection);
      else
        return false;

      IList<Input> currentInputs = _inputCollection.CurrentInputs;
      return currentInputs.Count == 0 || BroadcastInputPressed(currentInputs);
    }

    /// <summary>
    /// Updates the keyboard events that should be handled because a <see cref="ConsumerControl"/> usage that the system automatically converts to a keyboard event was pressed.
    /// <paramref name="unhandledUsages"/> will contain any usages that do not generate keyboard events so they can be handled directly. Usages that generate keyboard events will
    /// not be handled directly, instead the keyboard events will be handled by a subsequent call to <see cref="HandleKeyboardEvent(Event)"/> when the keyboard event is generated
    /// to take advantage of the system's keyboard repeat handling.
    /// </summary>
    /// <param name="hidEvent">The event that may contain consumer usages that generate keyboard events.</param>
    /// <param name="unhandledUsages">Any usages that do not generate keyboard events and should therefore be handled directly.</param>
    protected void UpdateKeyboardEventsToHandleForConsumerUsages(Event hidEvent, out IEnumerable<ushort> unhandledUsages)
    {
      // Only Consumer usages will be handled here
      if (hidEvent.UsagePageEnum != UsagePage.Consumer)
      {
        unhandledUsages = hidEvent.Usages;
        return;
      }

      // Create a new set of key down events to handle based on the currently pressed consumer usages
      HashSet<Keys> expectedKeyDowns = new HashSet<Keys>();
      // Get the usages that generate a keyboard event
      IEnumerable<ushort> handledUsages = hidEvent.Usages.Where(u => _consumerControlKeys.ContainsKey((ConsumerControl)u));
      // All other usages will not be handled here
      unhandledUsages = hidEvent.Usages.Except(handledUsages);
      // Add the corresponding keys to the set of key downs to handle
      foreach (ushort usage in handledUsages)
        expectedKeyDowns.Add(_consumerControlKeys[(ConsumerControl)usage]);

      // Handle a key up event for any previously handled key downs
      _expectedKeyUps = _expectedKeyDowns;
      _expectedKeyUps.ExceptWith(expectedKeyDowns);
      // Remember the new expected key downs
      _expectedKeyDowns = expectedKeyDowns;
    }

    /// <summary>
    /// Map of <see cref="ConsumerControl"/> usages to system generated keyboard <see cref="Keys"/>.
    /// </summary>
    /// <remarks>
    /// Windows generates additional WM_INPUT keyboard events for these <see cref="ConsumerControl"/> usages, e.g. if <see cref="ConsumerControl.ScanNextTrack"/>
    /// is pressed on a device, then first a WM_INPUT message for that usage will be received, then another WM_INPUT key down message will be received
    /// for the keyboard key <see cref="Keys.MediaNextTrack"/> then when <see cref="ConsumerControl.ScanNextTrack"/> is released there will be a WM_INPUT message with an
    /// empty <see cref="ConsumerControl"/> usage followed by a WM_INPUT key up message for <see cref="Keys.MediaNextTrack"/>.
    /// The full list of <see cref="ConsumerControl"/> usages that get converted to keyboard inputs can be found in the table here
    /// https://download.microsoft.com/download/1/6/1/161ba512-40e2-4cc9-843a-923143f3456c/translate.pdf at the bottom of page 3, any
    /// usage with a HID usage of 0x0C is a <see cref="ConsumerControl"/> that gets converted to keyboard input.
    /// </remarks>
    protected static readonly Dictionary<ConsumerControl, Keys> _consumerControlKeys = new Dictionary<ConsumerControl, Keys>
    {
      { ConsumerControl.ScanNextTrack, Keys.MediaNextTrack},
      { ConsumerControl.ScanPreviousTrack, Keys.MediaPreviousTrack },
      { ConsumerControl.Stop, Keys.MediaStop },
      { ConsumerControl.PlayPause, Keys.MediaPlayPause },
      { ConsumerControl.Mute, Keys.VolumeMute },
      { ConsumerControl.VolumeIncrement, Keys.VolumeUp },
      { ConsumerControl.VolumeDecrement, Keys.VolumeDown },
      { ConsumerControl.AppLaunchEmailReader, Keys.LaunchMail },
      { ConsumerControl.AppCtrlSearch, Keys.BrowserSearch },
      { ConsumerControl.AppCtrlHome, Keys.BrowserHome },
      { ConsumerControl.AppCtrlBack, Keys.BrowserBack },
      { ConsumerControl.AppCtrlForward, Keys.BrowserForward },
      { ConsumerControl.AppCtrlStop, Keys.BrowserStop },
      { ConsumerControl.AppCtrlRefresh, Keys.BrowserRefresh },
      { ConsumerControl.AppCtrlBookmarks, Keys.BrowserFavorites },
    };
  }
}
