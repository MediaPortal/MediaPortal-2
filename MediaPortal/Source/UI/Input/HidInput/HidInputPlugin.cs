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

using HidInput.DefaultMappings;
using HidInput.Devices;
using HidInput.Hid;
using HidInput.Messaging;
using HidInput.Settings;
using HidInput.Utils;
using HidInput.Windows;
using InputDevices.Common.Mapping;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.Services.Settings;
using MediaPortal.UI.General;
using MediaPortal.UI.SkinEngine.SkinManagement;
using SharpLib.Hid;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace HidInput
{
  public class HidInputPlugin : IPluginStateTracker
  {
    protected readonly object _syncObj = new object(); 
    protected HidManager _hidManager;
    protected IDictionary<string, HidInputDevice> _hidDevices;
    protected LegacyKeyboardDevice _legacyKeyboard;
    protected MessageFilter _messageFilter = new MessageFilter();

    protected SynchronousMessageQueue _messageQueue;
    protected SettingsChangeWatcher<HidInputSettings> _settingsWatcher;

    protected bool _canActivate = true;

    public void Activated(PluginRuntime pluginRuntime)
    {
      _hidDevices = new Dictionary<string, HidInputDevice>();
      _legacyKeyboard = new LegacyKeyboardDevice();
      _messageFilter = new MessageFilter();

      // A new hidden window is created by the HidManager to listen for HID messages, this
      // should be done on the same thread as the main window to ensure that
      // both windows' message loops are synchronized and we don't create
      // a message loop on a random thread.
      if (SkinContext.Form != null && SkinContext.Form.InvokeRequired)
        SkinContext.Form.BeginInvoke(new Action(Init));
      // If autoactivating when the PluginManager starts up the main window might not
      // have been created yet but currently this always happens on the main thread so
      // it should be OK to Init directly. This could break if the PluginManager is
      // modified to do a multithreaded/async startup
      else
        Init();
    }

    public void Continue()
    {
    }

    public bool RequestEnd()
    {
      return true;
    }

    public void Stop()
    {
      lock (_syncObj)
        _canActivate = false;
      _messageQueue?.Dispose();
      _settingsWatcher?.Dispose();
      _hidManager?.Dispose();
    }

    public void Shutdown()
    {
      Stop();
    }

    protected void Init()
    {
      lock (_syncObj)
      {
        if (!_canActivate)
          return;
        _hidManager = new HidManager();
        InitSettings();
        InitMessageQueue();
      }
    }

    protected void InitMessageQueue()
    {
      _messageQueue = new SynchronousMessageQueue(this, new[] { WindowsMessaging.CHANNEL, HidMessaging.CHANNEL });
      _messageQueue.MessagesAvailable += MessagesAvailable;
      _messageQueue.RegisterAtAllMessageChannels();
    }

    private void MessagesAvailable(SynchronousMessageQueue queue)
    {
      SystemMessage message;
      while ((message = queue.Dequeue()) != null)
      {
        if (message.ChannelName == WindowsMessaging.CHANNEL)
          HandleWindowsMessage(message);
        else if (message.ChannelName == HidMessaging.CHANNEL)
          HandleHidMessage(message);
      }
    }

    protected void HandleWindowsMessage(SystemMessage message)
    {
      WindowsMessaging.MessageType messageType = (WindowsMessaging.MessageType)message.MessageType;
      if (messageType == WindowsMessaging.MessageType.WindowsBroadcast)
      {
        Message msg = (Message)message.MessageData[WindowsMessaging.MESSAGE];

        bool handled = (bool?)message.MessageData[WindowsMessaging.HANDLED] == true;
        if (handled)
          return;
        // See if this is a WM_KEY[DOWN/UP] message that should be marked as handled because
        // the key was already handled in a HID message or our legacy keyboard handles it
        handled = _messageFilter.ShouldFilter(ref msg, out bool modified) || _legacyKeyboard.HandleKeyEvent(ref msg);
        if (handled)
          message.MessageData[WindowsMessaging.HANDLED] = handled;
        // A message might get modified by the call to ShouldFilter above by reducing the repeat count in a key down message.
        // Due to [un]boxing the modified message needs to be copied back into the message data.
        if (modified)
          message.MessageData[WindowsMessaging.MESSAGE] = msg;
      }
    }

    protected void HandleHidMessage(SystemMessage message)
    {
      HidMessaging.MessageType messageType = (HidMessaging.MessageType)message.MessageType;
      if (messageType == HidMessaging.MessageType.HidEvent)
      {
        Event hidEvent = (Event)message.MessageData[HidMessaging.EVENT];

        // This message has been handled by another plugin don't handle again but
        // update our message filter to ignore any subsequent keyboard messages
        // that might be automatically generated by this event.
        if ((bool?)message.MessageData[HidMessaging.HANDLED] == true)
          _messageFilter.Filter(hidEvent);

        // If the current focus is on a text input control don't handle the
        // message so that the raw characters are passed through.
        // This should probably be smarter to only allow printable keys through.
        else if (hidEvent.IsKeyboard && TextInputUtils.DoesCurrentFocusedControlNeedTextInput())
          return;

        // Event not yet handled, see if any message subscribers will handle it
        else if (HandleHidEvent(hidEvent))
        {
          _messageFilter.Filter(hidEvent);
          message.MessageData[HidMessaging.HANDLED] = true;
        }
      }
      else if (messageType == HidMessaging.MessageType.DeviceArrived || messageType == HidMessaging.MessageType.DeviceRemoved)
      {
        HandleDeviceRemoval((string)message.MessageData[HidMessaging.DEVICE_NAME]);
      }
      // If the window was [de]activated some input messages may have been missed, particularly
      // key up messages, so reset all input to get back to a known state
      else if (messageType == HidMessaging.MessageType.Deactivated || messageType == HidMessaging.MessageType.Activated)
      {
        ResetInput();
      }
    }

    protected void InitSettings()
    {
      _settingsWatcher = new SettingsChangeWatcher<HidInputSettings>(true);
      _settingsWatcher.SettingsChanged = SettingsChanged;
    }

    private void SettingsChanged(object sender, EventArgs e)
    {
    }

    protected bool HandleHidEvent(Event e)
    {
      // Windows generates keyboard events with a null device for some
      // Consumer usages, see if any device is expecting the keyboard
      // event because one of those Consumer usages is currently pressed.
      if (e.IsKeyboard && e.Device == null)
        return _hidDevices.Values.Any(d => d.HandleKeyboardEvent(e));

      string deviceId = HidInputDevice.GetDeviceId(e.Device);
      if (!_hidDevices.TryGetValue(deviceId, out HidInputDevice device))
        device = _hidDevices[deviceId] = CreateDevice(e.Device);
      return device.HandleHidEvent(e);
    }

    protected void HandleDeviceRemoval(string deviceName)
    {
      var devicesToRemove = _hidDevices.Values.Where(d => d.DeviceName == deviceName).ToArray();
      foreach (var device in devicesToRemove)
        _hidDevices.Remove(device.Metadata.Id);
    }

    protected HidInputDevice CreateDevice(Device device)
    {
      return new HidInputDevice(device, GetDefaultMapping(device));
    }

    protected IEnumerable<MappedAction> GetDefaultMapping(Device device)
    {
      if (device == null)
        return null;

      IEnumerable<Device> devices = _hidManager.ConnectedDevices.Where(d => d.VendorId == device.VendorId && d.ProductId == device.ProductId);
      if (devices.Any(d => d.UsagePage == (ushort)UsagePage.WindowsMediaCenterRemoteControl))
        return WindowsMediaCenterRemote.DefaultMapping;
      else if (devices.Any(d => d.IsGamePad))
        return GamePad.DefaultMapping;
      else
        return null;
    }

    private void ResetInput()
    {
      foreach (HidInputDevice device in _hidDevices.Values)
        device.ResetInput();
      _legacyKeyboard.ResetInput();
      _messageFilter.Reset();
    }
  }
}
