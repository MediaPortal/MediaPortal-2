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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Serialization;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.Settings;
using MediaPortal.Plugins.InputDeviceManager.Models;
using MediaPortal.Plugins.InputDeviceManager.RawInput;
using MediaPortal.UI.Control.InputManager;
using MediaPortal.UI.General;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.UI.SkinEngine.InputManagement;
using MediaPortal.UI.SkinEngine.ScreenManagement;

namespace MediaPortal.Plugins.InputDeviceManager
{
  public class InputDeviceManager : IPluginStateTracker
  {
    private const bool SUPPORT_REPEATS = true;
    private const int WM_KEYDOWN = 0x100;
    private const int WM_KEYUP = 0x101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;
    private const int WM_ACTIVATE = 0x0006;
    private const int WA_INACTIVE = 0;
    private const int WM_INPUT = 0x00FF;

    private static readonly ConcurrentDictionary<string, InputDevice> _inputDevices = new ConcurrentDictionary<string, InputDevice>();
    private static readonly ConcurrentDictionary<string, long> _pressedKeys = new ConcurrentDictionary<string, long>();
    private static List<Action<object, string, string, IDictionary<string, long>>> _externalKeyPressHandlers = new List<Action<object, string, string, IDictionary<string, long>>>();
    private static object _listSyncObject = new object();
    private static ConcurrentDictionary<long, (string Name, long Code)> _genericKeyDownEvents = new ConcurrentDictionary<long, (string Name, long Code)>();
    private static bool _nextKeyEventHandled = false;
    private static List<MappedKeyCode> _defaultRemoteKeyCodes = new List<MappedKeyCode>();
    private static readonly List<long> _navigationKeyboardKeys = new List<long>
    {
      (long)Keys.Return,
      (long)Keys.Escape,
      (long)Keys.Left,
      (long)Keys.Right,
      (long)Keys.Up,
      (long)Keys.Down,
      (long)Keys.PageDown,
      (long)Keys.PageUp,
      (long)Keys.Home,
      (long)Keys.End,
    };
    private static readonly List<Key> _navigationMp2Keys = new List<Key>
    {
      Key.Ok,
      Key.Escape,
      Key.Left,
      Key.Right,
      Key.Up,
      Key.Down,
      Key.PageDown,
      Key.PageUp,
      Key.Fwd,
      Key.Rew
    };
    //Windows is automatically generating keyboard codes for some consumer usages. We need to ignore those, 
    //because they lack device information needed to map them.
    private static readonly List<long> _windowsGeneratedKeys = new List<long>
    {
      GetUniqueGenericKeyCode(true, false, (long)Keys.BrowserBack),
      GetUniqueGenericKeyCode(true, false, (long)Keys.MediaNextTrack),
      GetUniqueGenericKeyCode(true, false, (long)Keys.MediaPreviousTrack),
      GetUniqueGenericKeyCode(true, false, (long)Keys.MediaPlayPause),
      GetUniqueGenericKeyCode(true, false, (long)Keys.VolumeUp),
      GetUniqueGenericKeyCode(true, false, (long)Keys.VolumeDown),
      GetUniqueGenericKeyCode(true, false, (long)Keys.VolumeMute),
    };
    private static ConcurrentDictionary<long, object> _ignoredKeys = new ConcurrentDictionary<long, object>();

    private SynchronousMessageQueue _messageQueue;

    public static InputDeviceManager Instance { get; private set; }
    public static IDictionary<string, InputDevice> InputDevices
    {
      get { return _inputDevices; }
    }

    #region Initialization

    public InputDeviceManager()
    {
      Instance = this;
    }
    
    #endregion

    #region Form key handling

