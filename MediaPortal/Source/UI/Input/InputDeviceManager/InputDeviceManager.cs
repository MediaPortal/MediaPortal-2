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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
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
using MediaPortal.UI.Services.UserManagement;
using SharpLib.Hid;
using SharpLib.Win32;

namespace MediaPortal.Plugins.InputDeviceManager
{
  public class InputDeviceManager : IPluginStateTracker
  {
    private const bool CAPTURE_ONLY_IN_FOREGROUND = true;
    private static readonly Dictionary<string, InputDevice> _inputDevices = new Dictionary<string, InputDevice>();
    private static IScreenControl _screenControl;
    private static readonly ConcurrentDictionary<string, long> _pressedKeys = new ConcurrentDictionary<string, long>();
    private static SharpLib.Hid.Handler _hidHandler;
    private delegate void OnHidEventDelegate(object sender, SharpLib.Hid.Event hidEvent);
    private static List<Action<object, SharpLib.Hid.Event>> _externalKeyPressHandlers = new List<Action<object, SharpLib.Hid.Event>>();
    private static object _listSyncObject = new object();
    private static Message _currentMessage;
    private static bool _currentMessageHandled;

    private SynchronousMessageQueue _messageQueue;

    public InputDeviceManager()
    {
      Instance = this;
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

    public static InputDeviceManager Instance { get; private set; }

    public static IDictionary<string, InputDevice> InputDevices
    {
      get { return _inputDevices; }
    }

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
                _currentMessage = (Message)message.MessageData[WindowsMessaging.MESSAGE];
                _currentMessageHandled = false;
                //WM_KEYDOWN and WM_KEYUP are not handled by SharpLibHid so wee need to handle them to avoid a duplicate key press
                if (_externalKeyPressHandlers.Count == 0 && (_currentMessage.Msg == 0x100 || _currentMessage.Msg == 0x101))
                {
                  var key = (Keys)_currentMessage.WParam;
                  if (_inputDevices.TryGetValue("Keyboard", out InputDevice device) && device.KeyMap.Any(m => m.Codes.FirstOrDefault()?.Code == (long)key))
                    _currentMessageHandled = true;
                }
                _hidHandler?.ProcessInput(ref _currentMessage);
                message.MessageData[WindowsMessaging.MESSAGE] = _currentMessage;
                if (_currentMessageHandled)
                  message.MessageData[WindowsMessaging.HANDLED] = true;
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

    private static string GetLogEventData(SharpLib.Hid.Event hidEvent, bool unsupported)
    {
      string str = unsupported ? "Unsupported" : ""; 
      str += "HID Event";
      if (hidEvent.IsButtonDown)
        str += ", DOWN";
      if (hidEvent.IsButtonUp)
        str += ", UP";
      if (hidEvent.IsGeneric)
      {
        str += ", Generic";
        for (int aIndex = 0; aIndex < hidEvent.Usages.Count; ++aIndex)
          str += ", Usage: " + hidEvent.UsageNameAndValue(aIndex);
        str += ", UsagePage: " + hidEvent.UsagePageNameAndValue() + ", UsageCollection: " + hidEvent.UsageCollectionNameAndValue() + ", Input Report: 0x" + hidEvent.InputReportString();
        if (hidEvent.Device?.IsGamePad ?? false)
        {
          str += ", GamePad, DirectionState: " + hidEvent.GetDirectionPadState();
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
        str += ", Mouse, Flags: " + hidEvent.RawInput.mouse.buttonsStr.usButtonFlags;
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

      return str;
    }

    private static void LogUnsupportedEvent(SharpLib.Hid.Event hidEvent)
    {
      ServiceRegistration.Get<ILogger>().Debug(GetLogEventData(hidEvent, true));
    }

    public static bool TryDecodeEvent(SharpLib.Hid.Event hidEvent, out string device, out string name, out long code, out bool buttonUp, out bool buttonDown)
    {
      device = "";
      name = "";
      code = 0;
      buttonUp = hidEvent.IsButtonUp;
      buttonDown = hidEvent.IsButtonDown;

      //if ((hidEvent.IsMouse && hidEvent.RawInput.mouse.buttonsStr.usButtonFlags > 0) || !hidEvent.IsMouse)
      //  ServiceRegistration.Get<ILogger>().Info(GetLogEventData(hidEvent, true));

      if (hidEvent.IsKeyboard)
      {
        if (hidEvent.VirtualKey != Keys.None)
        {
          if (hidEvent.VirtualKey != Keys.Escape) 
          {
            device = "Keyboard";
            name = KeyMapper.GetMicrosoftKeyName((int)hidEvent.VirtualKey);
            code = (long)hidEvent.VirtualKey;
          }
          else
          {
            return false; //Escape reserved for dialog close
          }
        }
        else
        {
          LogUnsupportedEvent(hidEvent);
          return false; //Unsupported
        }
      }
      else if (hidEvent.IsMouse)
      {
        device = "Mouse";
        int id = 0;
        switch (hidEvent.RawInput.mouse.buttonsStr.usButtonFlags)
        {
          case RawInputMouseButtonFlags.RI_MOUSE_LEFT_BUTTON_DOWN:
          case RawInputMouseButtonFlags.RI_MOUSE_LEFT_BUTTON_UP:
          case RawInputMouseButtonFlags.RI_MOUSE_RIGHT_BUTTON_DOWN:
          case RawInputMouseButtonFlags.RI_MOUSE_RIGHT_BUTTON_UP:
          case RawInputMouseButtonFlags.RI_MOUSE_WHEEL:
            return false; //Reserve these events for navigation purposes
          case RawInputMouseButtonFlags.RI_MOUSE_MIDDLE_BUTTON_DOWN:
            buttonDown = true;
            id = 3;
            break;
          case RawInputMouseButtonFlags.RI_MOUSE_MIDDLE_BUTTON_UP:
            buttonUp = true;
            id = 3;
            break;
          case RawInputMouseButtonFlags.RI_MOUSE_BUTTON_4_DOWN:
            buttonDown = true;
            id = 4;
            break;
          case RawInputMouseButtonFlags.RI_MOUSE_BUTTON_4_UP:
            buttonUp = true;
            id = 4;
            break;
          case RawInputMouseButtonFlags.RI_MOUSE_BUTTON_5_DOWN:
            buttonDown = true;
            id = 5;
            break;
          case RawInputMouseButtonFlags.RI_MOUSE_BUTTON_5_UP:
            buttonUp = true;
            id = 5;
            break;
          default:
            return false; //Unsupported
        }
        name = $"Button{id}";
        code = id;
      }
      else if (hidEvent.IsGeneric)
      {
        //If a usage is exiting and there are no button states, it must be a combined event
        //Sometimes button down events are without usage and therefore unknown
        if (hidEvent.Usages.Any() && !buttonDown && !buttonUp) 
        {
          buttonDown = true;
          buttonUp = true;
        }

        if (hidEvent.Device != null && hidEvent.Device.IsGamePad)
        {
          device = "GamePad";
          var state = hidEvent.GetDirectionPadState();
          var id = hidEvent.Usages.FirstOrDefault();
          if (state != DirectionPadState.Rest)
          {
            name = $"Pad{state.ToString()}";
            code = -(long)state;
          }
          else if ((buttonDown || buttonUp) && id > 0)
          {
            name = $"PadButton{id}";
            code = id;
          }
          else
          {
            LogUnsupportedEvent(hidEvent);
            return false; //Unsupported
          }
        }
        else if (hidEvent.UsagePageEnum == UsagePage.WindowsMediaCenterRemoteControl)
        {
          device = "Remote";
          var id = hidEvent.Usages.FirstOrDefault();
          if ((buttonDown || buttonUp) && id > 0)
          {
            string usage = id.ToString();
            if (Enum.IsDefined(typeof(RemoteButton), id))
              usage = Enum.GetName(typeof(RemoteButton), id);
            else if (Enum.IsDefined(typeof(SharpLib.Hid.Usage.WindowsMediaCenterRemoteControl), id))
              usage = Enum.GetName(typeof(SharpLib.Hid.Usage.WindowsMediaCenterRemoteControl), id);
            else if (Enum.IsDefined(typeof(SharpLib.Hid.Usage.HpWindowsMediaCenterRemoteControl), id))
              usage = Enum.GetName(typeof(SharpLib.Hid.Usage.HpWindowsMediaCenterRemoteControl), id);

            name = $"Remote{usage}";
            code = id;
          }
          else
          {
            LogUnsupportedEvent(hidEvent);
            return false; //Unsupported
          }
        }
        else if (hidEvent.UsagePageEnum == UsagePage.Consumer)
        {
          device = "Consumer";
          var id = hidEvent.Usages.FirstOrDefault();
          if ((buttonDown || buttonUp) && id > 0)
          {
            string usage = id.ToString();
            if (Enum.IsDefined(typeof(SharpLib.Hid.Usage.ConsumerControl), id))
              usage = Enum.GetName(typeof(SharpLib.Hid.Usage.ConsumerControl), id);
            name = $"{usage}";
            code = id;
          }
          else
          {
            LogUnsupportedEvent(hidEvent);
            return false; //Unsupported
          }
        }
        else if (hidEvent.UsagePageEnum == UsagePage.GameControls)
        {
          device = "Game";
          var id = hidEvent.Usages.FirstOrDefault();
          if ((buttonDown || buttonUp) && id > 0)
          {
            string usage = id.ToString();
            if (Enum.IsDefined(typeof(SharpLib.Hid.Usage.GameControl), id))
              usage = Enum.GetName(typeof(SharpLib.Hid.Usage.GameControl), id);
            name = $"{usage}";
            code = id;
          }
          else
          {
            LogUnsupportedEvent(hidEvent);
            return false; //Unsupported
          }
        }
        else if (hidEvent.UsagePageEnum == UsagePage.SimulationControls)
        {
          device = "Sim";
          var id = hidEvent.Usages.FirstOrDefault();
          if ((buttonDown || buttonUp) && id > 0)
          {
            string usage = id.ToString();
            if (Enum.IsDefined(typeof(SharpLib.Hid.Usage.SimulationControl), id))
              usage = Enum.GetName(typeof(SharpLib.Hid.Usage.SimulationControl), id);
            name = $"{usage}";
            code = id;
          }
          else
          {
            LogUnsupportedEvent(hidEvent);
            return false; //Unsupported
          }
        }
        else if (hidEvent.UsagePageEnum == UsagePage.Telephony)
        {
          device = "Mobile";
          var id = hidEvent.Usages.FirstOrDefault();
          if ((buttonDown || buttonUp) && id > 0)
          {
            string usage = id.ToString();
            if (Enum.IsDefined(typeof(SharpLib.Hid.Usage.TelephonyDevice), id))
              usage = Enum.GetName(typeof(SharpLib.Hid.Usage.TelephonyDevice), id);
            name = $"{usage}";
            code = id;
          }
          else
          {
            LogUnsupportedEvent(hidEvent);
            return false; //Unsupported
          }
        }
        else if (hidEvent.Device != null)
        {
          device = hidEvent.Device.Name;
          var id = hidEvent.Usages.FirstOrDefault();
          if ((buttonDown || buttonUp) && id > 0)
          {
            name = $"Event{id}";
            code = id;
          }
          else
          {
            LogUnsupportedEvent(hidEvent);
            return false; //Unsupported
          }
        }
      }
      else
      {
        LogUnsupportedEvent(hidEvent);
        return false; //Unsupported
      }
      return true;
    }

    private static void OnHidEvent(object sender, SharpLib.Hid.Event hidEvent)
    {
      try
      {
        if (!hidEvent.IsValid)
          return;
        if (CAPTURE_ONLY_IN_FOREGROUND && hidEvent.IsBackground)
          return;

        if (hidEvent.IsRepeat)
          ServiceRegistration.Get<ILogger>().Debug("HID Event: Repeat");

        if (!TryDecodeEvent(hidEvent, out string type, out string name, out long code, out bool buttonUp, out bool buttonDown))
          return;

        if (buttonDown)
          _pressedKeys.TryAdd(name, code);

        if (_externalKeyPressHandlers.Count == 0)
        {
          InputDevice device;
          if (_inputDevices.TryGetValue(type, out device))
          {
            var keyMappings = device.KeyMap.Where(m => m.Codes.Select(c => c.Code).SequenceEqual(_pressedKeys.Values));
            if (keyMappings?.Count() > 0)
            {
              //_currentMessage.Result = new IntPtr(1);
              _currentMessageHandled = true;
              if (buttonUp)
              {
                foreach (var keyMapping in keyMappings)
                {
                  string[] actionArray = keyMapping.Key.Split('.');
                  if (actionArray.Length >= 2)
                  {
                    if (keyMapping.Key.StartsWith(InputDeviceModel.KEY_PREFIX, StringComparison.InvariantCultureIgnoreCase))
                    {
                      ServiceRegistration.Get<ILogger>().Debug("Executing key action: " + actionArray[1]);
                      ServiceRegistration.Get<IInputManager>().KeyPress(Key.GetSpecialKeyByName(actionArray[1]));
                    }
                    else if (keyMapping.Key.StartsWith(InputDeviceModel.HOME_PREFIX, StringComparison.InvariantCultureIgnoreCase))
                    {
                      ServiceRegistration.Get<ILogger>().Debug("Executing home action: " + actionArray[1]);
                      NavigateToScreen(actionArray[1]);
                    }
                    else if (keyMapping.Key.StartsWith(InputDeviceModel.CONFIG_PREFIX, StringComparison.InvariantCultureIgnoreCase))
                    {
                      ServiceRegistration.Get<ILogger>().Debug("Executing config action: " + actionArray[1]);
                      NavigateToScreen(actionArray[1], InputDeviceModel.CONFIGURATION_STATE_ID);
                    }
                  }
                }
              }
            }
          }
        }
        else
        {
          lock (_listSyncObject)
          {
            foreach (var action in _externalKeyPressHandlers)
              action.Invoke(sender, hidEvent);
          }
          _currentMessageHandled = true;
        }

        if (buttonUp)
          _pressedKeys.TryRemove(name, out _);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("InputDeviceManager: HID event failed", ex);
      }
    }

    private static bool NavigateToScreen(string name, Guid? requiredState = null)
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

    private static void StartThread()
    {
      while (_screenControl == null)
      {
        try
        {
          if (ServiceRegistration.IsRegistered<IScreenControl>())
          {
            _screenControl = ServiceRegistration.Get<IScreenControl>();

            SharpLib.Win32.RawInputDeviceFlags flags = CAPTURE_ONLY_IN_FOREGROUND ? 0 : SharpLib.Win32.RawInputDeviceFlags.RIDEV_INPUTSINK;
            //SharpLib.Win32.RawInputDeviceFlags flags = SharpLib.Win32.RawInputDeviceFlags.RIDEV_EXINPUTSINK;
            //SharpLib.Win32.RawInputDeviceFlags flags = SharpLib.Win32.RawInputDeviceFlags.RIDEV_INPUTSINK;

            List<RAWINPUTDEVICE> devices = new List<RAWINPUTDEVICE>();

            devices.Add(new RAWINPUTDEVICE
            {
              usUsagePage = (ushort)SharpLib.Hid.UsagePage.WindowsMediaCenterRemoteControl,
              usUsage = (ushort)SharpLib.Hid.UsageCollection.WindowsMediaCenter.WindowsMediaCenterRemoteControl,
              dwFlags = flags,
              hwndTarget = _screenControl.MainWindowHandle
            });
            devices.Add(new RAWINPUTDEVICE
            {
              usUsagePage = (ushort)SharpLib.Hid.UsagePage.WindowsMediaCenterRemoteControl,
              usUsage = (ushort)SharpLib.Hid.UsageCollection.WindowsMediaCenter.WindowsMediaCenterLowLevel,
              dwFlags = flags,
              hwndTarget = _screenControl.MainWindowHandle
            });

            devices.Add(new RAWINPUTDEVICE
            {
              usUsagePage = (ushort)SharpLib.Hid.UsagePage.Consumer,
              usUsage = (ushort)SharpLib.Hid.UsageCollection.Consumer.ConsumerControl,
              dwFlags = flags,
              hwndTarget = _screenControl.MainWindowHandle
            });
            devices.Add(new RAWINPUTDEVICE
            {
              usUsagePage = (ushort)SharpLib.Hid.UsagePage.Consumer,
              usUsage = (ushort)SharpLib.Hid.UsageCollection.Consumer.ApplicationLaunchButtons,
              dwFlags = flags,
              hwndTarget = _screenControl.MainWindowHandle
            });
            devices.Add(new RAWINPUTDEVICE
            {
              usUsagePage = (ushort)SharpLib.Hid.UsagePage.Consumer,
              usUsage = (ushort)SharpLib.Hid.UsageCollection.Consumer.FunctionButtons,
              dwFlags = flags,
              hwndTarget = _screenControl.MainWindowHandle
            });
            devices.Add(new RAWINPUTDEVICE
            {
              usUsagePage = (ushort)SharpLib.Hid.UsagePage.Consumer,
              usUsage = (ushort)SharpLib.Hid.UsageCollection.Consumer.GenericGuiApplicationControls,
              dwFlags = flags,
              hwndTarget = _screenControl.MainWindowHandle
            });
            devices.Add(new RAWINPUTDEVICE
            {
              usUsagePage = (ushort)SharpLib.Hid.UsagePage.Consumer,
              usUsage = (ushort)SharpLib.Hid.UsageCollection.Consumer.MediaSelection,
              dwFlags = flags,
              hwndTarget = _screenControl.MainWindowHandle
            });
            devices.Add(new RAWINPUTDEVICE
            {
              usUsagePage = (ushort)SharpLib.Hid.UsagePage.Consumer,
              usUsage = (ushort)SharpLib.Hid.UsageCollection.Consumer.NumericKeyPad,
              dwFlags = flags,
              hwndTarget = _screenControl.MainWindowHandle
            });
            devices.Add(new RAWINPUTDEVICE
            {
              usUsagePage = (ushort)SharpLib.Hid.UsagePage.Consumer,
              usUsage = (ushort)SharpLib.Hid.UsageCollection.Consumer.PlaybackSpeed,
              dwFlags = flags,
              hwndTarget = _screenControl.MainWindowHandle
            });
            devices.Add(new RAWINPUTDEVICE
            {
              usUsagePage = (ushort)SharpLib.Hid.UsagePage.Consumer,
              usUsage = (ushort)SharpLib.Hid.UsageCollection.Consumer.ProgrammableButtons,
              dwFlags = flags,
              hwndTarget = _screenControl.MainWindowHandle
            });
            devices.Add(new RAWINPUTDEVICE
            {
              usUsagePage = (ushort)SharpLib.Hid.UsagePage.Consumer,
              usUsage = (ushort)SharpLib.Hid.UsageCollection.Consumer.SelectDisc,
              dwFlags = flags,
              hwndTarget = _screenControl.MainWindowHandle
            });
            devices.Add(new RAWINPUTDEVICE
            {
              usUsagePage = (ushort)SharpLib.Hid.UsagePage.Consumer,
              usUsage = (ushort)SharpLib.Hid.UsageCollection.Consumer.Selection,
              dwFlags = flags,
              hwndTarget = _screenControl.MainWindowHandle
            });

            devices.Add(new RAWINPUTDEVICE
            {
              usUsagePage = (ushort)SharpLib.Hid.UsagePage.GenericDesktopControls,
              usUsage = (ushort)SharpLib.Hid.UsageCollection.GenericDesktop.SystemControl,
              dwFlags = flags,
              hwndTarget = _screenControl.MainWindowHandle
            });
            devices.Add(new RAWINPUTDEVICE
            {
              usUsagePage = (ushort)SharpLib.Hid.UsagePage.GenericDesktopControls,
              usUsage = (ushort)SharpLib.Hid.UsageCollection.GenericDesktop.GamePad,
              dwFlags = flags,
              hwndTarget = _screenControl.MainWindowHandle
            });
            devices.Add(new RAWINPUTDEVICE
            {
              usUsagePage = (ushort)SharpLib.Hid.UsagePage.GenericDesktopControls,
              usUsage = (ushort)SharpLib.Hid.UsageCollection.GenericDesktop.Joystick,
              dwFlags = flags,
              hwndTarget = _screenControl.MainWindowHandle
            });
            devices.Add(new RAWINPUTDEVICE
            {
              usUsagePage = (ushort)SharpLib.Hid.UsagePage.GenericDesktopControls,
              usUsage = (ushort)SharpLib.Hid.UsageCollection.GenericDesktop.Keyboard,
              dwFlags = flags,
              hwndTarget = _screenControl.MainWindowHandle
            });
            devices.Add(new RAWINPUTDEVICE
            {
              usUsagePage = (ushort)SharpLib.Hid.UsagePage.GenericDesktopControls,
              usUsage = (ushort)SharpLib.Hid.UsageCollection.GenericDesktop.KeyPad,
              dwFlags = flags,
              hwndTarget = _screenControl.MainWindowHandle
            });
            devices.Add(new RAWINPUTDEVICE
            {
              usUsagePage = (ushort)SharpLib.Hid.UsagePage.GenericDesktopControls,
              usUsage = (ushort)SharpLib.Hid.UsageCollection.GenericDesktop.Mouse,
              dwFlags = flags,
              hwndTarget = _screenControl.MainWindowHandle
            });

            _hidHandler = new SharpLib.Hid.Handler(devices.ToArray(), true, -1, -1);
            _hidHandler.OnHidEvent += new Handler.HidEventHandler(OnHidEvent);
          }
        }
        catch (Exception ex)
        {
          // ignored
          ServiceRegistration.Get<ILogger>().Error("InputDeviceManager: Failure to register HID handler", ex);
        }
        Thread.Sleep(500);
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
            _inputDevices.Add(device.Type, device);
        }
        catch
        {
          // ignored
        }
    }

    public bool RegisterExternalKeyHandling(Action<object, SharpLib.Hid.Event> hidEvent)
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

    public bool UnRegisterExternalKeyHandling(Action<object, SharpLib.Hid.Event> hidEvent)
    {
      lock (_listSyncObject)
      {
        if (_externalKeyPressHandlers.Contains(hidEvent))
        {
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

    #region Implementation of IPluginStateTracker

    /// <summary>
    /// Will be called when the plugin is started. This will happen as a result of a plugin auto-start
    /// or an item access which makes the plugin active.
    /// This method is called after the plugin's state was set to <see cref="PluginState.Active"/>.
    /// </summary>
    public void Activated(PluginRuntime pluginRuntime)
    {
      LoadSettings();
      SubscribeToMessages();
      var thread = new Thread(StartThread);
      thread.Start();
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
      if (_hidHandler != null)
      {
        //First de-register
        _hidHandler.Dispose();
        _hidHandler = null;
      }
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
