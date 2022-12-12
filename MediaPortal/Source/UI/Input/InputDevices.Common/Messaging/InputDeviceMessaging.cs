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
using MediaPortal.Common;
using MediaPortal.Common.Messaging;
using System.Collections.Generic;

namespace InputDevices.Common.Messaging
{
  /// <summary>
  /// Sends device input messages to the MP2 system.
  /// </summary>
  public static class InputDeviceMessaging
  {
    /// <summary>
    /// The name of the channel to preview input messages. Implementations that need to, typically temporarily, intercept
    /// inputs before they are handled should subscribe to this channel and set the <see cref="HANDLED"/> key on the
    /// <see cref="SystemMessage.MessageData"/> to <c>true</c> to prevent the default handling of the message.
    /// </summary>
    public const string PREVIEW_CHANNEL = "InputDevicesNew.Preview";

    /// <summary>
    /// The name of the channel where input messages are sent after any listeners that are subscribed to <see cref="PREVIEW_CHANNEL"/>
    /// have possibly handled the message. Subscribers should check whether the <see cref="HANDLED"/> key on the
    /// <see cref="SystemMessage.MessageData"/> has been set to <c>true</c> by a previous subscriber to determine whether the message
    /// has already been handled.
    /// </summary>
    public const string CHANNEL = "InputDevicesNew";

    /// <summary>
    /// The message types of an input message. 
    /// </summary>
    public enum MessageType
    {
      /// <summary>
      /// The type of a message containing device inputs. The <see cref="SystemMessage.MessageData"/>
      /// will contain a <see cref="DEVICE_ID"/> key with the device id <see cref="string"/>, a <see cref="PRESSED_INPUTS"/>
      /// key containing an <see cref="IList{T}"/> whose generic type argument is <see cref="Input"/> of all the inputs currently pressed and a <see cref="HANDLED"/>
      /// key with a <see cref="bool"/> indicating whether the inputs have been handled. Optionally a <see cref="DEFAULT_MAPPING"/>
      /// key may be present containing the default <see cref="InputDeviceMapping"/> for the device that generated the input.
      /// </summary>
      InputPressed
    }

    /// <summary>
    /// Key for the <see cref="DeviceMetadata"/> contained in the <see cref="SystemMessage.MessageData"/>
    /// of a message with type <see cref="MessageType.InputPressed"/>.
    /// </summary>
    public const string DEVICE_METADATA = "DeviceMetadata";

    /// <summary>
    /// Key for the <see cref="IList{Input}"/> contained in the <see cref="SystemMessage.MessageData"/>
    /// of a message with type <see cref="MessageType.InputPressed"/>.
    /// </summary>
    public const string PRESSED_INPUTS = "PressedInputs";

    /// <summary>
    /// Key for the <see cref="bool"/> indicating whether a message has been handled contained in the <see cref="SystemMessage.MessageData"/>
    /// of a message with type <see cref="MessageType.InputPressed"/>.
    /// </summary>
    public const string HANDLED = "Handled";

    /// <summary>
    /// Broadcasts an <see cref="MessageType.InputPressed"/> message.
    /// </summary>
    /// <param name="deviceMetadata">The metadata of the device that orginated the inputs.</param>
    /// <param name="pressedInputs">All currently pressed inputs on the device.</param>
    /// <param name="additionalData">Additional device specific data to include in the message.</param>
    /// <returns><c>true</c> if the message was handled; else <c>false</c>.</returns>
    public static bool BroadcastInputPressedMessage(DeviceMetadata deviceMetadata, IList<Input> pressedInputs, IDictionary<string, object> additionalData = null)
    {
      SystemMessage msg = new SystemMessage(MessageType.InputPressed);
      msg.MessageData[DEVICE_METADATA] = deviceMetadata;
      msg.MessageData[PRESSED_INPUTS] = pressedInputs;
      msg.MessageData[HANDLED] = false;
      if (additionalData != null)
        foreach (KeyValuePair<string, object> keyValuePair in additionalData)
          msg.MessageData[keyValuePair.Key] = keyValuePair.Value;

      IMessageBroker messageBroker = ServiceRegistration.Get<IMessageBroker>();

      messageBroker.Send(PREVIEW_CHANNEL, msg);

      // A subscriber to the preview channel may have already handled the message however
      // still send it on the main channel and let subscribers determine what to do with
      // an already handled message.
      messageBroker.Send(CHANNEL, msg);

      return msg.MessageData[HANDLED] as bool? == true;
    }
  }
}
