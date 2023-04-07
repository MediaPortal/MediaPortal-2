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
using InputDevices.Common.Serializing;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace Tests.Client.InputDevices
{
  public class Mapping
  {
    [Test]
    public void ShouldDeserializeMapping()
    {
      IList<InputDeviceMapping> expectedMappings = MappingTestUtils.CreateTestMappings();
      List<SerializedMapping> serialized = expectedMappings.Select(m => m.ToSerializedMapping()).ToList();

      IList<InputDeviceMapping> actualMappings;
      using (MemoryStream stream = new MemoryStream())
      {
        XmlSerializer serializer = new XmlSerializer(typeof(List<SerializedMapping>));
        serializer.Serialize(stream, serialized);
        stream.Position = 0;
        actualMappings = ((List<SerializedMapping>)serializer.Deserialize(stream)).Select(m => m.ToDeviceMapping()).ToList();
      }

      for (int i = 0; i < expectedMappings.Count; i++)
      {
        Assert.AreEqual(expectedMappings[i].DeviceId, actualMappings[i].DeviceId);
        Assert.IsTrue(MappingTestUtils.AreListItemsEqual(expectedMappings[i].MappedActions, actualMappings[i].MappedActions, MappingTestUtils.AreMappedActionsEqual));
      }
    }

    [Test]
    public void ShouldAddMapping()
    {
      InputDeviceMapping inputDeviceMapping = new InputDeviceMapping("Test");

      inputDeviceMapping.AddOrUpdateActionMapping(new InputAction("type", "action"), new[] { new Input("input", "inputName") }, out _);

      Assert.IsTrue(inputDeviceMapping.TryGetMappedAction(new[] { new Input("input", "inputName") }, out _));
      Assert.IsTrue(inputDeviceMapping.TryGetMappedInputs(new InputAction("type", "action"), out _));
    }

    [Test]
    public void ShouldReplaceMappingWithSameInput()
    {
      InputDeviceMapping inputDeviceMapping = new InputDeviceMapping("Test", new[] { new MappedAction(new InputAction("type", "action"), new Input("input", "inputName")) });

      inputDeviceMapping.AddOrUpdateActionMapping(new InputAction("type", "replacementAction"), new[] { new Input("input", "inputName") }, out InputAction replacedAction);

      Assert.AreEqual(new InputAction("type", "action"), replacedAction);
      Assert.IsFalse(inputDeviceMapping.TryGetMappedInputs(new InputAction("type", "action"), out _));
    }

    [Test]
    public void ShouldRemoveMappingByAction()
    {
      InputDeviceMapping inputDeviceMapping = new InputDeviceMapping("Test", new[] { new MappedAction(new InputAction("type", "action"), new Input("input", "inputName")) });

      inputDeviceMapping.RemoveMapping(new InputAction("type", "action"));

      Assert.IsFalse(inputDeviceMapping.TryGetMappedAction(new[] { new Input("input", "inputName") }, out _));
      Assert.IsFalse(inputDeviceMapping.TryGetMappedInputs(new InputAction("type", "action"), out _));
    }

    [Test]
    public void ShouldRemoveMappingByInput()
    {
      InputDeviceMapping inputDeviceMapping = new InputDeviceMapping("Test", new[] { new MappedAction(new InputAction("type", "action"), new Input("input", "inputName")) });

      inputDeviceMapping.RemoveMapping(new[] { new Input("input", "inputName") });

      Assert.IsFalse(inputDeviceMapping.TryGetMappedAction(new[] { new Input("input", "inputName") }, out _));
      Assert.IsFalse(inputDeviceMapping.TryGetMappedInputs(new InputAction("type", "action"), out _));
    }
  }
}