    private void OnPreviewMessage(SynchronousMessageQueue queue)
    {
      try
      {
        SystemMessage message;
        while ((message = queue.Dequeue()) != null)
        {
          if (message.ChannelName == WindowsMessaging.CHANNEL)
          {
            WindowsMessaging.MessageType messageType = (WindowsMessaging.MessageType)message.MessageType;
            switch (messageType)
            {
              case WindowsMessaging.MessageType.WindowsBroadcast:
                Message msg = (Message)message.MessageData[WindowsMessaging.MESSAGE];
                //We need to handle the keyboard keys in HID handler because we need know which device sends them
                if (msg.Msg == WM_KEYDOWN || msg.Msg == WM_KEYUP || msg.Msg == WM_SYSKEYDOWN || msg.Msg == WM_SYSKEYUP)
                {
                  var key = ConvertSystemKey((Keys)msg.WParam);
                  
                  //Check if Windows generated this key from another raw input. Ignore it if it did
                  if (_windowsGeneratedKeys.Contains(GetUniqueGenericKeyCode(true, false, (long)key)))
                    _nextKeyEventHandled = true;

                  if (_nextKeyEventHandled)
                  {
                    //SharpLibHid handles WM_INPUT messages from which Windows creates WM_KEYDOWN, WM_KEYUP, WM_SYSKEYDOWN and WM_SYSKEYUP messages.
                    //So to avoid a duplicate key press we need to handle (consume) the following WM_KEYDOWN, WM_KEYUP, WM_SYSKEYDOWN and WM_SYSKEYUP messages.
                    _nextKeyEventHandled = false;
                    message.MessageData[WindowsMessaging.HANDLED] = true;
                    //ServiceRegistration.Get<ILogger>().Debug("InputDeviceManager: Preview message handled {0}", key);
                  }
                }
                else if (msg.Msg == WM_ACTIVATE & msg.WParam == (IntPtr)WA_INACTIVE)
                {
                  ServiceRegistration.Get<ILogger>().Debug("InputDeviceManager: Pressed keys reset");

                  _nextKeyEventHandled = false;
                  _genericKeyDownEvents.Clear();
                  _pressedKeys.Clear();
                }
                break;
              case WindowsMessaging.MessageType.HidBroadcast:
                HidEvent hidEvent = (HidEvent)message.MessageData[WindowsMessaging.HID_EVENT];

                // If the current focus is in a control that handles text input, e.g. a text box, don't map the key
                if (hidEvent.IsKeyboard && DoesCurrentFocusedControlNeedTextInput())
                  return;

                if (!TryDecodeEvent(hidEvent, out string type, out string name, out long code, out bool buttonUp, out bool buttonDown))
                  return;

                if (CheckKeyPresses(type, hidEvent.Device?.FriendlyName, hidEvent.IsGeneric, hidEvent.IsRepeat, new[] { new KeyCode(name, code) }, buttonUp, buttonDown) && hidEvent.IsKeyboard)
                  _nextKeyEventHandled = true;
                break;
            }
          }
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("InputDeviceManager: Preview message failed", ex);
      }
    }

    private void SubscribeToMessages()
    {
      if (_messageQueue != null)
        return;
      _messageQueue = new SynchronousMessageQueue(this, new[] { WindowsMessaging.CHANNEL });
      _messageQueue.MessagesAvailable += OnPreviewMessage;
      _messageQueue.RegisterAtAllMessageChannels();
    }

    private void UnsubscribeFromMessages()
    {
      if (_messageQueue == null)
        return;
      _messageQueue.Dispose();
      _messageQueue = null;
    }

    #endregion

    #region Logging   

    private static string GetLogEventData(string info, HidEvent hidEvent)
    {
      string str = string.IsNullOrEmpty(info) ? "" : $"{info} ";
      str += "HID Event";
      if (hidEvent.IsButtonDown)
        str += ", DOWN";
      if (hidEvent.IsButtonUp)
        str += ", UP";
      if (hidEvent.IsGeneric)
      {
        str += ", Generic";
        for (int aIndex = 0; aIndex < hidEvent.Usages.Count; ++aIndex)
          str += ", Usage: " + hidEvent.UsageNameAndValues[aIndex];
        str += ", UsagePage: " + hidEvent.UsagePageNameAndValue + ", UsageCollection: " + hidEvent.UsageCollectionNameAndValue + ", Input Report: 0x" + hidEvent.InputReportString;
        if (hidEvent.Device?.IsGamePad ?? false)
        {
          str += ", GamePad, DirectionState: " + hidEvent.DirectionPadState;
        }
        else if (hidEvent.UsagePageEnum == UsagePage.WindowsMediaCenterRemoteControl)
        {
          str += ", Remote";
        }
        else if (hidEvent.UsagePageEnum == UsagePage.Consumer)
        {
          str += ", Consumer";
        }
        else if (hidEvent.UsagePageEnum == UsagePage.SimulationControls)
        {
          str += ", Sim";
        }
        else if (hidEvent.UsagePageEnum == UsagePage.Telephony)
        {
          str += ", Mobile";
        }
      }
      else if (hidEvent.IsKeyboard)
        str += ", Keyboard" + (object)", Virtual Key: " + hidEvent.VirtualKey.ToString();
      else if (hidEvent.IsMouse)
        str += ", Mouse, Flags: " + hidEvent.MouseButtonFlags;
      if (hidEvent.IsBackground)
        str += ", Background";
      if (hidEvent.IsRepeat)
        str += ", Repeat: " + hidEvent.RepeatCount;
      if (hidEvent.HasModifierAlt)
        str += ", AltKey";
      if (hidEvent.HasModifierControl)
        str += ", ControlKey";
      if (hidEvent.HasModifierShift)
        str += ", ShiftKey";
      if (hidEvent.HasModifierWindows)
        str += ", WindowsKey";
      if (!string.IsNullOrEmpty(hidEvent.Device?.FriendlyName))
        str += ", FriendlyName: " + hidEvent.Device.FriendlyName;
      if (!string.IsNullOrEmpty(hidEvent.Device?.Manufacturer))
        str += ", Manufacturer: " + hidEvent.Device.Manufacturer;
      if (!string.IsNullOrEmpty(hidEvent.Device?.Product))
        str += ", Product: " + hidEvent.Device.Product;
      if (!string.IsNullOrEmpty(hidEvent.Device?.ProductId.ToString()))
        str += ", ProductId: " + hidEvent.Device.ProductId.ToString();
      if (!string.IsNullOrEmpty(hidEvent.Device?.VendorId.ToString()))
        str += ", VendorId: " + hidEvent.Device.VendorId.ToString();
      if (!string.IsNullOrEmpty(hidEvent.Device?.Version.ToString()))
        str += ", Version: " + hidEvent.Device.Version.ToString();

      return str;
    }

    private static void LogEvent(string info, HidEvent hidEvent)
    {
      ServiceRegistration.Get<ILogger>().Debug(GetLogEventData(info, hidEvent));
    }

    #endregion

    #region HID event handling

    public static long GetUniqueGenericKeyCode(UsagePage usagePage, long keyCode)
    {
      return GetUniqueGenericKeyCode(false, false, usagePage, keyCode);
    }

    public static long GetUniqueGenericKeyCode(bool isKeyboard, bool isMouse, long keyCode)
    {
      return GetUniqueGenericKeyCode(isKeyboard, isMouse, UsagePage.Undefined, keyCode);
    }

    //There can be overlap between key codes from differen usage pages. This function makes them unique
    public static long GetUniqueGenericKeyCode(bool isKeyboard, bool isMouse, UsagePage usagePage, long keyCode)
    {
      if (keyCode < 1000)
      {
        if (isKeyboard)
          return keyCode; //Must be returned as is to be comparable to keys
        else if (isMouse)
          return 1000 + keyCode;
        if (usagePage == UsagePage.WindowsMediaCenterRemoteControl)
          return 2000 + keyCode;
        else if (usagePage == UsagePage.Consumer)
          return 3000 + keyCode;
        else if (usagePage == UsagePage.GameControls)
          return 4000 + keyCode;
        else if (usagePage == UsagePage.SimulationControls)
          return 5000 + keyCode;
        else if (usagePage == UsagePage.Telephony)
          return 6000 + keyCode;
        else if (usagePage == UsagePage.GenericDesktopControls)
          return 7000 + keyCode;

        return keyCode;
      }
      else
      {
        return -keyCode;
      }
    }

    private static bool TryDecodeEvent(HidEvent hidEvent, out string device, out string name, out long code, out bool buttonUp, out bool buttonDown)
    {
      device = "";
      name = "";
      code = 0;
      buttonUp = hidEvent.IsButtonUp;
      buttonDown = hidEvent.IsButtonDown;

      //Below code snippet logs all HID events (except mouse movement) and should only be used for debugging
      //if ((hidEvent.IsMouse && hidEvent.RawInput.mouse.buttonsStr.usButtonFlags > 0) || !hidEvent.IsMouse)
      //  ServiceRegistration.Get<ILogger>().Debug(GetLogEventData("Log", hidEvent));

      //Windows is automatically generating keyboard codes for some consumer usages. We need to ignore those, 
      //because they lack device information needed to map them.
      if (hidEvent.Device == null)
      {
        //LogEvent("Invalid device", hidEvent);
        return false;
      }
      long deviceId = (hidEvent.Device.VendorId << 16) | hidEvent.Device.ProductId;
      device = deviceId.ToString("X");

      if (hidEvent.IsKeyboard)
      {
        if (hidEvent.VirtualKey != Keys.None)
        {
          if (hidEvent.VirtualKey != Keys.Escape)
          {
            var key = ConvertSystemKey(hidEvent.VirtualKey);
            name = KeyMapper.GetMicrosoftKeyName((int)key);
            code = GetUniqueGenericKeyCode(true, false, (long)key);
          }
          else
          {
            return false; //Escape reserved for dialog close
          }
        }
        else
        {
          LogEvent("Invalid key", hidEvent);
          return false; //Unsupported
        }
      }
      else if (hidEvent.IsMouse)
      {
        int id = 0;
        switch (hidEvent.MouseButtonFlags)
        {
          case RawMouseButtonFlags.LeftButtonDown:
          case RawMouseButtonFlags.LeftButtonUp:
          case RawMouseButtonFlags.RightButtonDown:
          case RawMouseButtonFlags.RightButtonUp:
          case RawMouseButtonFlags.MouseWheel:
          case RawMouseButtonFlags.MouseHorizontalWheel:
            return false; //Reserve these events for navigation purposes
          case RawMouseButtonFlags.MiddleButtonDown:
            buttonDown = true;
            id = 3;
            break;
          case RawMouseButtonFlags.MiddleButtonUp:
            buttonUp = true;
            id = 3;
            break;
          case RawMouseButtonFlags.Button4Down:
            buttonDown = true;
            id = 4;
            break;
          case RawMouseButtonFlags.Button4Up:
            buttonUp = true;
            id = 4;
            break;
          case RawMouseButtonFlags.Button5Down:
            buttonDown = true;
            id = 5;
            break;
          case RawMouseButtonFlags.Button5Up:
            buttonUp = true;
            id = 5;
            break;
          default:
            return false; //Unsupported
        }
        name = $"Button{id}";
        code = GetUniqueGenericKeyCode(false, true, id);
      }
      else if (hidEvent.IsGeneric)
      {
        long usageCategoryId = (hidEvent.UsagePage << 16) | hidEvent.UsageCollection;

        //Generic events with no usages are button up
        if (!hidEvent.Usages.Any() || buttonUp)
        {
          buttonUp = true;

          //Usage was saved from button down because its usage cannot be determined here
          if (!_genericKeyDownEvents.TryRemove(usageCategoryId, out var button))
          {
            bool handled = false;
            if (hidEvent.Device?.IsGamePad == true)
            {
              //Button down never happened so presume this is it if possible
              //because sometimes button down events are triggered as button up events
              var state = hidEvent.DirectionPadState;
              if (state != DirectionPadState.Rest)
              {
                name = $"Pad{state.ToString()}";
                code = -(long)state;
                buttonDown = true;
                handled = true;
              }
            }
            if (!handled)
            {
              //Some devices send duplicate button up events so ignore
              LogEvent("Unknown key", hidEvent);
              return false;
            }
          }
          else
          {
            name = button.Name;
            code = GetUniqueGenericKeyCode(hidEvent.UsagePageEnum, button.Code);
          }
        }
        else //Generic events with usages are button down
        {
          buttonDown = true;

          var id = hidEvent.Usages.FirstOrDefault();
          if (string.IsNullOrEmpty(device) || id == 0)
          {
            LogEvent("Invalid usage", hidEvent);
            return false;
          }
          if (_ignoredKeys.ContainsKey(GetUniqueGenericKeyCode(hidEvent.UsagePageEnum, id)))
            return false;

          //Some devices send duplicate button down events so ignore
          if (_genericKeyDownEvents.Values.Any(b => b.Code == id))
          {
            LogEvent("Duplicate key", hidEvent);
            return false;
          }

          if (hidEvent.Device?.IsGamePad == true)
          {
            var state = hidEvent.DirectionPadState;
            if (state != DirectionPadState.Rest)
            {
              name = $"Pad{state.ToString()}";
              code = -(long)state;
            }
            else if (buttonDown || buttonUp)
            {
              name = $"PadButton{id}";
              code = GetUniqueGenericKeyCode(hidEvent.UsagePageEnum, id);
            }
          }
          else if (hidEvent.UsagePageEnum == UsagePage.WindowsMediaCenterRemoteControl)
          {
            if (buttonDown || buttonUp)
            {
              string usage = id.ToString();
              if (Enum.IsDefined(typeof(WindowsMediaCenterRemoteControl), id))
                usage = Enum.GetName(typeof(WindowsMediaCenterRemoteControl), id);
              else if (Enum.IsDefined(typeof(HpWindowsMediaCenterRemoteControl), id))
                usage = Enum.GetName(typeof(HpWindowsMediaCenterRemoteControl), id);
              else
              {
                IgnoreKey(id, hidEvent);
                return false;
              }

              name = $"Remote{usage}";
              code = GetUniqueGenericKeyCode(hidEvent.UsagePageEnum, id);
            }
          }
          else if (hidEvent.UsagePageEnum == UsagePage.Consumer)
          {
            if (buttonDown || buttonUp)
            {
              string usage = id.ToString();
              if (Enum.IsDefined(typeof(ConsumerControl), id))
                usage = Enum.GetName(typeof(ConsumerControl), id);
              else
              {
                IgnoreKey(id, hidEvent);
                return false;
              }

              name = $"{usage}";
              code = GetUniqueGenericKeyCode(hidEvent.UsagePageEnum, id);
            }
          }
          else if (hidEvent.UsagePageEnum == UsagePage.GameControls)
          {
            if (buttonDown || buttonUp)
            {
              string usage = id.ToString();
              if (Enum.IsDefined(typeof(GameControl), id))
                usage = Enum.GetName(typeof(GameControl), id);
              else
              {
                IgnoreKey(id, hidEvent);
                return false;
              }

              name = $"{usage}";
              code = GetUniqueGenericKeyCode(hidEvent.UsagePageEnum, id);
            }
          }
          else if (hidEvent.UsagePageEnum == UsagePage.SimulationControls)
          {
            if (buttonDown || buttonUp)
            {
              string usage = id.ToString();
              if (Enum.IsDefined(typeof(SimulationControl), id))
                usage = Enum.GetName(typeof(SimulationControl), id);
              else
              {
                IgnoreKey(id, hidEvent);
                return false;
              }

              name = $"{usage}";
              code = GetUniqueGenericKeyCode(hidEvent.UsagePageEnum, id);
            }
          }
          else if (hidEvent.UsagePageEnum == UsagePage.Telephony)
          {
            if (buttonDown || buttonUp)
            {
              string usage = id.ToString();
              if (Enum.IsDefined(typeof(TelephonyDevice), id))
                usage = Enum.GetName(typeof(TelephonyDevice), id);
              else
              {
                IgnoreKey(id, hidEvent);
                return false;
              }

              name = $"{usage}";
              code = GetUniqueGenericKeyCode(hidEvent.UsagePageEnum, id);
            }
          }
          else if (hidEvent.UsagePageEnum == UsagePage.GenericDesktopControls)
          {
            if (buttonDown || buttonUp)
            {
              string usage = id.ToString();
              if (Enum.IsDefined(typeof(GenericDesktop), id))
                usage = Enum.GetName(typeof(GenericDesktop), id);
              else
              {
                IgnoreKey(id, hidEvent);
                return false;
              }

              name = $"{usage}";
              code = GetUniqueGenericKeyCode(hidEvent.UsagePageEnum, id);
            }
          }
          else if (buttonDown || buttonUp)
          {
            //LogEvent("Unsupported", hidEvent);
            return false;
          }
          else
          {
            //Is not a key up or down event so this is not possible to support
            return false;
          }

          //Save so it can be used for button up
          _genericKeyDownEvents.TryAdd(usageCategoryId, (name, id));
        }
      }
      else
      {
        //LogEvent("Unsupported", hidEvent);
        return false;
      }

      return true;
    }

    private static bool KeyCombinationsMatch(IEnumerable<long> keyMapping, IEnumerable<long> pressedKeys)
    {
      if (keyMapping.Count() == 0 || pressedKeys.Count() == 0)
        return false;

      return keyMapping.All(c => pressedKeys.Contains(c)) && pressedKeys.All(c => keyMapping.Contains(c));
    }

    private static void IgnoreKey(ushort id, HidEvent hidEvent)
    {
      if (_ignoredKeys.TryAdd(GetUniqueGenericKeyCode(hidEvent.UsagePageEnum, id), null))
        ServiceRegistration.Get<ILogger>().Debug(GetLogEventData("Ignored unknown key", hidEvent));
    }

    /// <summary>
    /// Windows message key and HID key might be different for the same key, so convert them to the same base key if needed
    /// </summary>
    private static Keys ConvertSystemKey(Keys key)
    {
      if (key == Keys.LControlKey || key == Keys.RControlKey || key == Keys.Control)
        return Keys.ControlKey;
      if (key == Keys.LMenu || key == Keys.RMenu || key == Keys.Alt)
        return Keys.Menu;
      if (key == Keys.LShiftKey || key == Keys.RShiftKey || key == Keys.Shift)
        return Keys.ShiftKey;

      return key;
    }

    private static bool CheckMappedKeys(InputDevice device, IEnumerable<long> codes, bool isRepeat, bool handleKeyPressIfFound)
    {
      if (device?.KeyMap.Count > 0)
      {
        var keyMappings = device.KeyMap.Where(m => KeyCombinationsMatch(m.Codes.Select(c => c.Code), codes));
        if (keyMappings?.Count() > 0)
        {
          //ServiceRegistration.Get<ILogger>().Debug("InputDeviceManager: Found keys: " + string.Join(", ", keyMappings.SelectMany(k => k.Codes).Select(k => k.Key)));
          //ServiceRegistration.Get<ILogger>().Debug("InputDeviceManager: Found codes: " + string.Join(", ", keyMappings.SelectMany(k => k.Codes).Select(k => k.Code)));
          return HandleKeyPress(keyMappings, isRepeat, handleKeyPressIfFound);
        }
      }
      return false;
    }

    private static bool CheckDefaultRemoteKeys(IEnumerable<long> codes, bool isRepeat, bool handleKeyPressIfFound)
    {
      var keyMappings = _defaultRemoteKeyCodes.Where(m => KeyCombinationsMatch(m.Codes.Select(c => c.Code), codes));
      if (keyMappings?.Count() > 0)
      {
        //ServiceRegistration.Get<ILogger>().Debug("InputDeviceManager: Found default keys: " + string.Join(", ", keyMappings.SelectMany(k => k.Codes).Select(k => k.Key)));
        //ServiceRegistration.Get<ILogger>().Debug("InputDeviceManager: Found default codes: " + string.Join(", ", keyMappings.SelectMany(k => k.Codes).Select(k => k.Code)));
        return HandleKeyPress(keyMappings, isRepeat, handleKeyPressIfFound);
      }
      return false;
    }

    private static bool IsModifierKey(long code)
    {
      return code == GetUniqueGenericKeyCode(true, false, (long)Keys.ControlKey) || 
        code == GetUniqueGenericKeyCode(true, false, (long)Keys.Menu) || 
        code == GetUniqueGenericKeyCode(true, false, (long)Keys.ShiftKey) || 
        code == GetUniqueGenericKeyCode(true, false, (long)Keys.RWin) || 
        code == GetUniqueGenericKeyCode(true, false, (long)Keys.LWin);
    }

    private static bool AddPressedKey(string name, long code)
    {
      if (_pressedKeys.Values.Any(c => !IsModifierKey(c)) && !IsModifierKey(code) && !_pressedKeys.Values.Any(c => c == code))
      {
        //ServiceRegistration.Get<ILogger>().Debug("InputDeviceManager: Invalid key {0} in combination with {1}", name, string.Join(", ", _pressedKeys.Keys));
        return false;
      }
      else
      {
        _pressedKeys.TryAdd(name, code);
        return true;
      }
    }

    private static void RemovePressedKey(string name)
    {
      _pressedKeys.TryRemove(name, out _);
    }

    private static bool CheckKeyPresses(string type, string deviceName, bool isGeneric, bool isRepeat, IEnumerable<KeyCode> keys, bool buttonUp, bool buttonDown)
    {
      bool handleKeyPress = (SUPPORT_REPEATS && buttonDown) || (!SUPPORT_REPEATS && buttonUp);

      if (buttonDown)
      {
        foreach (var key in keys)
        {
          if (!AddPressedKey(key.Key, key.Code))
          {
            //Key should not be handled if it could not be added because then the previously added key will be handled again instead
            handleKeyPress = false;
          }
        }
      }

      //Check mapped keys
      bool keyHandled = false;
      if (_inputDevices.TryGetValue(type, out var device))
      {
        //ServiceRegistration.Get<ILogger>().Debug("InputDeviceManager: Checking mapping for device: " + device.Name);
        //ServiceRegistration.Get<ILogger>().Debug("InputDeviceManager: Checking keys: " + string.Join(", ", _pressedKeys.Select(k => k.Key)));
        //ServiceRegistration.Get<ILogger>().Debug("InputDeviceManager: Checking codes: " + string.Join(", ", _pressedKeys.Select(k => k.Value)));

        if (CheckMappedKeys(device, _pressedKeys.Values, isRepeat, handleKeyPress))
          keyHandled = true;
      }

      //Check if default handling is available for unhandled key presses
      if (!keyHandled)
      {
        //ServiceRegistration.Get<ILogger>().Debug("InputDeviceManager: Checking default keys: " + string.Join(", ", _pressedKeys.Select(k => k.Key)));
        //ServiceRegistration.Get<ILogger>().Debug("InputDeviceManager: Checking default codes: " + string.Join(", ", _pressedKeys.Select(k => k.Value)));

        if (CheckDefaultRemoteKeys(_pressedKeys.Values, isRepeat, handleKeyPress))
          keyHandled = true;
      }

      if (_externalKeyPressHandlers.Count > 0)
      {
        if (handleKeyPress && !isRepeat) //Only send handled presses and no repeats for better consistency
        {
          lock (_listSyncObject)
          {
            foreach (var action in _externalKeyPressHandlers)
              action.Invoke(Instance, deviceName, type, _pressedKeys);
          }
        }

        //Allow navigation keys if unhandled
        if (isGeneric || !keys.All(k => _navigationKeyboardKeys.Contains(k.Code)))
          keyHandled = true;
      }

      if (buttonUp)
      {
        foreach (var key in keys)
          RemovePressedKey(key.Key);
      }
      return keyHandled;
    }

    private static bool HandleKeyPress(IEnumerable<MappedKeyCode> keyMappings, bool isRepeat, bool handleKeyPressIfFound)
    {
      if (!(keyMappings?.Count() > 0))
        return false;

      bool keyHandled = false;
      bool externalHandling = _externalKeyPressHandlers.Count > 0;
      foreach (var keyMapping in keyMappings)
      {
        string[] actionArray = keyMapping.Key.Split('.');
        if (actionArray.Length >= 2)
        {
          if (keyMapping.Key.StartsWith(InputDeviceModel.KEY_PREFIX, StringComparison.InvariantCultureIgnoreCase))
          {
            if (_navigationMp2Keys.Any(k => k.Name == actionArray[1]) || !externalHandling) //Only allow navigation key during external handling
            {
              if (handleKeyPressIfFound)
              {
                ServiceRegistration.Get<ILogger>().Debug("InputDeviceManager: Executing key action: '{0}'", actionArray[1]);
                if (!actionArray[1].Equals("None", StringComparison.InvariantCultureIgnoreCase))
                {
                  var key = Key.GetSpecialKeyByName(actionArray[1]);
                  if (key != null) //It is a special key
                    ServiceRegistration.Get<IInputManager>().KeyPress(key);
                  else //Presume it is a printable key
                    ServiceRegistration.Get<IInputManager>().KeyPress(new Key(actionArray[1][0]));
                }
              }
              keyHandled = true;
            }
          }
          else if (!externalHandling) //Don't interfere with external handlers by executing actions
          {
            if (keyMapping.Key.StartsWith(InputDeviceModel.ACTION_PREFIX, StringComparison.InvariantCultureIgnoreCase))
            {
              HandleAction(handleKeyPressIfFound, isRepeat, actionArray[1]);
              keyHandled = true;
            }
            else if (keyMapping.Key.StartsWith(InputDeviceModel.HOME_PREFIX, StringComparison.InvariantCultureIgnoreCase))
            {
              HandleAction(handleKeyPressIfFound, isRepeat, actionArray[1], InputDeviceModel.HOME_STATE_ID);
              keyHandled = true;
            }
            else if (keyMapping.Key.StartsWith(InputDeviceModel.CONFIG_PREFIX, StringComparison.InvariantCultureIgnoreCase))
            {
              HandleAction(handleKeyPressIfFound, isRepeat, actionArray[1], InputDeviceModel.CONFIGURATION_STATE_ID);
              keyHandled = true;
            }
          }
        }
      }
      return keyHandled;
    }

    private static bool HandleAction(bool handleKeyPressIfFound, bool isRepeat, string actionName, Guid? requiredState = null)
    {
      if (handleKeyPressIfFound && !isRepeat)
      {
        string actionType = "global";
        if (requiredState == InputDeviceModel.HOME_STATE_ID)
          actionType = "home";
        else if (requiredState == InputDeviceModel.CONFIGURATION_STATE_ID)
          actionType = "config";

        ServiceRegistration.Get<ILogger>().Debug("InputDeviceManager: Executing {0} action: {1}", actionType, actionName);
        return ExecuteAction(actionName, requiredState);
      }
      return false;
    }

    private static bool ExecuteAction(string name, Guid? requiredState = null)
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      if (workflowManager != null)
      {
        if (requiredState.HasValue && workflowManager.CurrentNavigationContext.WorkflowState.StateId != requiredState.Value)
          workflowManager.NavigatePush(requiredState.Value);

        foreach (NavigationContext context in workflowManager.NavigationContextStack.ToList())
        {
          var action = context.MenuActions.Values.FirstOrDefault(a => a.Name == name);
          if (action != null)
          {
            action.Execute();
            return true;
          }
        }
      }
      return false;
    }

