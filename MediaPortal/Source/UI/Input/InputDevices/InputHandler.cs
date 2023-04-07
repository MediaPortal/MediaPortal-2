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

using InputDevices.Common.Devices;
using InputDevices.Common.Inputs;
using InputDevices.Common.Mapping;
using InputDevices.Common.Messaging;
using InputDevices.Mapping;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Messaging;
using MediaPortal.UI.Control.InputManager;
using System;
using System.Collections.Generic;

namespace InputDevices
{
  public class InputHandler
  {
    protected SynchronousMessageQueue _messageQueue;
    protected DeviceMappingWatcher _mappingWatcher;
    protected InputActionExecutorFactory _inputActionExecutorFactory;

    public InputHandler()
    {
      _mappingWatcher = new DeviceMappingWatcher();
      _inputActionExecutorFactory = new InputActionExecutorFactory();
      InitMessageQueue();
    }

    protected void InitMessageQueue()
    {
      if (_messageQueue != null)
        return;
      _messageQueue = new SynchronousMessageQueue(this, new[] { InputDeviceMessaging.CHANNEL });
      _messageQueue.MessagesAvailable += MessagesAvailable;
      _messageQueue.RegisterAtAllMessageChannels();
    }

    private void MessagesAvailable(SynchronousMessageQueue queue)
    {
      SystemMessage message;
      while ((message = queue.Dequeue()) != null)
      {
        try
        {
          if (message.MessageType as InputDeviceMessaging.MessageType? == InputDeviceMessaging.MessageType.InputPressed)
            message.MessageData[InputDeviceMessaging.HANDLED] = HandleInputMessage(message);
        }
        catch (Exception ex)
        {
          // In theory this should never be reached, but this message queue runs on the main thread and any uncaught exceptions
          // in the message loop will bring down the whole 
          ServiceRegistration.Get<ILogger>().Error($"{nameof(InputHandler)}: Error processing InputPressed message", ex);
        }
      }
    }

    private bool HandleInputMessage(SystemMessage message)
    {
      // Input already handled elsewhere?
      if (message.MessageData[InputDeviceMessaging.HANDLED] as bool? == true)
        return true;

      DeviceMetadata device = message.MessageData[InputDeviceMessaging.DEVICE_METADATA] as DeviceMetadata;
      if (!_mappingWatcher.TryGetMapping(device?.Id, out InputDeviceMapping mapping))
        mapping = device?.DefaultMapping;

      if (mapping == null)
        return false;

      IEnumerable<Input> inputs = message.MessageData[InputDeviceMessaging.PRESSED_INPUTS] as IEnumerable<Input>;
      if (!mapping.TryGetMappedAction(inputs, out InputAction inputAction))
        return false;

#if EXTENDED_INPUT_LOGGING
      ServiceRegistration.Get<ILogger>().Debug($"{nameof(InputHandler)}: Handling input action {inputAction} for inputs {Input.GetInputString(inputs)}");
#endif

      return HandleInputAction(inputAction);
    }

    protected bool HandleInputAction(InputAction inputAction)
    {
      if (!_inputActionExecutorFactory.TryCreate(inputAction, out IInputActionExecutor inputActionExecutor))
        return false;

      inputActionExecutor.Execute();
      return true;
    }

    protected IInputManager InputManager
    {
      get { return ServiceRegistration.Get<IInputManager>(); }
    }
  }
}
