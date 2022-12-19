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

using InputDevices.Common.Inputs;
using SharpLib.Hid;
using SharpLib.Hid.Usage;
using System;
using System.Collections.Generic;

namespace HidInput.Inputs
{
  /// <summary>
  /// Type specific implementation of <see cref="GenericInput"/> which will be created and added to an <see cref="InputCollection"/>
  /// when <see cref="GenericInput.TryDecodeEvent(Event, InputCollection)"/> is called with a generic <see cref="Event"/>.
  /// A type specific implementation is needed so that <see cref="InputCollection.RemoveAll(Type)"/> can be used to only remove inputs
  /// for a specific usage page.
  /// </summary>
  /// <typeparam name="T"></typeparam>
  public class GenericInput<T> : GenericInput where T : Enum
  {
    public GenericInput(T usage)
      : base(usage)
    {
      Usage = usage;
    }

    public T Usage { get; }
  }

  /// <summary>
  /// Base class for all HID inputs that don't have a specialized implementation, i.e. everything except mouse, keyboard and gamepad input.
  /// The input type added by a call to <see cref="TryDecodeEvent(Event, InputCollection)"/> will be a specific implementation of <see cref="GenericInput{T}"/>
  /// where T is the corresponding usage type for the inputs's <see cref="UsagePage"/>.
  /// </summary>
  public class GenericInput : Input 
  {
    /// <summary>
    /// Maps a <see cref="UsagePage"/> to a <see cref="IGenericInputFactory"/> that will
    /// create a specific <see cref="GenericInput{T}"/> with the appropriate type.
    /// </summary>
    static readonly Dictionary<UsagePage, IGenericInputFactory> _usageTypeFactories = new Dictionary<UsagePage, IGenericInputFactory>
    {
      { UsagePage.GenericDesktopControls, new GenericInputFactory<GenericDevice>() },
      { UsagePage.Consumer, new GenericInputFactory<ConsumerControl>() },
      { UsagePage.WindowsMediaCenterRemoteControl, new GenericInputFactory<WindowsMediaCenterRemoteControl>() },
      { UsagePage.Telephony, new GenericInputFactory<TelephonyDevice>() },
      { UsagePage.SimulationControls, new GenericInputFactory<SimulationControl>() },
      { UsagePage.GameControls, new GenericInputFactory<GameControl>() }
    };

    public GenericInput(Enum usage)
    : base($"Hid.{usage.GetType().Name}.{usage}", usage.ToString(), false)
    { }

    public static bool TryDecodeEvent(Event hidEvent, InputCollection inputCollection, IEnumerable<ushort> usagesToHandle = null)
    {
      // Gamepad input is handled separately, don't handle it here
      if (!hidEvent.IsGeneric || hidEvent.Device?.IsGamePad == true)
        return false;

      if (!_usageTypeFactories.TryGetValue(hidEvent.UsagePageEnum, out IGenericInputFactory inputFactory))
        return false;

      // Each event contains the current state of all usages, any previously pressed usages are no longer
      // pressed if they are not contained in the usages for this event so just remove all previously pressed
      // usages of this type and add the currently pressed usages.
      bool result = inputCollection.RemoveAll(inputFactory.InputType);

      foreach (ushort usage in usagesToHandle ?? hidEvent.Usages)
        if (inputFactory.TryCreateInput(usage, out GenericInput input))
          result |= inputCollection.AddInput(input);

      return result;
    }
  }
}