    /// <summary>
    /// Checks if the currently focused control requires text input.
    /// </summary>
    /// <returns><c>True</c> if the focused control requires text input.</returns>
    private static bool DoesCurrentFocusedControlNeedTextInput()
    {
      var sm = ServiceRegistration.Get<IScreenManager>(false) as ScreenManager;
      if (sm == null)
        return false;

      Visual focusedElement = sm.FocusedScreen?.FocusedElement;
      while (focusedElement != null)
      {
        // Currently only the TextControl requires text input but ideally this check would be extensible
        // as a plugin could potentially add a new control which wouldn't be handled here.
        if (focusedElement is TextControl)
          return true;
        focusedElement = focusedElement.VisualParent;
      }
      return false;
    }

    #endregion

    #region Settings

    private ICollection<MappedKeyCode> LoadRemoteMap(string remoteFile)
    {
      if (!File.Exists(remoteFile))
        return new List<MappedKeyCode>();

      XmlSerializer reader = new XmlSerializer(typeof(List<MappedKeyCode>));
      using (StreamReader file = new StreamReader(remoteFile))
        return (ICollection<MappedKeyCode>)reader.Deserialize(file);
    }

    private void LoadDefaulRemoteMaps(PluginRuntime pluginRuntime)
    {
      _defaultRemoteKeyCodes.Clear();
      var keyCodes = LoadRemoteMap(pluginRuntime.Metadata.GetAbsolutePath("DefaultRemoteMap.xml"));
      foreach (MappedKeyCode mkc in keyCodes)
      {
        _defaultRemoteKeyCodes.Add(mkc);
      }
    }

