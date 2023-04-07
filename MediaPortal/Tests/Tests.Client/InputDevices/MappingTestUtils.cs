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
using InputDevices.Common.Mapping;
using System;
using System.Collections.Generic;

namespace Tests.Client.InputDevices
{
  public class MappingTestUtils
  {
    public static List<InputDeviceMapping> CreateTestMappings()
    {
      List<InputDeviceMapping> inputDeviceMappings = new List<InputDeviceMapping>();

      int currentInput = 0;

      for (int i = 0; i < 2; i++)
      {
        List<MappedAction> mappings = new List<MappedAction>();
        for (int j = 0; j < 2; j++)
          mappings.Add(
            new MappedAction(new InputAction("Action Type " + j, "Action " + j), new List<Input> { new Input(currentInput.ToString(), "Button" + currentInput++), new Input(currentInput.ToString(), "Button " + currentInput++) })
            );
        inputDeviceMappings.Add(new InputDeviceMapping("Device " + i, mappings));
      }

      inputDeviceMappings.Add(new InputDeviceMapping("Device " + 2, new List<MappedAction>()));

      inputDeviceMappings.Add(new InputDeviceMapping("Device " + 3, new List<MappedAction> { new MappedAction(new InputAction("EmptyInputType", "EmptyInput"), new List<Input>()) }));

      return inputDeviceMappings;
    }

    public static bool AreMappedActionsEqual(MappedAction expected, MappedAction actual)
    {
      return expected.Action == actual.Action && AreListItemsEqual(expected.Inputs, actual.Inputs, AreMappedInputsEqual);
    }

    public static bool AreMappedInputsEqual(Input expected, Input actual)
    {
      return expected.Id == actual.Id && expected.Name == actual.Name;
    }

    public static bool AreListItemsEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual, Func<T, T, bool> comparer)
    {
      if (expected == null || actual == null)
        return expected == null && actual == null;

      List<T> expectedList = new List<T>(expected);
      List<T> actualList = new List<T>(actual);
      if (expectedList.Count != actualList.Count)
        return false;

      for (int i = 0; i < expectedList.Count; i++)
        if (!comparer.Invoke(expectedList[i], actualList[i]))
          return false;
      return true;
    }
  }
}