    private void LoadSettings()
    {
      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
      InputManagerSettings settings = settingsManager.Load<InputManagerSettings>();

      UpdateLoadedSettings(settings);
    }

    /// <summary>
    /// This function updates the local variable "_inputDevices"
    /// </summary>
    /// <param name="settings"></param>
    public void UpdateLoadedSettings(InputManagerSettings settings)
    {
      _inputDevices.Clear();
      if (settings != null && settings.InputDevices != null)
        try
        {
          foreach (InputDevice device in settings.InputDevices)
            _inputDevices.TryAdd(device.Type, device);
        }
        catch
        {
          // ignored
        }
    }

    #endregion

    #region External event handling

    public bool RegisterExternalKeyHandling(Action<object, string, string, IDictionary<string, long>> hidEvent)
    {
      lock (_listSyncObject)
      {
        if (!_externalKeyPressHandlers.Contains(hidEvent))
        {
          _externalKeyPressHandlers.Add(hidEvent);
          return true;
        }
      }
      return false;
    }

    public bool UnRegisterExternalKeyHandling(Action<object, string, string, IDictionary<string, long>> hidEvent)
    {
      lock (_listSyncObject)
      {
        if (_externalKeyPressHandlers.Contains(hidEvent))
        {
          _nextKeyEventHandled = false;
          _externalKeyPressHandlers.Remove(hidEvent);
          return true;
        }
      }
      return false;
    }

    public void RemoveAllExternalKeyHandling()
    {
      lock (_listSyncObject)
      {
        _externalKeyPressHandlers.Clear();
      }
    }

    #endregion

    #region Implementation of IPluginStateTracker

    /// <summary>
    /// Will be called when the plugin is started. This will happen as a result of a plugin auto-start
    /// or an item access which makes the plugin active.
    /// This method is called after the plugin's state was set to <see cref="PluginState.Active"/>.
    /// </summary>
    public void Activated(PluginRuntime pluginRuntime)
    {
      LoadSettings();
      LoadDefaulRemoteMaps(pluginRuntime);
      SubscribeToMessages();
    }

    /// <summary>
    /// Schedules the stopping of this plugin. This method returns the information
    /// if this plugin can be stopped. Before this method is called, the plugin's state
    /// will be changed to <see cref="PluginState.EndRequest"/>.
    /// </summary>
    /// <remarks>
    /// This method is part of the first phase in the two-phase stop procedure.
    /// After this method returns <c>true</c> and all item's clients also return <c>true</c>
    /// as a result of their stop request, the plugin's state will change to
    /// <see cref="PluginState.Stopping"/>, then all uses of items by clients will be canceled,
    /// then this plugin will be stopped by a call to method <see cref="IPluginStateTracker.Stop"/>.
    /// If either this method returns <c>false</c> or one of the items clients prevent
    /// the stopping, the plugin will continue to be active and the method <see cref="IPluginStateTracker.Continue"/>
    /// will be called.
    /// </remarks>
    /// <returns><c>true</c>, if this plugin can be stopped at this time, else <c>false</c>.
    /// </returns>
    public bool RequestEnd()
    {
      return true;
    }

    /// <summary>
    /// Second step of the two-phase stopping procedure. This method stops this plugin,
    /// i.e. removes the integration of this plugin into the system, which was triggered
    /// by the <see cref="IPluginStateTracker.Activated"/> method.
    /// </summary>
    public void Stop()
    {
      UnsubscribeFromMessages();
    }

    /// <summary>
    /// Revokes the end request which was triggered by a former call to the
    /// <see cref="IPluginStateTracker.RequestEnd"/> method and restores the active state. After this call, the plugin remains active as
    /// it was before the call of <see cref="IPluginStateTracker.RequestEnd"/> method.
    /// </summary>
    public void Continue()
    {
    }

    /// <summary>
    /// Will be called before the plugin manager shuts down. The plugin can perform finalization
    /// tasks here. This method will called independently from the plugin state, i.e. it will also be called when the plugin
    /// was disabled or not started at all.
    /// </summary>
    public void Shutdown()
    {
    }

    #endregion
  }
}
